using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Core.Emulator;

/// <summary>
/// Pre-fills a symbol's CandleList for an emulator run.
///
/// The emulator drives off the 1-minute interval and aggregates higher intervals itself via
/// <see cref="CandleTools.BulkCalculateCandles"/>, so only 1m candles are fetched from the
/// per-exchange candles.db. Enough 1m history is loaded BEFORE the replay window so the
/// longest indicator lookback (typically SMA200) on the longest active interval has at least
/// 260 candles to work with when the TickRunner emits its first replay candle.
/// </summary>
public static class IndicatorWarmup
{
    // CollectCandles in IndicatorData.cs requires at least 260 candles per interval; we add a
    // small safety margin so that aligned-bucket boundaries can never push us under.
    private const int MinCandlesPerInterval = 260;
    private const int SafetyExtraBars = 10;

    /// <summary>
    /// Delegates to <see cref="SignalPrepare.GetActiveIntervals"/> so the emulator never has
    /// to keep its own copy of Prepare's strategy / zone / forced-1m logic in sync. Prepare
    /// itself is the single source of truth for "which intervals does the engine maintain"
    /// — bootstrap calls SignalPrepare.Prepare(), and from there we just read the result.
    /// </summary>
    public static List<CryptoInterval> ResolveActiveIntervals()
        => SignalPrepare.GetActiveIntervals();


    /// <summary>
    /// The full set of intervals the emulator must MAINTAIN in memory during a replay: the active
    /// strategy + zone intervals from <see cref="ResolveActiveIntervals"/>, plus the intervals the
    /// trading-pause rules read (<c>Settings.Trading.PauseTradingRules</c> — by default BTCUSDT on
    /// 2m and 5m). Without the pause-rule intervals their reference symbol has no candles on those
    /// timeframes and <c>TradingRules.CalculateTradingRules</c> logs "Missing candles for
    /// tradingrules?" on every single tick.
    ///
    /// These extra intervals are synthesised from 1m by the warmup / TickRunner (never fetched), so
    /// only 1m needs to be on disk. The pause-rule's reference symbol must of course be one of the
    /// run symbols, otherwise it has no 1m candles to synthesise from in the first place.
    /// </summary>
    public static List<CryptoInterval> ResolveMaintainedIntervals()
    {
        var seen = new HashSet<CryptoIntervalPeriod>();
        var result = new List<CryptoInterval>();

        foreach (var interval in ResolveActiveIntervals())
        {
            if (seen.Add(interval.IntervalPeriod))
                result.Add(interval);
        }

        foreach (var rule in GlobalData.Settings.Trading.PauseTradingRules)
        {
            if (GlobalData.IntervalListPeriod.TryGetValue(rule.Interval, out CryptoInterval? interval)
                && seen.Add(interval.IntervalPeriod))
            {
                result.Add(interval);
            }
        }

        result.Sort((a, b) => a.Duration.CompareTo(b.Duration));
        return result;
    }


    /// <summary>
    /// How much history (in <em>that interval's own units, expressed in minutes</em>) the
    /// fetch routine should ensure is available before the replay window starts. The fetcher
    /// pulls candles per interval directly, so we no longer over-fetch 1m candles to cover
    /// a 1w warmup — the 1w interval pulls 270 weekly bars (about 5 years) in 1w-resolution
    /// instead of millions of 1m bars.
    /// <list type="bullet">
    ///   <item>1m gets a fixed-cap window (24 h) — typical 1m indicators reach back at most
    ///         a few hundred bars; a day of history is comfortably enough.</item>
    ///   <item>Every higher interval gets <see cref="MinCandlesPerInterval"/> + safety bars
    ///         worth of its own duration. SMA200 on 1d → ≈ 270 days; on 1w → ≈ 5 years.</item>
    /// </list>
    /// </summary>
    public static uint ComputeWarmupMinutes(CryptoInterval interval)
    {
        const uint OneMinuteWarmupMinutes = 24 * 60;  // 24 hours of 1m history
        if (interval.Duration <= 1)
            return OneMinuteWarmupMinutes;
        return (uint)((MinCandlesPerInterval + SafetyExtraBars) * interval.Duration);
    }


    /// <summary>
    /// Backward-compatible overload still used by <see cref="PrepareSymbol"/>: the warmup
    /// span the 1m CandleList must cover so the longest active higher interval can be
    /// reconstructed from 1m at run start. Use the per-interval overload for the fetch step.
    /// </summary>
    public static uint ComputeWarmupMinutes(IReadOnlyList<CryptoInterval> activeIntervals)
    {
        uint maxDuration = 1;
        foreach (var interval in activeIntervals)
        {
            if (interval.Duration > maxDuration)
                maxDuration = interval.Duration;
        }
        return (uint)((MinCandlesPerInterval + SafetyExtraBars) * maxDuration);
    }


    /// <summary>
    /// Loads all 1m candles for <paramref name="symbol"/> in the warmup+replay window from
    /// the per-exchange candles.db, injects them into the 1m CandleList, then aggregates the
    /// active higher intervals from the 1m candles via <see cref="CandleTools.BulkCalculateCandles"/>.
    /// Returns the replay-window 1m candles separately so the TickRunner can pop them one by
    /// one without removing anything from the CandleList.
    /// </summary>
    public static List<CryptoCandle> PrepareSymbol(CryptoSymbol symbol,
        CandleTime replayFrom, CandleTime replayTo)
    {
        var activeIntervals = ResolveMaintainedIntervals();
        uint warmupMinutes = ComputeWarmupMinutes(activeIntervals);

        // Clamp warmupFrom to 0 if it would go negative (when replayFrom is very early).
        CandleTime warmupFrom = replayFrom.Minutes > warmupMinutes
            ? new CandleTime(replayFrom.Minutes - warmupMinutes)
            : new CandleTime(0);

        if (!GlobalData.IntervalListPeriodName.TryGetValue("1m", out CryptoInterval? interval1m))
            throw new InvalidOperationException("1m interval not registered in GlobalData.IntervalListPeriodName");

        // Pull the entire 1m range in one DB pass — cheaper than two queries.
        var all1m = CandleSource.Load(symbol, interval1m, warmupFrom, replayTo);
        var symbolInterval1m = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);

        var replayCandles = new List<CryptoCandle>();
        foreach (var candle in all1m)
        {
            // Warmup candles go into the CandleList immediately so indicators can build state.
            // Replay candles will be added one-by-one by the TickRunner so the strategy sees
            // exactly the same incremental view the live scanner sees.
            if (candle.OpenTime.Minutes < replayFrom.Minutes)
                symbolInterval1m.CandleList.TryAdd(candle.OpenTime, candle);
            else
                replayCandles.Add(candle);
        }

        if (symbolInterval1m.CandleList.Count > 0)
        {
            symbolInterval1m.LastCandle = symbolInterval1m.CandleList.Values.Last();
        }

        // Aggregate higher intervals from the 1m candles already in the CandleList. We aggregate
        // up to replayFrom (exclusive) so the higher-interval CandleLists end one bar short of
        // the first replay candle — the TickRunner will produce that bar at the appropriate
        // close-time during the replay.
        foreach (var higher in activeIntervals)
        {
            if (higher.IntervalPeriod == CryptoIntervalPeriod.interval1m)
                continue;

            CandleTools.BulkCalculateCandles(symbol, interval1m, higher, replayFrom);
        }

        return replayCandles;
    }
}
