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

using System.Text.Json;

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
            // Config names may be bare pairs from before the product moved into the symbol name
            if (!exchange.TryGetSymbolByPair(name, out CryptoSymbol? symbol))
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
    /// signal/position counters — clears <see cref="GlobalData.CurrentEmulatorRunId"/> and puts
    /// the clock back on real time.
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

        // Put the clock back on real time. The replay parks it on the moment it last replayed and
        // nothing moved it back, so everything that happened between two runs saw the end of the
        // previous run as "now". "Fetch candles" is what suffered: ZoneCandleEngine will not ask an
        // exchange for candles beyond the clock (they would be in the future), so after a run it
        // could no longer synchronise past that run's end date - the gap between the last run and
        // today stayed empty however often the button was pressed, until the emulator was restarted.
        if (GlobalData.Clock is EmulatorClock clock)
            clock.UtcNow = DateTime.UtcNow;

        // Release the position-check handler's reused DB connection so the file is not left locked
        // (a Reset deletes it, which fails on Windows while a handle is open). Reopened next run.
        GlobalData.ThreadCheckPosition?.CloseEmulatorConnection();

        // Fold the write-ahead log back into the database, now that this run's connections are
        // closed. WAL keeps every change in a side file until a checkpoint moves it into the
        // database itself, and the automatic one only runs when nothing is reading at that moment -
        // during a batch there almost always is, so it kept being skipped and the side file grew
        // for the whole batch. Measured on 02-09-2026: 721 MB after four runs, all of it folded
        // back in one go when the window was closed, which is the minutes-long wait that looked
        // like the emulator ignoring the Stop button. Per run it is the same work spread out.
        CheckpointWal(database);

        // Close the per-run log file opened in StartRun; subsequent lines go only to the shared logs.
        ScannerLog.StopRunLog();
    }


    /// <summary>
    /// Moves the write-ahead log into the database file and empties it, and says in the log what
    /// that cost and how much was folded back.
    /// <para>
    /// TRUNCATE rather than PASSIVE: passive leaves the file at whatever size it reached, and the
    /// size is the whole point. It waits for readers, so it belongs after the run's own connections
    /// have been released - a checkpoint that cannot get the file to itself quietly does nothing.
    /// </para>
    /// <para>
    /// Never throws. A checkpoint that fails costs disk space and a slower shutdown; it is not a
    /// reason to fail a run that has already been written away.
    /// </para>
    /// </summary>
    private static void CheckpointWal(CryptoDatabase database)
    {
        try
        {
            // On the connection this method was handed rather than one of its own. A second
            // connection would be a second reader, and TRUNCATE waits for readers - so it would be
            // waiting for us.
            //
            // The path the connection actually resolved to, rather than one rebuilt from
            // AppDataFolder here - the two can drift, and then the sizes below describe another file.
            string wal = database.Connection.DataSource + "-wal";
            long before = File.Exists(wal) ? new FileInfo(wal).Length : 0;
            if (before == 0)
                return;

            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            database.Connection.Execute("PRAGMA wal_checkpoint(TRUNCATE);");
            stopwatch.Stop();

            long after = File.Exists(wal) ? new FileInfo(wal).Length : 0;
            GlobalData.AddTextToLogTab($"WAL checkpoint: {before / (1024 * 1024)} MB folded back in "
                + $"{stopwatch.Elapsed.TotalSeconds:F1}s, {after / (1024 * 1024)} MB left");
        }
        catch (Exception ex)
        {
            GlobalData.AddTextToLogTab($"WAL checkpoint FAILED: {ex.Message}");
        }
    }


    /// <summary>
    /// Whether a run's aggregates may be recomputed from the position table.
    ///
    /// <para>
    /// No, when the table holds no positions for it while the run row says it had some: those
    /// positions were archived away, and the counters and summary on the row ARE the run now.
    /// Recomputing would replace them with zeros and destroy the only record that is left. Session0
    /// is the whole argument - 6.366 runs of stored aggregates and an empty Position table, so one
    /// press of Recalculate would wipe out every result in it.
    /// </para>
    /// <para>
    /// A run that genuinely traded nothing (a zone strategy with an empty interval list, say) has a
    /// stored count of zero as well, so it loses nothing by being skipped.
    /// </para>
    /// </summary>
    internal static bool CanRecalculate(int positionsInTable, int storedPositionCount) =>
        positionsInTable > 0 || storedPositionCount <= 0;


    /// <summary>
    /// Fills a run's stored aggregates from its current signals and positions: signal/position
    /// counts, the open/won/lost/timeout split, and the realised Profit and Invested totals over the
    /// CLOSED positions. Profit/Invested are stored as TEXT (decimal), so they are CAST to REAL for the
    /// numeric comparison and SUM. Caller is responsible for persisting the run (Update).
    ///
    /// <para>Returns false and changes nothing when the run's positions are gone; see
    /// <see cref="CanRecalculate"/>.</para>
    /// </summary>
    private static bool ComputeRunStats(CryptoDatabase database, CryptoEmulatorRun run)
    {
        int id = run.Id;
        int timeoutStatus = (int)CryptoPositionStatus.Timeout;
        int cancelledStatus = (int)CryptoPositionStatus.Cancelled;

        int positions = database.Connection.ExecuteScalar<int>(
            "select count(*) from position where EmulatorRunId = @id", new { id });
        if (!CanRecalculate(positions, run.PositionCount))
            return false;

        // Because we delete the signals afterwards (db gets way to large)
        int count = database.Connection.ExecuteScalar<int>(
            "select count(*) from signal where EmulatorRunId = @id", new { id });
        if (count > 0)
            run.SignalCount = count;
        run.PositionCount = positions;

        // Outcome split. Open = no CloseTime yet. Timeout = the entry order never filled (status
        // Timeout) — it never became a real trade, so it is excluded from Won/Lost and counted on its
        // own. Cancelled = replaced by a newer signal before filling. The remaining closed positions
        // are won/lost on their realised Profit.
        run.PositionsOpen = database.Connection.ExecuteScalar<int>(
            "select count(*) from position where EmulatorRunId = @id and CloseTime is null", new { id });
        run.PositionsTimeout = database.Connection.ExecuteScalar<int>(
            "select count(*) from position where EmulatorRunId = @id and CloseTime is not null and Status = @timeoutStatus", new { id, timeoutStatus });
        run.PositionsCancelled = database.Connection.ExecuteScalar<int>(
            "select count(*) from position where EmulatorRunId = @id and CloseTime is not null and Status = @cancelledStatus", new { id, cancelledStatus });
        run.PositionsWon = database.Connection.ExecuteScalar<int>(
            "select count(*) from position where EmulatorRunId = @id and CloseTime is not null and Status not in (@timeoutStatus, @cancelledStatus) and CAST(Profit as REAL) > 0", new { id, timeoutStatus, cancelledStatus });
        run.PositionsLost = database.Connection.ExecuteScalar<int>(
            "select count(*) from position where EmulatorRunId = @id and CloseTime is not null and Status not in (@timeoutStatus, @cancelledStatus) and (Profit is null or CAST(Profit as REAL) <= 0)", new { id, timeoutStatus, cancelledStatus });

        double profit = database.Connection.ExecuteScalar<double?>(
            "select sum(CAST(Profit as REAL)) from position where EmulatorRunId = @id and CloseTime is not null", new { id }) ?? 0.0;
        run.Profit = (decimal)profit;

        // Total invested capital of the closed positions (same scope as Profit), so the Results grid
        // can show the total return as a percentage: 100 * Profit / Invested.
        double invested = database.Connection.ExecuteScalar<double?>(
            "select sum(CAST(Invested as REAL)) from position where EmulatorRunId = @id and CloseTime is not null", new { id }) ?? 0.0;
        run.Invested = (decimal)invested;

        ComputeRunSummary(database, run);
        return true;
    }


    /// <summary>
    /// Fills the part of the run row that is DERIVED from its positions and therefore has to be
    /// computed while they are still there: peak capital and peak position count, the long/short
    /// split, the average winner and loser, the position durations, and the DCA breakdown.
    ///
    /// <para>
    /// Everything here is scoped to the positions that actually put money to work: closed, and with
    /// a positive Invested. A cancelled or timed-out entry never became a trade and would otherwise
    /// drag every average towards zero.
    /// </para>
    /// </summary>
    private static void ComputeRunSummary(CryptoDatabase database, CryptoEmulatorRun run)
    {
        int id = run.Id;

        // Peak capital: walk the open and close moments and keep the running total's high-water mark.
        var peak = database.Connection.QueryFirstOrDefault<PeakRow>(PeakExposureSql, new { id });
        run.PeakInvested = (decimal)(peak?.Money ?? 0.0);
        run.PeakPositions = (int)(peak?.Positions ?? 0.0);

        // Long and short kept apart. A short's stop sits nearer and its target further, so a
        // directional claim can only be made per side - the aggregate hides exactly that.
        run.PositionsLong = 0;
        run.PositionsShort = 0;
        run.ProfitLong = 0m;
        run.ProfitShort = 0m;
        foreach (var row in database.Connection.Query<SideRow>(
            "select Side, count(*) as Trades, sum(CAST(Profit as REAL)) as Profit from position " +
            "where " + TradedPositions + " group by Side", new { id }))
        {
            if (row.Side == (int)CryptoTradeSide.Long)
            {
                run.PositionsLong = row.Trades;
                run.ProfitLong = (decimal)row.Profit;
            }
            else
            {
                run.PositionsShort = row.Trades;
                run.ProfitShort = (decimal)row.Profit;
            }
        }

        // Mean winner and mean loser. With PositionsOpen they say what the run becomes if every
        // still-open position ends as the average winner, and as the average loser - which is how a
        // run with a lot of them has to be read, because the winners already closed at take profit
        // and what is left leans to the losing side.
        run.AverageWin = (decimal)(database.Connection.ExecuteScalar<double?>(
            "select avg(CAST(Profit as REAL)) from position where " + TradedPositions + " and CAST(Profit as REAL) > 0",
            new { id }) ?? 0.0);
        run.AverageLoss = (decimal)(database.Connection.ExecuteScalar<double?>(
            "select avg(CAST(Profit as REAL)) from position where " + TradedPositions + " and (Profit is null or CAST(Profit as REAL) <= 0)",
            new { id }) ?? 0.0);

        // Durations, so the Results grid no longer has to aggregate the whole Position table on every
        // refresh - and still shows them once the positions are archived away.
        var duration = database.Connection.QueryFirstOrDefault<DurationRow>(
            "select avg((julianday(CloseTime) - julianday(CreateTime)) * 86400) as AvgDurationSec, " +
            "       min((julianday(CloseTime) - julianday(CreateTime)) * 86400) as MinDurationSec, " +
            "       max((julianday(CloseTime) - julianday(CreateTime)) * 86400) as MaxDurationSec " +
            "from position where " + TradedPositions, new { id });
        run.AvgDurationSec = duration?.AvgDurationSec;
        run.MinDurationSec = duration?.MinDurationSec;
        run.MaxDurationSec = duration?.MaxDurationSec;

        // The DCA breakdown, on PartCount (the parts that actually FILLED) and not on ActiveDca -
        // that one is a bool saying a DCA order is still pending, and mixing the two up makes the
        // averaged-down positions look like they win every time.
        var breakdown = database.Connection.Query<CryptoDcaBucket>(
            "select PartCount as Parts, count(*) as Count, " +
            "       sum(case when CAST(Profit as REAL) > 0 then 1 else 0 end) as Won, " +
            "       sum(CAST(Profit as REAL)) as Profit, sum(CAST(Invested as REAL)) as Invested " +
            "from position where " + TradedPositions + " group by PartCount order by PartCount", new { id }).AsList();

        run.DcaBreakdownJson = breakdown.Count > 0 ? JsonSerializer.Serialize(breakdown) : null;

        // One line per position, built in Core so the migration backfills with the same code.
        run.PositionDigestJson = PositionDigest.Build(database, id);
    }




    /// <summary>
    /// The positions of one run that actually put money to work: closed, and with a positive
    /// Invested. A cancelled or timed-out entry never became a trade and would otherwise drag every
    /// average in the summary towards zero.
    /// </summary>
    internal const string TradedPositions =
        "EmulatorRunId = @id and CloseTime is not null and CAST(Invested as REAL) > 0";

    /// <summary>
    /// Peak capital and the peak number of simultaneously open positions, in one pass: turn every
    /// position into an open event and a close event, run a signed total over them in time order and
    /// keep the high-water mark of both.
    /// <para>
    /// The ordering carries the only real decision here. At an identical timestamp the closes go
    /// first, because <c>order by At, Amount</c> puts the negative deltas in front - a position that
    /// frees its money in the same minute another one takes it does not stack. That is the
    /// conservative reading, and it is what makes the answer stable when many positions open on the
    /// same candle.
    /// </para>
    /// </summary>
    internal const string PeakExposureSql =
        "select max(Money) as Money, max(Positions) as Positions from (" +
        "  select sum(Amount) over (order by At, Amount rows between unbounded preceding and current row) as Money, " +
        "         sum(One) over (order by At, Amount rows between unbounded preceding and current row) as Positions " +
        "  from (" +
        "    select CreateTime as At, CAST(Invested as REAL) as Amount, 1 as One from position where " + TradedPositions +
        "    union all " +
        "    select CloseTime as At, -CAST(Invested as REAL) as Amount, -1 as One from position where " + TradedPositions +
        "  )" +
        ")";

    /// <summary>Projection for the peak-exposure query; both aggregates come back as REAL.</summary>
    internal class PeakRow
    {
        public double? Money { get; set; }
        public double? Positions { get; set; }
    }


    /// <summary>Projection for the per-side totals.</summary>
    private class SideRow
    {
        public int Side { get; set; }
        public int Trades { get; set; }
        public double Profit { get; set; }
    }


    /// <summary>Projection for the duration aggregates; all three are null without closed positions.</summary>
    private class DurationRow
    {
        public double? AvgDurationSec { get; set; }
        public double? MinDurationSec { get; set; }
        public double? MaxDurationSec { get; set; }
    }


    /// <summary>
    /// Recomputes and stores the aggregates (counts, open/won/lost split, Profit and Invested) plus
    /// the run summary for the given runs from their current signals and positions. Use to backfill
    /// runs created before a stat column existed (e.g. Invested → the Profit % column, or the whole
    /// summary added in database version 91) or after positions were edited. Does NOT touch
    /// FinishedAt/Result/config.
    ///
    /// <para>
    /// Runs whose positions have been archived away are LEFT ALONE - see <see cref="CanRecalculate"/>.
    /// Returns how many were updated and how many were skipped for that reason, so the caller can say
    /// so rather than reporting a smaller number without explanation.
    /// </para>
    /// </summary>
    public static (int Updated, int Skipped) RecalculateRuns(IEnumerable<int> runIds,
        IProgress<(int Done, int Total)>? progress = null)
    {
        using var database = new CryptoDatabase();
        database.Open();

        // Materialised so the total is known up front: backfilling a whole session reads every
        // position of every run and writes a digest per run, which takes long enough that a caller
        // without a count has nothing to show but a frozen window.
        List<int> ids = runIds.ToList();

        int updated = 0;
        int skipped = 0;
        int done = 0;
        foreach (int id in ids)
        {
            var run = database.Connection.Get<CryptoEmulatorRun>(id);
            if (run != null)
            {
                if (ComputeRunStats(database, run))
                {
                    database.Connection.Update(run);
                    updated++;
                }
                else
                {
                    skipped++;
                }
            }
            progress?.Report((++done, ids.Count));
        }
        return (updated, skipped);
    }


    /// <summary>
    /// What <see cref="PurgeBeforeRun"/> did, so the caller can log it instead of guessing.
    /// </summary>
    /// <param name="Purged">True when the tables were cleared, false when the check below refused.</param>
    /// <param name="Unprotected">
    /// Runs that still hold positions but have no digest. As long as there is one, NOTHING is
    /// cleared: deleting those positions would throw away what no column can give back. The
    /// database migration fills the digests, so this only ever fires after a run that never
    /// reached FinishRun.
    /// </param>
    public readonly record struct PurgeResult(bool Purged, int Unprotected);


    /// <summary>
    /// Clears the emulator's bulk data at the START of a run: positions, their parts and steps,
    /// signals, zones, orders, trades and the paper assets. Everything, so the database stays the
    /// size of one run instead of growing with every entry of a ten-day queue - and so what is in
    /// it is always the run you are looking at.
    /// <para>
    /// Whole tables rather than a delete per run: DROP TABLE deallocates the B-tree pages in one
    /// step where a filtered delete walks millions of rows, and CreateTables puts the empty tables
    /// back. Nothing of value is in them by then - the counters, the summary and the position
    /// digest are on the EmulatorRun row, which is not touched. Asset is rebuilt at the start of
    /// every replay anyway (ReplayRunner calls PaperAssets.ResetAssets).
    /// </para>
    /// </summary>
    public static PurgeResult PurgeBeforeRun()
    {
        using var database = new CryptoDatabase();
        database.Open();

        // The rail the archive of 30-08-2026 did not have: it was copied while a run was still
        // going, and that run lost its numbers with no way back. One run without a digest is
        // enough to leave everything alone - the point of the check is not to lose data, and
        // clearing "all but that one" would only hide the problem.
        int unprotected = database.Connection.ExecuteScalar<int>(
            "select count(*) from EmulatorRun r " +
            "where (r.PositionDigestJson is null or r.PositionDigestJson = '') " +
            "  and exists (select 1 from Position p where p.EmulatorRunId = r.Id)");
        if (unprotected > 0)
            return new PurgeResult(false, unprotected);

        // Foreign keys are ENFORCED on these connections, and with them on a DROP TABLE is not the
        // cheap page-free it looks like: SQLite turns it into a DELETE of every row plus a
        // constraint check per row. Measured on a 292 MB parent table with 1,2 million children:
        // seconds with them off, still running after ten minutes with them on.
        //
        // Restored in the finally, and to what it WAS rather than blindly to ON: Microsoft.Data.Sqlite
        // pools the native handle per connection string (see the note in CryptoDatabase.Open), so a
        // pragma left behind here travels to whoever leases that handle next.
        int previous = database.Connection.ExecuteScalar<int>("PRAGMA foreign_keys");
        try
        {
            database.Connection.Execute("PRAGMA foreign_keys = OFF");

            foreach (string table in new[]
                     { "[Asset]", "[Trade]", "[Order]", "PositionStep", "PositionPart", "Position", "Signal", "Zone" })
            {
                database.Connection.Execute($"DROP TABLE IF EXISTS {table}");
            }
            CryptoDatabase.CreateTables(database);
        }
        finally
        {
            database.Connection.Execute($"PRAGMA foreign_keys = {previous}");
        }

        return new PurgeResult(true, 0);
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
    public static (int Updated, int Skipped) RecalculateAllRuns()
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
                    "delete from AssetSnapshot where EmulatorRunId = @id", new { id = runId }, transaction);
                database.Connection.Execute(
                    "delete from AssetAdjustment where EmulatorRunId = @id", new { id = runId }, transaction);
                // BarometerSnapshot arrived with the barometer-in-the-emulator change and also
                // references EmulatorRun; without this the delete fails on "FOREIGN KEY constraint
                // failed" and rolls the whole thing back.
                database.Connection.Execute(
                    "delete from BarometerSnapshot where EmulatorRunId = @id", new { id = runId }, transaction);
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
            database.Connection.Execute(
                "delete from AssetSnapshot where EmulatorRunId is not null", transaction: transaction);
            database.Connection.Execute(
                "delete from AssetAdjustment where EmulatorRunId is not null", transaction: transaction);
            database.Connection.Execute(
                "delete from BarometerSnapshot where EmulatorRunId is not null", transaction: transaction);
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
