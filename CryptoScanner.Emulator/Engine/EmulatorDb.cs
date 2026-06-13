using CryptoScanner.Core.Const;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
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
            symbol.Data.ResetTrendData();
        }
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

        // Close the per-run log file opened in StartRun; subsequent lines go only to the shared logs.
        ScannerLog.StopRunLog();
    }


    /// <summary>
    /// Fills a run's stored aggregates from its current signals and positions: signal/position
    /// counts, the open/won/lost split, and the realised Profit and Invested totals over the CLOSED
    /// positions. Profit/Invested are stored as TEXT (decimal), so they are CAST to REAL for the
    /// numeric comparison and SUM. Caller is responsible for persisting the run (Update).
    /// </summary>
    private static void ComputeRunStats(CryptoDatabase database, CryptoEmulatorRun run)
    {
        int id = run.Id;

        run.SignalCount = database.Connection.ExecuteScalar<int>(
            "select count(*) from signal where EmulatorRunId = @id", new { id });
        run.PositionCount = database.Connection.ExecuteScalar<int>(
            "select count(*) from position where EmulatorRunId = @id", new { id });

        // Outcome split. Open = no CloseTime yet; closed positions are won/lost on their realised Profit.
        run.PositionsOpen = database.Connection.ExecuteScalar<int>(
            "select count(*) from position where EmulatorRunId = @id and CloseTime is null", new { id });
        run.PositionsWon = database.Connection.ExecuteScalar<int>(
            "select count(*) from position where EmulatorRunId = @id and CloseTime is not null and CAST(Profit as REAL) > 0", new { id });
        run.PositionsLost = database.Connection.ExecuteScalar<int>(
            "select count(*) from position where EmulatorRunId = @id and CloseTime is not null and (Profit is null or CAST(Profit as REAL) <= 0)", new { id });

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
}
