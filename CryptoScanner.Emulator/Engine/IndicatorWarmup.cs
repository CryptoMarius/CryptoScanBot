using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Warms up a symbol's CandleList for an emulator run by replaying the pre-replay 1m candles
/// through the live 1m handler (<see cref="CandleTools.Process1mCandleAsync"/>), so the candle
/// history is built incrementally and identically to the actual replay.
///
/// Only 1m candles are read from the per-exchange candles.db; every higher timeframe is
/// synthesised from 1m. Enough 1m history is loaded BEFORE the replay window so the longest
/// indicator lookback (typically SMA200) on the longest active interval has at least 260 candles
/// of its own resolution — for a higher interval that necessarily means many more than 260 1m
/// candles (e.g. SMA200 on 1h needs ~200×60 = 12 000 1m candles to build the 200 hourly bars).
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


    ///// <summary>
    ///// Backward-compatible overload still used by <see cref="PrepareSymbol"/>: the warmup
    ///// span the 1m CandleList must cover so the longest active higher interval can be
    ///// reconstructed from 1m at run start. Use the per-interval overload for the fetch step.
    ///// </summary>
    //public static uint ComputeWarmupMinutes(IReadOnlyList<CryptoInterval> activeIntervals)
    //{
    //    uint maxDuration = 1;
    //    foreach (var interval in activeIntervals)
    //    {
    //        if (interval.Duration > maxDuration)
    //            maxDuration = interval.Duration;
    //    }
    //    return (uint)((MinCandlesPerInterval + SafetyExtraBars) * maxDuration);
    //}


    /// <summary>
    /// Warms a symbol up for the replay. For EVERY interval (1m and all higher ones) it loads the
    /// last <see cref="MinCandlesPerInterval"/>+ candles ending just before <paramref name="replayFrom"/>
    /// straight from the per-exchange candles.db into that interval's own CandleList. Reading is
    /// almost free (the candles were already stored by "Fetch candles"), and loading each interval at
    /// its OWN resolution avoids the absurd cost of rebuilding, say, a single 1w bar from ~2 million
    /// 1m candles. Each interval therefore has real history for SMA200 (≥200 bars) and for the day/
    /// week bars that rely on it.
    ///
    /// Only the 1m interval gets a replay list — the candles fed one minute at a time during the
    /// replay. The higher intervals are EXTENDED during the replay by
    /// <see cref="CandleTools.Process1mCandleAsync"/>, which appends each newly-closed higher bar
    /// onto the DB-loaded history. No higher candle straddling replayFrom is loaded (only bars that
    /// close at or before replayFrom), so the strategy never sees a future-containing bar.
    /// </summary>
    public static CryptoCandleList PrepareSymbol(CryptoSymbol symbol,
        CandleTime replayFrom, CandleTime replayTo)
    {

        symbol.ClearCandles();

        if (!GlobalData.IntervalListPeriodName.TryGetValue("1m", out CryptoInterval? interval1m))
            throw new InvalidOperationException("1m interval not registered in GlobalData.IntervalListPeriodName");

        // Ascending order so an interval's ConstructFrom is already warmed when (rarely) we have to
        // synthesise a chain interval the DB does not contain.
        var dlzSettings = GlobalData.Settings.Signal.ZonesDlz;

        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            // Candles of EACH interval's own resolution to load before replayFrom: enough for SMA200
            // (200) plus a safety margin, and enough history to make a day/week bar meaningful.
            // For DLZ-enabled intervals the zone depth (CandleCount) can be much larger.
            int depth = 270;
            if (dlzSettings.IntervalList.Contains(interval.Name) && dlzSettings.CandleCount > depth)
                depth = dlzSettings.CandleCount;
            CandleTime from = new(replayFrom.Minutes - (uint)depth * interval.Duration);

            //CandleTime lastWarmup = new(replayTo.Minutes);
            //if (interval.IntervalPeriod > CryptoIntervalPeriod.interval1m)
            //    lastWarmup = new(replayFrom.Minutes);

            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            foreach (CryptoCandle candle in CandleSource.Load(symbol, interval, from, replayFrom))
            {
                // Guard against look-ahead: only bars that fully close at/before replayFrom.
                if (candle.OpenTime + interval.Duration <= replayFrom)
                    symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);
            }

            // Fallback for a chain interval the DB happens not to have (e.g. an intermediate 5m that
            // "Fetch candles" never pulled): rebuild its warmup from the already-warmed ConstructFrom.
            // Cheap — it aggregates from the immediate lower interval, never straight from 1m.
            //if (interval.ConstructFrom != null && symbolInterval.CandleList.Count == 0)
            //    CandleTools.BulkCalculateCandles(symbol, interval.ConstructFrom, interval, replayFrom);
        }

        // Only the 1m interval is fed candle-by-candle during the replay. Set its window aside,
        // keyed by OpenTime, so the TickRunner can look each minute up by candle-time.
        CryptoCandleList replayCandles = [];
        foreach (CryptoCandle candle in CandleSource.Load(symbol, interval1m, replayFrom, replayTo))
        {
            if (candle.OpenTime >= replayFrom)
                replayCandles.Add(candle.OpenTime, candle);
        }

        return replayCandles;
    }
}
