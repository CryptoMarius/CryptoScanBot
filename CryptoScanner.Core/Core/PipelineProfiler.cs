using System.Diagnostics;

namespace CryptoScanner.Core.Core;

/// <summary>
/// Opt-in, lightweight profiler for the per-candle scanner pipeline in
/// <c>PositionMonitor.NewCandleArrivedAsync</c>. It breaks that method's cost down into four buckets
/// so a run reveals where the dominant "pipeline" time actually goes:
/// <list type="bullet">
///   <item><see cref="PrepareTicks"/> — <c>SignalPrepare.Execute</c> (indicator calculation).</item>
///   <item><see cref="ExecuteTicks"/> — <c>SignalExecute.ExecuteAsync</c> (strategy algorithms,
///         which themselves touch some indicators).</item>
///   <item><see cref="TradeTicks"/> — paper-trade fills + trading rules + create/extend position.</item>
///   <item><see cref="PositionCheckTicks"/> — <c>ThreadCheckPosition.AddToQueue</c> (the
///         ThreadCheckFinishedPosition path; in emulator mode this runs synchronously and opens a DB
///         connection, so this bucket tells us whether optimising that is worthwhile).</item>
/// </list>
///
/// Disabled by default (<see cref="Enabled"/> is false), so the LIVE scanner is unaffected — the
/// only cost there is reading a few <see cref="Stopwatch.GetTimestamp"/> values per candle, which is
/// negligible, and <see cref="Record"/> returns immediately without accumulating. The emulator's
/// <c>TickRunner</c> turns it on at run start and reads the totals at the end. The replay processes
/// ticks one at a time on a single thread, so the plain <c>+=</c> accumulation needs no locking.
/// </summary>
public static class PipelineProfiler
{
    /// <summary>When false, <see cref="Record"/> is a no-op so the live scanner never accumulates.</summary>
    public static bool Enabled;

    public static long PrepareTicks;
    public static long ExecuteTicks;
    public static long TradeTicks;
    public static long PositionCheckTicks;

    /// <summary>How many NewCandleArrivedAsync bodies were fully measured (reached the Record call).</summary>
    public static long CandleArrivals;

    // Sub-breakdown of the PrepareTicks ("indicators") bucket. The Skender / fill-loop / Lux
    // counters that used to sit here belonged to the batch path, which no longer exists; the
    // incremental hub reports its own split through the Hub* counters below.
    public static long PrepCollectTicks;   // CollectCandles (build the history window)

    // The rest of the warm-up branch in PrepareViaHub, which PrepCollectTicks above only covered the
    // first step of. Everything after CollectCandles - a fresh IntervalIndicatorHub fed the whole
    // history one candle at a time, a BuildCurrent per candle, and the BandRangeTracker rebuild - was
    // measured by nothing at all, which is why the emulator report showed "indicators 36,638s" with
    // 159s attributed underneath it (run 229, 23-08-2026).
    //
    // PrepWarmupTicks contains PrepCollectTicks, PrepHubFeedTicks and PrepBandRangeTicks; the
    // remainder is bookkeeping between them.
    public static long PrepCalls;              // PrepareIndicators calls
    public static long PrepAlreadyPresent;     // returned immediately: Data already held this candle
    public static long PrepNotEnoughHistory;   // CollectCandles said no
    public static long PrepWarmupCalls;
    public static long PrepWarmupTicks;
    public static long PrepHubFeedTicks;       // the per-candle hub.Add + BuildCurrent + Data insert loop
    public static long PrepHubFeedCandles;     // how many candles that loop processed
    public static long PrepBandRangeTicks;     // BandRangeTracker.Build

    // WHY a warm-up was needed, which is the number that decides whether this is worth fixing: an
    // incremental hub that never gets to be incremental costs a full 260-candle rebuild per candle.
    // Tested in the order below, each call counted once for the first reason that applied.
    public static long PrepWarmupHubNull;      // no hub yet (or nothing added to it yet)
    public static long PrepWarmupGap;          // this candle does not directly follow the last one added
    public static long PrepWarmupExplicit;     // caller asked for a bigger window (chart)
    public static long PrepWarmupConfig;       // settings changed since the hub was built

    // Sub-breakdown of the ExecuteTicks ("algorithms") bucket, accumulated inside
    // SignalExecute.ExecuteAsync. Tells us whether the dominant SignalExecute time is normal-strategy
    // evaluation, zone-touch detection (FVG/DLZ/SMC), or scales with the number of signals created.
    // The "rest" (barometer + loop overhead) is ExecuteTicks − SeStrategyTicks − SeZoneTouchTicks.
    public static long SeStrategyTicks;    // ExecuteAlgorithmAsync for normal (barometer-checked) strategies
    public static long SeZoneTouchTicks;   // ExecuteAlgorithmAsync for the FVG/DLZ/SMC zone strategies
    public static long SeEvaluations;      // number of ExecuteAlgorithmAsync calls
    public static long SeSignals;          // how many of those produced a signal

    // Carve-outs: these overlap the buckets above (they are NOT additive to the total). They isolate
    // pieces that would otherwise stay hidden inside a larger bucket:
    //   • TrendTicks    — MarketTrend.CalculateMarketTrendAsync, called from within the strategy
    //                     algorithms, so it is part of SeStrategyTicks/SeZoneTouchTicks.
    //   • FvgInlineTicks — ZoneFvg.ScanForNew, run inside SignalPrepare (part of PrepareTicks).
    //   • SmcInlineTicks — ZoneSmc.Detect, run inside SignalPrepare (part of PrepareTicks).
    public static long TrendTicks;
    public static long TrendCalls;
    public static long FvgInlineTicks;
    public static long SmcInlineTicks;

    // DLZ carve-out, same idea as FvgInline/SmcInline above but bigger, and until 2026-08-22 the only
    // one missing. In the emulator SignalPrepare calls ZoneThreadCalculate.CalculateZones directly
    // (SignalPrepare.cs, the IsEmulatorMode branch), so every second DLZ spends lands in PrepareTicks
    // - the bucket the chunk report prints as "indicators". That made DLZ indistinguishable from the
    // indicator hub in a report where "indicators" was routinely 99% of the pipeline. The "zones"
    // bucket does not help: it measures the queue drain, which the emulator never uses, so it prints
    // 0.0 every chunk.
    //
    // DlzInlineTicks overlaps PrepareTicks and is NOT additive to it. The four phase counters below
    // do add up to DlzInlineTicks, minus the bookkeeping between them.
    public static long DlzInlineTicks;
    public static long DlzInlineCalls;

    // Phases inside one CalculateZonesAsync, so a slow recalculation can be attributed:
    //   • Feed   — LoadHistoricCandles + CalculatePivots: getting candles in and fed to the ZigZag.
    //   • Judge  — CalculateDlzAsync: the dominance verdicts, the zoom and the intro grading.
    //   • Merge  — the zone index, AddZonesToInternalLists, the retention/delete pass.
    //   • Broken — CheckAndMarkBrokenZones.
    public static long DlzFeedTicks;
    public static long DlzJudgeTicks;
    public static long DlzMergeTicks;
    public static long DlzBrokenTicks;

    // Which branch a recalculation took. The incremental branch is skipped whenever the candle marker
    // has fallen behind the window (ZoneDlz, the ProcessedCandleMarker >= minDate test), so a symbol
    // whose trigger fires less often than the window is long falls back to a complete rescan every
    // single time. These two counters are what shows whether that is happening in practice.
    public static long DlzFullRuns;
    public static long DlzIncrementalRuns;

    // Sub-breakdown of DlzJudgeTicks (CalculateDlzAsync). The walk itself covers every pivot in the
    // list to keep the previous/previous2 window intact, but only the ones outside the committed
    // region are actually judged - and judging one means zooming down the intervals until the zone is
    // narrow enough, which is where the candles come from. Counting the walk, the verdicts and the
    // zoom steps separately is what tells whether the cost is the NUMBER of pivots judged or what
    // judging one costs.
    public static long DlzPivotsWalked;
    public static long DlzPivotsSkipped;       // settled and already committed: no verdict recomputed
    public static long DlzPivotsJudged;        // MakeDominantAndZoomInAsync actually ran
    public static long DlzZoomTicks;           // MakeDominantAndZoomInAsync, total
    public static long DlzZoomSteps;           // how many intervals down the zoom walked, summed
    public static long DlzGradeTicks;          // GradeIntro

    // Candle reads from candles.db, wherever they come from. The zone engine is by far the biggest
    // caller, and until 23-08-2026 it read the COMPLETE series for an interval whatever window the
    // caller asked for - so this is the counter that says whether bounding the read actually landed.
    public static long CandleReadCalls;
    public static long CandleReadRows;
    public static long CandleReadTicks;

    // Holes the zone walks stepped over. Every zone loop reads its candles by key and a key that is
    // not in memory used to fall through the if - so a missing candle read as "nothing happened" and
    // said nothing about it. ZoneCandleGaps counts them as the existing loops walk; GapsInterrupted
    // is the subset longer than ZoneCandleGaps.ToleratedGap, i.e. the ones that can actually change
    // which zones survive. Refetches are the stretches that were read back in because the walk
    // started before the loaded window.
    public static long ZoneGapWalks;           // walks that found at least one missing candle
    public static long ZoneGapCandles;         // missing candles, summed over those walks
    public static long ZoneGapWorst;           // longest run of consecutive missing candles seen
    public static long ZoneGapInterrupted;     // walks whose longest run exceeded ToleratedGap
    public static long ZoneGapRefetches;       // EnsureHistoryLoadedAsync actually read something
    public static long ZoneGapRefetchCandles;  // candles those reads covered

    // Sub-breakdown of the hub incremental path (PrepareViaHub non-warmup).
    public static long HubAddTicks;
    public static long HubBuildTicks;
    public static long HubDataInsertTicks;
    public static long HubApplyLuxTicks;
    public static long HubIncrementalCalls;

    // Sub-breakdown of the TrendTicks carve-out, accumulated inside MarketTrend.CalculateMarketTrendAsync
    // / TrendCalculator.CalculateBothAsync / TrendTools.AddCandlesToIndicatorsAsync. Tells us whether the
    // dominant trend cost is the per-symbol lock wait, the candle ingestion into the ZigZag indicator
    // (and how many candles that actually processes per call — confirms whether the emulator-mode window
    // clamp in TrendCalculator is doing its job), or the Dow/BOS interpretation passes.
    public static long TrendLockWaitTicks;     // CalculateMarketTrendAsync: time blocked on symbol.Data.TrendLock
    public static long TrendCalcBothTicks;     // TrendCalculator.CalculateBothAsync, total (all exit paths)
    public static long TrendCalcBothCalls;     // number of CalculateBothAsync calls (one per stale interval)
    public static long TrendIngestTicks;       // AddCandlesToIndicatorsAsync: candle-lock wait + ingest loop + FinishBatch
    public static long TrendIngestCandles;     // total candles fed into the ZigZag indicator across all calls
    public static long TrendDowTicks;          // TrendInterval.InterpretZigZagPoints
    public static long TrendBosTicks;          // TrendIntervalBos.InterpretZigZagPoints

    // Sub-breakdown of the positionCheck bucket, accumulated inside
    // TradeTools.CalculatePositionResultsViaOrders. Tells us whether positionCheck's cost is the DB
    // load of orders/trades (only done once per position, then cached), the per-order processing loop
    // (fee calc, paper-asset bookkeeping, step status), the profit/break-even recalculation, or the
    // final DB persist (the transaction that updates step/part/position rows).
    public static long PosLoadOrdersTicks;   // LoadOrdersFromDatabaseAndExchangeAsync
    public static long PosOrderLoopTicks;    // the foreach order loop
    public static long PosCalcProfitTicks;   // CalculateProfitAndBreakEvenPrice
    public static long PosPersistTicks;      // the transaction block (Update part/step/position)
    public static long PosCalls;             // number of CalculatePositionResultsViaOrders calls

    // Sub-breakdown of PositionMonitor.CheckThePosition — the OTHER path inside the positionCheck
    // bucket (run via ThreadCheckFinishedPosition.ProcessPosition -> PositionOpenAsUsual, on every
    // candle that has an open position, NOT gated behind ForceCheckPosition like
    // CalculatePositionResultsViaOrders). Measurements showed CalculatePositionResultsViaOrders only
    // accounts for ~1% of the positionCheck bucket, so this is where the remaining cost almost
    // certainly sits: cancelling/repositioning stale orders, the DCA check, or placing/modifying the
    // live buy/sell orders.
    public static long CheckPosCancelTicks;  // CancelOrdersIfClosedOrTimeoutOrReposition
    public static long CheckPosDcaTicks;     // CheckAddDcaFixedPercentage
    public static long CheckPosHandleTicks;  // HandlePosition (place/modify orders, LockProfits)
    public static long CheckPosCalls;        // number of CheckThePosition calls

    // Cross-check on the two outer edges of NewCandleArrivedAsync's positionCheck bucket.
    // PcAddToQueueTicks wraps the exact same statement profPositionCheckStart/Record already time via
    // subtraction, so the two should match — a guard against an arithmetic slip in the diff-based
    // timestamps above. PcCleanCandleTicks covers the CandleTools.CleanCandleDataAsync tail that
    // previously ran AFTER PipelineProfiler.Record and so fell outside every bucket; it is gated
    // behind !IsEmulatorMode, so it stays 0 in emulator runs and only fires on the live scanner.
    public static long PcAddToQueueTicks;
    public static long PcCleanCandleTicks;

    // Diagnostic: trigger-price skip effectiveness in NewCandleArrivedAsync
    public static long SkipHasPosition;       // candles where a position exists
    public static long SkipTriggersNull;      // ...but triggers not yet set (Waiting status)
    public static long SkipForceCheck;        // ...but ForceCheckPosition is true
    public static long SkipPriceOutside;      // ...but candle crosses a boundary
    public static long SkipSuccess;           // candles actually skipped

    // Sub-breakdown of ThreadCheckFinishedPosition.ProcessPosition — the body AddToQueue runs
    // synchronously in emulator mode, i.e. what the positionCheck bucket above actually measures.
    // PositionResults and CheckThePosition (above) are only two of its branches; PpReadyTicks
    // (PositionReadyCancelAllOrderAndMove) was never instrumented, so this is what reveals whether
    // that uninstrumented branch is the source of the gap between positionCheck and those two.
    public static long PpTotalTicks;
    public static long PpCalls;
    public static long PpForceCheckTicks;    // TradeTools.CalculatePositionResultsViaOrders call site
    public static long PpStatusNewTicks;     // the status==New short-circuit (trace + return)
    public static long PpReadyTicks;         // PositionReadyCancelAllOrderAndMove
    public static long PpReadyCalls;
    public static long PpOpenAsUsualTicks;   // PositionOpenAsUsual
    public static long PpOpenAsUsualCalls;

    // Sub-breakdown of the actual database activity, accumulated inside ThreadSaveObjects.Flush() —
    // the emulator's per-tick synchronous persist (see PersistAndCalculateZonesAsync). Separate from
    // PosLoadOrdersTicks/PosPersistTicks (also real DB time, but nested inside the positionCheck bucket
    // instead) so TickRunner can report one consolidated "database total" line across the whole run.
    public static long DbFlushOpenTicks;     // CryptoDatabase.Open() (connection open + PRAGMA)
    public static long DbFlushWriteTicks;    // the foreach WriteObject loop (the actual Insert/Update/Delete calls)
    public static long DbFlushCommitTicks;   // transaction.Commit()
    public static long DbFlushCalls;         // number of non-empty Flush() calls
    public static long DbFlushItems;         // total queued objects written across all flushes


    /// <summary>Clears all counters. Call once at the start of a run before enabling.</summary>
    /// <summary>One zone walk that found holes; see <see cref="Zones.ZoneCandleGaps"/>.</summary>
    public static void RecordZoneCandleGap(int missing, int longestGap, bool interrupted)
    {
        if (!Enabled)
            return;
        ZoneGapWalks++;
        ZoneGapCandles += missing;
        if (longestGap > ZoneGapWorst)
            ZoneGapWorst = longestGap;
        if (interrupted)
            ZoneGapInterrupted++;
    }


    /// <summary>One stretch that was read back in because a walk started before the loaded window.</summary>
    public static void RecordZoneCandleRefetch(int candles)
    {
        if (!Enabled)
            return;
        ZoneGapRefetches++;
        ZoneGapRefetchCandles += candles;
    }


    public static void Reset()
    {
        PrepareTicks = 0;
        ExecuteTicks = 0;
        TradeTicks = 0;
        PositionCheckTicks = 0;
        CandleArrivals = 0;

        ZoneGapWalks = 0;
        ZoneGapCandles = 0;
        ZoneGapWorst = 0;
        ZoneGapInterrupted = 0;
        ZoneGapRefetches = 0;
        ZoneGapRefetchCandles = 0;

        PrepCollectTicks = 0;
        PrepCalls = 0;
        PrepAlreadyPresent = 0;
        PrepNotEnoughHistory = 0;
        PrepWarmupCalls = 0;
        PrepWarmupTicks = 0;
        PrepHubFeedTicks = 0;
        PrepHubFeedCandles = 0;
        PrepBandRangeTicks = 0;
        PrepWarmupHubNull = 0;
        PrepWarmupGap = 0;
        PrepWarmupExplicit = 0;
        PrepWarmupConfig = 0;

        SeStrategyTicks = 0;
        SeZoneTouchTicks = 0;
        SeEvaluations = 0;
        SeSignals = 0;

        HubAddTicks = 0;
        HubBuildTicks = 0;
        HubDataInsertTicks = 0;
        HubApplyLuxTicks = 0;
        HubIncrementalCalls = 0;

        TrendTicks = 0;
        TrendCalls = 0;
        FvgInlineTicks = 0;
        SmcInlineTicks = 0;

        DlzInlineTicks = 0;
        DlzInlineCalls = 0;
        DlzFeedTicks = 0;
        DlzJudgeTicks = 0;
        DlzMergeTicks = 0;
        DlzBrokenTicks = 0;
        DlzFullRuns = 0;
        DlzIncrementalRuns = 0;
        DlzPivotsWalked = 0;
        DlzPivotsSkipped = 0;
        DlzPivotsJudged = 0;
        DlzZoomTicks = 0;
        DlzZoomSteps = 0;
        DlzGradeTicks = 0;

        CandleReadCalls = 0;
        CandleReadRows = 0;
        CandleReadTicks = 0;

        TrendLockWaitTicks = 0;
        TrendCalcBothTicks = 0;
        TrendCalcBothCalls = 0;
        TrendIngestTicks = 0;
        TrendIngestCandles = 0;
        TrendDowTicks = 0;
        TrendBosTicks = 0;

        PosLoadOrdersTicks = 0;
        PosOrderLoopTicks = 0;
        PosCalcProfitTicks = 0;
        PosPersistTicks = 0;
        PosCalls = 0;

        CheckPosCancelTicks = 0;
        CheckPosDcaTicks = 0;
        CheckPosHandleTicks = 0;
        CheckPosCalls = 0;

        PcAddToQueueTicks = 0;
        PcCleanCandleTicks = 0;
        SkipHasPosition = 0;
        SkipTriggersNull = 0;
        SkipForceCheck = 0;
        SkipPriceOutside = 0;
        SkipSuccess = 0;

        PpTotalTicks = 0;
        PpCalls = 0;
        PpForceCheckTicks = 0;
        PpStatusNewTicks = 0;
        PpReadyTicks = 0;
        PpReadyCalls = 0;
        PpOpenAsUsualTicks = 0;
        PpOpenAsUsualCalls = 0;

        DbFlushOpenTicks = 0;
        DbFlushWriteTicks = 0;
        DbFlushCommitTicks = 0;
        DbFlushCalls = 0;
        DbFlushItems = 0;
    }


    // All Record* methods are thread-safe (Interlocked), because the emulator processes the symbols
    // of one minute in parallel — multiple threads accumulate into these counters at once. Each is a
    // no-op unless Enabled, so the live scanner pays nothing.

    /// <summary>Adds one candle's per-phase Stopwatch-tick deltas (NewCandleArrivedAsync buckets).</summary>
    public static void Record(long prepare, long execute, long trade, long positionCheck)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref PrepareTicks, prepare);
        Interlocked.Add(ref ExecuteTicks, execute);
        Interlocked.Add(ref TradeTicks, trade);
        Interlocked.Add(ref PositionCheckTicks, positionCheck);
        Interlocked.Increment(ref CandleArrivals);
    }

    /// <summary>Adds the CollectCandles sub-bucket of the indicator phase.</summary>
    public static void RecordPrepCollect(long collect)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref PrepCollectTicks, collect);
    }


    /// <summary>One PrepareIndicators call, and how far it got.</summary>
    public static void RecordPrepCall(bool alreadyPresent)
    {
        if (!Enabled)
            return;
        Interlocked.Increment(ref PrepCalls);
        if (alreadyPresent)
            Interlocked.Increment(ref PrepAlreadyPresent);
    }


    /// <summary>Why this call could not continue the hub incrementally. Counted once per warm-up.</summary>
    public static void RecordPrepWarmupReason(bool hubNull, bool gap, bool explicitWindow, bool configChanged)
    {
        if (!Enabled)
            return;
        if (hubNull)
            Interlocked.Increment(ref PrepWarmupHubNull);
        else if (gap)
            Interlocked.Increment(ref PrepWarmupGap);
        else if (explicitWindow)
            Interlocked.Increment(ref PrepWarmupExplicit);
        else if (configChanged)
            Interlocked.Increment(ref PrepWarmupConfig);
    }


    /// <summary>One completed warm-up: the whole branch, the feed loop inside it and the band-range rebuild.</summary>
    public static void RecordPrepWarmup(long total, long hubFeed, long candles, long bandRange)
    {
        if (!Enabled)
            return;
        Interlocked.Increment(ref PrepWarmupCalls);
        Interlocked.Add(ref PrepWarmupTicks, total);
        Interlocked.Add(ref PrepHubFeedTicks, hubFeed);
        Interlocked.Add(ref PrepHubFeedCandles, candles);
        Interlocked.Add(ref PrepBandRangeTicks, bandRange);
    }


    /// <summary>A warm-up that stopped because there was not enough history.</summary>
    public static void RecordPrepNotEnoughHistory()
    {
        if (!Enabled)
            return;
        Interlocked.Increment(ref PrepNotEnoughHistory);
    }


    /// <summary>One read of candles out of candles.db: how long it took and how many rows it produced.</summary>
    public static void RecordCandleRead(long ticks, long rows)
    {
        if (!Enabled)
            return;
        Interlocked.Increment(ref CandleReadCalls);
        Interlocked.Add(ref CandleReadRows, rows);
        Interlocked.Add(ref CandleReadTicks, ticks);
    }


    /// <summary>One walked pivot in CalculateDlzAsync, and whether its verdict was recomputed.</summary>
    public static void RecordDlzPivot(bool skipped, bool judged)
    {
        if (!Enabled)
            return;
        Interlocked.Increment(ref DlzPivotsWalked);
        if (skipped)
            Interlocked.Increment(ref DlzPivotsSkipped);
        if (judged)
            Interlocked.Increment(ref DlzPivotsJudged);
    }


    /// <summary>One MakeDominantAndZoomInAsync call, measured around it by the caller.</summary>
    public static void RecordDlzZoom(long ticks)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref DlzZoomTicks, ticks);
    }


    /// <summary>One step down the intervals inside a zoom. Counted where it happens, because the
    /// loop leaves as soon as the zone is narrow enough and the number of steps is the point.</summary>
    public static void RecordDlzZoomStep()
    {
        if (!Enabled)
            return;
        Interlocked.Increment(ref DlzZoomSteps);
    }


    /// <summary>One GradeIntro call.</summary>
    public static void RecordDlzGrade(long ticks)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref DlzGradeTicks, ticks);
    }

    /// <summary>Adds one hub incremental call's sub-phase ticks.</summary>
    public static void RecordHubIncremental(long hubAdd, long hubBuild, long dataInsert, long applyLux)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref HubAddTicks, hubAdd);
        Interlocked.Add(ref HubBuildTicks, hubBuild);
        Interlocked.Add(ref HubDataInsertTicks, dataInsert);
        Interlocked.Add(ref HubApplyLuxTicks, applyLux);
        Interlocked.Increment(ref HubIncrementalCalls);
    }


    /// <summary>Records one ExecuteAlgorithmAsync call: time + whether it was a normal strategy
    /// (checkBarometer) or a zone-touch, and whether it produced a signal.</summary>
    public static void RecordSignalExecuteCall(long algoTicks, bool checkBarometer, bool signalCreated)
    {
        if (!Enabled)
            return;
        if (checkBarometer)
            Interlocked.Add(ref SeStrategyTicks, algoTicks);
        else
            Interlocked.Add(ref SeZoneTouchTicks, algoTicks);
        Interlocked.Increment(ref SeEvaluations);
        if (signalCreated)
            Interlocked.Increment(ref SeSignals);
    }

    /// <summary>Records one trend calculation (CalculateMarketTrendAsync).</summary>
    public static void RecordTrend(long ticks)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref TrendTicks, ticks);
        Interlocked.Increment(ref TrendCalls);
    }

    /// <summary>Adds the lock-wait time CalculateMarketTrendAsync spends blocked on symbol.Data.TrendLock,
    /// before it gets to do (or skip, if cached) any actual recompute.</summary>
    public static void RecordTrendLockWait(long ticks)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref TrendLockWaitTicks, ticks);
    }

    /// <summary>Adds one TrendCalculator.CalculateBothAsync call (one per stale interval inside a
    /// CalculateMarketTrendAsync recompute) — total time regardless of exit path.</summary>
    public static void RecordTrendCalcBoth(long ticks)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref TrendCalcBothTicks, ticks);
        Interlocked.Increment(ref TrendCalcBothCalls);
    }

    /// <summary>Adds one TrendTools.AddCandlesToIndicatorsAsync call: candle-lock wait + the ingest
    /// loop (indicator.Calculate per candle) + FinishBatch, plus how many candles it actually fed in —
    /// confirms whether the emulator-mode window clamp keeps this bounded.</summary>
    public static void RecordTrendIngest(long ticks, long candleCount)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref TrendIngestTicks, ticks);
        Interlocked.Add(ref TrendIngestCandles, candleCount);
    }

    /// <summary>Adds one TrendInterval.InterpretZigZagPoints (Dow theory) call.</summary>
    public static void RecordTrendDow(long ticks)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref TrendDowTicks, ticks);
    }

    /// <summary>Adds one TrendIntervalBos.InterpretZigZagPoints (BOS/CHoCH) call.</summary>
    public static void RecordTrendBos(long ticks)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref TrendBosTicks, ticks);
    }

    /// <summary>Adds the inline FVG (ScanForNew) carve-out.</summary>
    public static void RecordFvgInline(long ticks)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref FvgInlineTicks, ticks);
    }

    /// <summary>Adds the inline SMC (Detect) carve-out.</summary>
    public static void RecordSmcInline(long ticks)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref SmcInlineTicks, ticks);
    }

    /// <summary>
    /// Adds one whole DLZ recalculation (ZoneDlz.CalculateZonesAsync) to the carve-out. Measured
    /// around the call in ZoneThreadCalculate rather than in SignalPrepare, so the FVG call sitting
    /// next to it stays out of the number and both call routes - the emulator's direct one and the
    /// live queue drain - are covered by the same measurement.
    /// </summary>
    public static void RecordDlzInline(long ticks)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref DlzInlineTicks, ticks);
        Interlocked.Increment(ref DlzInlineCalls);
    }

    /// <summary>
    /// Adds the per-phase split of one DLZ recalculation, plus which branch it took.
    /// See the counters for what each phase covers.
    /// </summary>
    public static void RecordDlzPhases(long feed, long judge, long merge, long broken, bool incremental)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref DlzFeedTicks, feed);
        Interlocked.Add(ref DlzJudgeTicks, judge);
        Interlocked.Add(ref DlzMergeTicks, merge);
        Interlocked.Add(ref DlzBrokenTicks, broken);
        if (incremental)
            Interlocked.Increment(ref DlzIncrementalRuns);
        else
            Interlocked.Increment(ref DlzFullRuns);
    }

    /// <summary>Adds one CalculatePositionResultsViaOrders call's per-phase Stopwatch-tick deltas
    /// (the sub-breakdown of the positionCheck bucket).</summary>
    public static void RecordPositionResultPhases(long loadOrders, long orderLoop, long calcProfit, long persist)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref PosLoadOrdersTicks, loadOrders);
        Interlocked.Add(ref PosOrderLoopTicks, orderLoop);
        Interlocked.Add(ref PosCalcProfitTicks, calcProfit);
        Interlocked.Add(ref PosPersistTicks, persist);
        Interlocked.Increment(ref PosCalls);
    }

    /// <summary>Adds one CheckThePosition call's per-phase Stopwatch-tick deltas (the sub-breakdown
    /// of the positionCheck bucket's "other" path, alongside CalculatePositionResultsViaOrders).</summary>
    public static void RecordCheckPositionPhases(long cancel, long dca, long handle)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref CheckPosCancelTicks, cancel);
        Interlocked.Add(ref CheckPosDcaTicks, dca);
        Interlocked.Add(ref CheckPosHandleTicks, handle);
        Interlocked.Increment(ref CheckPosCalls);
    }

    /// <summary>Cross-check wrap of the exact statement the positionCheck bucket already times via
    /// subtraction (see <see cref="Record"/>) — the two totals should match.</summary>
    public static void RecordAddToQueue(long ticks)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref PcAddToQueueTicks, ticks);
    }

    /// <summary>Wraps the CandleTools.CleanCandleDataAsync tail of NewCandleArrivedAsync, which runs
    /// AFTER <see cref="Record"/> and previously fell outside every bucket.</summary>
    public static void RecordCleanCandle(long ticks)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref PcCleanCandleTicks, ticks);
    }

    /// <summary>Adds one ThreadCheckFinishedPosition.ProcessPosition call's total duration plus its
    /// branch breakdown (the body the positionCheck bucket measures in emulator mode).</summary>
    public static void RecordProcessPosition(long total, long forceCheck, long statusNew, long ready, long openAsUsual)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref PpTotalTicks, total);
        Interlocked.Increment(ref PpCalls);
        Interlocked.Add(ref PpForceCheckTicks, forceCheck);
        Interlocked.Add(ref PpStatusNewTicks, statusNew);
        if (ready > 0)
        {
            Interlocked.Add(ref PpReadyTicks, ready);
            Interlocked.Increment(ref PpReadyCalls);
        }
        if (openAsUsual > 0)
        {
            Interlocked.Add(ref PpOpenAsUsualTicks, openAsUsual);
            Interlocked.Increment(ref PpOpenAsUsualCalls);
        }
    }

    /// <summary>Adds one ThreadSaveObjects.Flush() call's per-phase Stopwatch-tick deltas — the
    /// emulator's actual per-tick database write activity.</summary>
    public static void RecordDbFlush(long open, long write, long commit, long itemCount)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref DbFlushOpenTicks, open);
        Interlocked.Add(ref DbFlushWriteTicks, write);
        Interlocked.Add(ref DbFlushCommitTicks, commit);
        Interlocked.Increment(ref DbFlushCalls);
        Interlocked.Add(ref DbFlushItems, itemCount);
    }
}
