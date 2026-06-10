using System.Diagnostics;
using System.Threading;

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
}
