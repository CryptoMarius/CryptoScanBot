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

    // Sub-breakdown of the PrepareTicks ("indicators") bucket, accumulated inside
    // CryptoIndicatorDataList.CalculateIndicators / PrepareIndicators. Tells us whether the indicator
    // cost is the candle-list building, the Skender batch math, the per-candle fill loop, or Lux —
    // which decides whether an incremental rewrite is even needed or just cheaper bookkeeping.
    public static long PrepCollectTicks;   // CollectCandles (build the 260-candle history list)
    public static long PrepSkenderTicks;   // the Skender GetXxx(...) batch calls
    public static long PrepFillTicks;      // the loop filling CryptoData per candle
    public static long PrepLuxTicks;       // LuxIndicator.Calculate (recursive RMA)

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
    public static void Reset()
    {
        PrepareTicks = 0;
        ExecuteTicks = 0;
        TradeTicks = 0;
        PositionCheckTicks = 0;
        CandleArrivals = 0;

        PrepCollectTicks = 0;
        PrepSkenderTicks = 0;
        PrepFillTicks = 0;
        PrepLuxTicks = 0;

        SeStrategyTicks = 0;
        SeZoneTouchTicks = 0;
        SeEvaluations = 0;
        SeSignals = 0;

        TrendTicks = 0;
        TrendCalls = 0;
        FvgInlineTicks = 0;
        SmcInlineTicks = 0;

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

    /// <summary>Adds the Skender / fill-loop / Lux sub-buckets of the indicator phase.</summary>
    public static void RecordIndicatorPhases(long skender, long fill, long lux)
    {
        if (!Enabled)
            return;
        Interlocked.Add(ref PrepSkenderTicks, skender);
        Interlocked.Add(ref PrepFillTicks, fill);
        Interlocked.Add(ref PrepLuxTicks, lux);
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
