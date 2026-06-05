using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

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
    /// Returns the union of active intervals across long+short configurations, ordered by
    /// duration. Used as the set of intervals the engine needs to maintain.
    /// </summary>
    public static List<CryptoInterval> ResolveActiveIntervals()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in GlobalData.Settings.Signal.Long.Interval)
            names.Add(name);
        foreach (var name in GlobalData.Settings.Signal.Short.Interval)
            names.Add(name);

        var result = new List<CryptoInterval>(names.Count);
        foreach (var name in names)
        {
            if (GlobalData.IntervalListPeriodName.TryGetValue(name, out CryptoInterval? interval))
                result.Add(interval);
        }
        result.Sort((a, b) => a.Duration.CompareTo(b.Duration));
        return result;
    }


    /// <summary>
    /// Computes how many minutes of 1m history are needed before <paramref name="replayFrom"/>
    /// so that every active interval has at least <see cref="MinCandlesPerInterval"/> aggregated
    /// candles available. Long active intervals (e.g. 1d) dominate this number.
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
        var activeIntervals = ResolveActiveIntervals();
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
