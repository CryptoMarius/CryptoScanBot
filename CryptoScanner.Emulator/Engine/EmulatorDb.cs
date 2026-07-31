#if DEBUG
using CryptoScanner.Analyzers.Choch.Signal;
#endif
using CryptoScanner.Core.Const;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;


using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Utility entry points for the emulator's own CryptoScanBot.db lifecycle.
///
/// The emulator runs in a separate data folder (set via the --folder command-line flag,
/// resolved by IPlatformService.GetDataDirectory). Database paths, settings files and cached
/// candle files are therefore physically separate from the live scanner — this class only
/// adds the run-bookkeeping on top of that isolation.
/// </summary>
public static class EmulatorDb
{
    /// <summary>
    /// Drops the emulator's main CryptoScanBot.db file and rebuilds it from scratch via the
    /// regular migration flow. Idempotent. Per-exchange candle DBs are intentionally left
    /// alone (they hold the historical candles the run replays).
    /// </summary>
    public static void Reset()
    {
        string dbFile = Path.Combine(GlobalData.AppDataFolder, Constants.AppName + ".db");
        if (File.Exists(dbFile))
            File.Delete(dbFile);

        // Recreate via the standard schema bootstrap.
        CryptoDatabase.SetDatabaseDefaults();
    }

    /// <summary>
    /// Clears the in-memory zone state (DLZ/FVG/SMC lists + DLZ swing-point admin) for the given
    /// symbols so a run starts from a blank in-memory slate.
    ///
    /// Stored zones are NO LONGER deleted here: each zone now carries its EmulatorRunId (set on insert)
    /// and <see cref="Zones.ZoneDlz.LoadZonesForSymbol"/> loads only the active run's zones. A fresh run
    /// has none yet, so it starts clean automatically and cannot inherit a previous run's already-closed
    /// zones (the look-ahead that made runs non-reproducible). Keeping prior runs' zones in the DB is
    /// exactly what lets the chart show a finished run's zones afterwards. The in-memory reset is still
    /// required: without it the first inline FVG/SMC scan (before the first DLZ reload) would still see
    /// last run's leftover in-memory zones from the same app session.
    /// </summary>
    public static void ClearZonesForSymbols(CryptoScanner.Core.Model.CryptoExchange exchange, IEnumerable<string> symbolNames)
    {
        foreach (string name in symbolNames)
        {
            if (!exchange.SymbolListName.TryGetValue(name, out CryptoSymbol? symbol))
                continue;

            symbol.Data.ResetFvgData();
            symbol.Data.ResetDlzData();
            symbol.Data.ResetSmcData();
            // Full reset (incl. per-interval cached ZigZag indicators): a new run reusing this symbol
            // object must not inherit cached pivots/trend from a previous run, which may have replayed
            // a different period or used different settings (TrendType/UseHighLow/CandleCount).
            symbol.Data.ResetTrendDataAndCaches();
            // ...and the incremental zone-calculation cursors (FVG/SMC/DLZ) — a fresh run must do a
            // full historical rescan, not "continue" from a previous run's progress.
            symbol.Data.ResetZoneCalculationCursors();

            // Force ZoneThreadCalculate.CalculateZones to (re)load this symbol's zones from the DB
            // on its next drain instead of assuming the previous run's load is still valid.
            symbol.Data.ZonesLoaded = false;
            symbol.Data.ZonesLoadedRunId = null;
        }

#if DEBUG
        SignalChochLongBase.ResetDiagnosticLog();
#endif
    }


    /// <summary>
    /// Switches the emulator's main CryptoScanBot.db into a write-optimised mode for a run.
    /// <c>journal_mode=WAL</c> is PERSISTENT — SQLite stores it in the database file header — so this
    /// single call before the replay starts applies to every <see cref="CryptoDatabase"/> connection
    /// the run later opens (each <see cref="ThreadSaveObjects.Flush"/> opens its own), without any
    /// change to Core. WAL replaces the default DELETE journal's create-and-delete-per-transaction
    /// churn (plus an fsync per commit) with a single append-only log; a higher
    /// <c>wal_autocheckpoint</c> lets the many tiny per-tick flush transactions accumulate before
    /// SQLite checkpoints them back to the main file. Safe for an emulator run: a crash mid-run
    /// loses only that run's (reproducible) data, never the historical candles.
    /// </summary>
    public static void EnableFastWriteMode()
    {
        using var database = new CryptoDatabase();
        database.Open();
        database.Connection.Execute("PRAGMA journal_mode=WAL;");
        database.Connection.Execute("PRAGMA wal_autocheckpoint=10000;");
    }


    /// <summary>
    /// Inserts an EmulatorRun row and stores its id in
    /// <see cref="GlobalData.CurrentEmulatorRunId"/> so subsequent signals and positions are
    /// tagged with it. Call once at run start.
    /// </summary>
    public static CryptoEmulatorRun StartRun(string configJson, DateTime fromDate, DateTime toDate,
        string label = "", string? settingsJson = null, string? gitSha = null)
    {
        using var database = new CryptoDatabase();
        database.Open();

        var run = new CryptoEmulatorRun
        {
            StartedAt = DateTime.UtcNow,
            Label = label,
            FromDate = fromDate,
            ToDate = toDate,
            ConfigJson = configJson,
            SettingsJson = settingsJson,
            GitSha = gitSha,
        };
        run.Id = (int)database.Connection.Insert(run);

        GlobalData.CurrentEmulatorRunId = run.Id;

        // Open a dedicated log file named after this run id, so every line produced during the run
        // lands in its own "<base> Run <id>.log" alongside the shared default/error/trace logs.
        ScannerLog.StartRunLog(run.Id);
        return run;
    }

    /// <summary>
    /// Marks the active run as finished — updates FinishedAt, Result and the
    /// signal/position counters — and clears <see cref="GlobalData.CurrentEmulatorRunId"/>.
    /// </summary>
    public static void FinishRun(string result)
    {
        int? runId = GlobalData.CurrentEmulatorRunId;
        if (runId == null)
            return;

        using var database = new CryptoDatabase();
        database.Open();

        var run = database.Connection.Get<CryptoEmulatorRun>(runId.Value);
        if (run != null)
        {
            run.FinishedAt = DateTime.UtcNow;
            run.Result = result;
            ComputeRunStats(database, run);
            database.Connection.Update(run);
        }

        GlobalData.CurrentEmulatorRunId = null;

        // Release the position-check handler's reused DB connection so the file is not left locked
        // (a Reset deletes it, which fails on Windows while a handle is open). Reopened next run.
        GlobalData.ThreadCheckPosition?.CloseEmulatorConnection();

        // Close the per-run log file opened in StartRun; subsequent lines go only to the shared logs.
        ScannerLog.StopRunLog();
    }


    /// <summary>
    /// Fills a run's stored aggregates from its current signals and positions: signal/position
    /// counts, the open/won/lost/timeout split, and the realised Profit and Invested totals over the
    /// CLOSED positions. Profit/Invested are stored as TEXT (decimal), so they are CAST to REAL for the
    /// numeric comparison and SUM. Caller is responsible for persisting the run (Update).
    /// </summary>
    private static void ComputeRunStats(CryptoDatabase database, CryptoEmulatorRun run)
    {
        int id = run.Id;
        int timeoutStatus = (int)CryptoPositionStatus.Timeout;

        // Because we delete the signals afterwards (db gets way to large)
        int count = database.Connection.ExecuteScalar<int>(
            "select count(*) from signal where EmulatorRunId = @id", new { id });
        if (count > 0)
            run.SignalCount = count;
        run.PositionCount = database.Connection.ExecuteScalar<int>(
            "select count(*) from position where EmulatorRunId = @id", new { id });

        // Outcome split. Open = no CloseTime yet. Timeout = the entry order never filled (status
        // Timeout) — it never became a real trade, so it is excluded from Won/Lost and counted on its
        // own. The remaining closed positions are won/lost on their realised Profit.
        run.PositionsOpen = database.Connection.ExecuteScalar<int>(
            "select count(*) from position where EmulatorRunId = @id and CloseTime is null", new { id });
        run.PositionsTimeout = database.Connection.ExecuteScalar<int>(
            "select count(*) from position where EmulatorRunId = @id and CloseTime is not null and Status = @timeoutStatus", new { id, timeoutStatus });
        run.PositionsWon = database.Connection.ExecuteScalar<int>(
            "select count(*) from position where EmulatorRunId = @id and CloseTime is not null and Status != @timeoutStatus and CAST(Profit as REAL) > 0", new { id, timeoutStatus });
        run.PositionsLost = database.Connection.ExecuteScalar<int>(
            "select count(*) from position where EmulatorRunId = @id and CloseTime is not null and Status != @timeoutStatus and (Profit is null or CAST(Profit as REAL) <= 0)", new { id, timeoutStatus });

        double profit = database.Connection.ExecuteScalar<double?>(
            "select sum(CAST(Profit as REAL)) from position where EmulatorRunId = @id and CloseTime is not null", new { id }) ?? 0.0;
        run.Profit = (decimal)profit;

        // Total invested capital of the closed positions (same scope as Profit), so the Results grid
        // can show the total return as a percentage: 100 * Profit / Invested.
        double invested = database.Connection.ExecuteScalar<double?>(
            "select sum(CAST(Invested as REAL)) from position where EmulatorRunId = @id and CloseTime is not null", new { id }) ?? 0.0;
        run.Invested = (decimal)invested;
    }


    /// <summary>
    /// Recomputes and stores the aggregates (counts, open/won/lost split, Profit and Invested) for the
    /// given runs from their current signals and positions. Use to backfill runs created before a stat
    /// column existed (e.g. Invested → the Profit % column) or after positions were edited. Does NOT
    /// touch FinishedAt/Result/config. Returns the number of runs updated.
    /// </summary>
    public static int RecalculateRuns(IEnumerable<int> runIds)
    {
        using var database = new CryptoDatabase();
        database.Open();

        int updated = 0;
        foreach (int id in runIds)
        {
            var run = database.Connection.Get<CryptoEmulatorRun>(id);
            if (run == null)
                continue;
            ComputeRunStats(database, run);
            database.Connection.Update(run);
            updated++;
        }
        return updated;
    }


    /// <summary>
    /// Deletes all rows from Signal, Order, Trade, PositionStep and PositionPart — the bulk data
    /// that the trader needs during a run but that can be discarded once the run's stats are computed
    /// in FinishRun. Position and EmulatorRun rows are kept (they hold the aggregated results).
    /// Uses bare DELETE (no WHERE) which SQLite optimizes as a page-deallocation truncate.
    /// Call VACUUM separately (e.g. at end of a queue batch) to reclaim disk space.
    /// </summary>
    public static void PurgeTransientData()
    {
        using var database = new CryptoDatabase();
        database.Open();
        database.Connection.Execute("update position set signalid=null where not signalid=null");
        database.Connection.Execute("delete from [Asset]");
        database.Connection.Execute("delete from [Signal]");
        database.Connection.Execute("delete from [Order]");
        database.Connection.Execute("delete from [Trade]");
        // Need these for insights
        //database.Connection.Execute("delete from PositionStep");
        //database.Connection.Execute("delete from PositionPart");
        //database.Connection.Execute("delete from Position");
    }


    /// <summary>
    /// Reclaims unused pages after transient data has been purged.
    /// </summary>
    public static void Vacuum()
    {
        using var database = new CryptoDatabase();
        database.Open();
        database.Connection.Execute("VACUUM");
    }


    /// <summary>Recalculates every run in the EmulatorRun table. Convenience wrapper for the full backfill.</summary>
    public static int RecalculateAllRuns()
    {
        List<int> ids;
        using (var database = new CryptoDatabase())
        {
            database.Open();
            ids = database.Connection.Query<int>("select Id from EmulatorRun").AsList();
        }
        return RecalculateRuns(ids);
    }


    /// <summary>
    /// Permanently removes an emulator run and everything tagged with it: the run's signals, its
    /// positions, and the position parts/steps that hang off those positions. Parts and steps carry
    /// no EmulatorRunId of their own (they reference PositionId), so they are removed via a subselect
    /// on the run's positions. Everything runs inside one transaction, so a failure leaves the run
    /// fully intact rather than half-deleted.
    ///
    /// Zones now carry an EmulatorRunId too, so the run's zones are removed as well — otherwise a
    /// deleted run would leave orphaned zones behind that the chart could still load.
    /// </summary>
    /// <summary>
    /// Returns the full scanner settings snapshot (GlobalData.Settings serialized at run start) that
    /// was stored with the run, or null when the run has none (e.g. a legacy run). Loaded on demand
    /// so the runs grid does not have to carry the large JSON for every row.
    /// </summary>
    public static string? GetSettingsJson(int runId)
    {
        using var database = new CryptoDatabase();
        database.Open();
        return database.Connection.ExecuteScalar<string?>(
            "select SettingsJson from EmulatorRun where Id = @id", new { id = runId });
    }


    /// <summary>
    /// Updates the free-text Label (remark) of one run. Used by the Results tab's "Edit label…" action so
    /// a run can be annotated or renamed after the fact. Only the Label column is touched.
    /// </summary>
    public static void UpdateLabel(int runId, string label)
    {
        using var database = new CryptoDatabase();
        database.Open();
        database.Connection.Execute(
            "update EmulatorRun set Label = @label where Id = @id", new { id = runId, label });
    }


    public static void DeleteRun(int runId) => DeleteRuns([runId]);


    /// <summary>
    /// Deletes multiple emulator runs (and everything tagged with them — signals, positions and their
    /// parts/steps, plus the run's zones) in a SINGLE transaction, so a multi-select delete is
    /// all-or-nothing. See <see cref="DeleteRun"/> for the per-run rationale.
    /// </summary>
    public static void DeleteRuns(IEnumerable<int> runIds)
    {
        using var database = new CryptoDatabase();
        database.Open();
        using var transaction = database.BeginTransaction();
        try
        {
            foreach (int runId in runIds)
            {
                // Orders and Trades are linked via PositionStep.OrderId (the exchange order id).
                // They must be removed before the steps themselves are deleted.
                database.Connection.Execute(
                    "delete from [Trade] where OrderId in (select OrderId from PositionStep where PositionId in (select Id from Position where EmulatorRunId = @id) union select Order2Id from PositionStep where Order2Id is not null and PositionId in (select Id from Position where EmulatorRunId = @id))",
                    new { id = runId }, transaction);
                database.Connection.Execute(
                    "delete from [Order] where OrderId in (select OrderId from PositionStep where PositionId in (select Id from Position where EmulatorRunId = @id) union select Order2Id from PositionStep where Order2Id is not null and PositionId in (select Id from Position where EmulatorRunId = @id))",
                    new { id = runId }, transaction);
                database.Connection.Execute(
                    "delete from PositionStep where PositionId in (select Id from Position where EmulatorRunId = @id)",
                    new { id = runId }, transaction);
                database.Connection.Execute(
                    "delete from PositionPart where PositionId in (select Id from Position where EmulatorRunId = @id)",
                    new { id = runId }, transaction);
                database.Connection.Execute(
                    "delete from Position where EmulatorRunId = @id", new { id = runId }, transaction);
                database.Connection.Execute(
                    "delete from Signal where EmulatorRunId = @id", new { id = runId }, transaction);
                database.Connection.Execute(
                    "delete from Zone where EmulatorRunId = @id", new { id = runId }, transaction);
                database.Connection.Execute(
                    "delete from EmulatorRun where Id = @id", new { id = runId }, transaction);
            }
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }


    /// <summary>
    /// Permanently removes EVERY emulator run and everything tagged with one (signals, positions,
    /// their parts/steps, and zones) — a full reset back to a clean slate. Unlike
    /// <see cref="DeleteRuns"/>, this does not filter by a specific set of run ids: it deletes every
    /// row whose EmulatorRunId is not null in one pass per table, which is what makes it fast even
    /// with many runs (no per-id subselect, no loop).
    /// </summary>
    public static void DeleteAllRuns()
    {
        using var database = new CryptoDatabase();
        database.Open();
        using var transaction = database.BeginTransaction();
        try
        {
            // Orders and Trades are linked via PositionStep.OrderId; delete them before the steps.
            database.Connection.Execute(
                "delete from [Trade] where OrderId in (select OrderId from PositionStep where PositionId in (select Id from Position where EmulatorRunId is not null) union select Order2Id from PositionStep where Order2Id is not null and PositionId in (select Id from Position where EmulatorRunId is not null))",
                transaction: transaction);
            database.Connection.Execute(
                "delete from [Order] where OrderId in (select OrderId from PositionStep where PositionId in (select Id from Position where EmulatorRunId is not null) union select Order2Id from PositionStep where Order2Id is not null and PositionId in (select Id from Position where EmulatorRunId is not null))",
                transaction: transaction);
            database.Connection.Execute(
                "delete from PositionStep where PositionId in (select Id from Position where EmulatorRunId is not null)",
                transaction: transaction);
            database.Connection.Execute(
                "delete from PositionPart where PositionId in (select Id from Position where EmulatorRunId is not null)",
                transaction: transaction);
            database.Connection.Execute(
                "delete from Position where EmulatorRunId is not null", transaction: transaction);
            database.Connection.Execute(
                "delete from Signal where EmulatorRunId is not null", transaction: transaction);
            database.Connection.Execute(
                "delete from Zone where EmulatorRunId is not null", transaction: transaction);
            database.Connection.Execute("delete from EmulatorRun", transaction: transaction);

            // Reset the auto-increment counters so the next run starts at id 1 again.
            // sqlite_sequence only contains rows for tables that have had at least one insert,
            // so the WHERE guards against a no-op on a fresh DB.
            foreach (string table in new[] { "EmulatorRun" })
                database.Connection.Execute(
                    "delete from sqlite_sequence where name = @table",
                    new { table }, transaction);
            transaction.Commit();
        }
        catch
        {
            transaction.Rollback();
            throw;
        }
    }
}
