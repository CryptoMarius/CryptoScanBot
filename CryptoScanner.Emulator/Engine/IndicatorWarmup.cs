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
    /// How many candles of history to keep per interval, both for the warmup and for the pruning
    /// between chunks. The two MUST use the same number: pruning to less than the warmup loaded
    /// silently removes history that the engine assumed was there.
    ///
    /// The 1m interval is a special case. Indicators need a few hundred bars, but two consumers
    /// read back a full day or more: the barometer, and SignalCreate.CalculateLastPeriodsInInterval
    /// (the 24-hour change, which invalidates a signal when it falls outside the configured range).
    /// The live scanner reserves 24h + barometer hours for exactly that reason, so we take the same
    /// number here instead of a second, lower one — at 270 the 24-hour lookup fails right after
    /// every chunk boundary and silently falls back to a candle a few hours old.
    /// </summary>
    public static int WarmupDepth(CryptoInterval interval)
    {
        int depth = MinCandlesPerInterval + SafetyExtraBars;
        if (interval.Duration <= 1)
        {
            int barometerDepth = (int)CandleTools.CandleCountFetch1m + SafetyExtraBars;
            if (barometerDepth > depth)
                depth = barometerDepth;
        }

        // The zones reach back CandleTools.CandleCountFetch candles, the same depth as the rest of
        // the engine since 2026-08-22 - they no longer carry a setting of their own. Still asked per
        // interval because only the intervals DLZ actually runs on need that depth.
        var dlzSettings = GlobalData.Settings.Signal.ZonesDlz;
        if (dlzSettings.IntervalList.Contains(interval.Name) && CandleTools.CandleCountFetch > depth)
            depth = CandleTools.CandleCountFetch;

        // The short filter reads 1h support levels over the last 300 candles plus their pivots and
        // ATR; the live scanner has 500 1h candles for it, so the emulator keeps the same.
        if (interval.IntervalPeriod == CryptoIntervalPeriod.interval1h && CandleTools.CandleCountFetch > depth
            && Core.Trend.SupportResistanceCandles.ShortFilterActive())
            depth = CandleTools.CandleCountFetch;
        return depth;
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
    /// Warmup only: clears candle state and loads indicator history for all intervals from the DB.
    /// Does NOT load replay candles — call <see cref="LoadReplayCandles"/> separately.
    /// </summary>
    public static void WarmupSymbol(CryptoSymbol symbol, CandleTime replayFrom)
    {
        symbol.ClearCandles();

        if (!GlobalData.IntervalListPeriodName.TryGetValue("1m", out CryptoInterval? _))
            throw new InvalidOperationException("1m interval not registered in GlobalData.IntervalListPeriodName");

        // Ascending order so an interval's ConstructFrom is already warmed when (rarely) we have to
        // synthesise a chain interval the DB does not contain.
        var dlzSettings = GlobalData.Settings.Signal.ZonesDlz;

        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            // Candles of EACH interval's own resolution to load before replayFrom: enough for SMA200
            // (200) plus a safety margin, and enough history to make a day/week bar meaningful.
            // For DLZ-enabled intervals the zone depth (CandleCount) can be much larger, and 1m
            // needs a full day plus the barometer window — WarmupDepth is the single source for all
            // three cases, shared with the between-chunk pruning so the two cannot drift apart.
            int depth = WarmupDepth(interval);
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

        // Start the run on the volume of the last day BEFORE it, not on whatever the most recent
        // "fetch symbols" left behind. Without this the first replayed day still decides on today's
        // volume, and on a symbol whose daily candles start later it would keep that value for the
        // whole run.
        EmulatorVolume.SeedFromWarmup(symbol);
    }


    /// <summary>
    /// Loads replay candles at the given base interval for the given window from the DB. The
    /// returned list is keyed by OpenTime so the TickRunner can look each step up by candle-time.
    /// Only candles at or after <paramref name="replayFrom"/> are included.
    /// </summary>
    public static CryptoCandleList LoadReplayCandles(CryptoSymbol symbol,
        CandleTime replayFrom, CandleTime replayTo, CryptoInterval? baseInterval = null)
    {
        CryptoInterval interval = baseInterval
            ?? GlobalData.IntervalListPeriodName.GetValueOrDefault("1m")
            ?? throw new InvalidOperationException("1m interval not registered in GlobalData.IntervalListPeriodName");

        CryptoCandleList replayCandles = [];
        foreach (CryptoCandle candle in CandleSource.Load(symbol, interval, replayFrom, replayTo))
        {
            if (candle.OpenTime >= replayFrom)
                replayCandles.Add(candle.OpenTime, candle);
        }

        return replayCandles;
    }
}
