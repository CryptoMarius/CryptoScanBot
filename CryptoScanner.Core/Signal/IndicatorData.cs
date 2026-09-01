using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

using Skender.Stock.Indicators;

using System.Diagnostics;

namespace CryptoScanner.Core.Signal;

public static class IndicatorEngine
{
    /// <summary>
    /// Ensures indicator data exists for <paramref name="openTime"/> in the requested higher interval and
    /// returns it as a MyData (candle + indicator data). Used for the multi-timeframe (MTF/HTF) strategies.
    /// </summary>
    public static (bool success, CryptoSymbolInterval higherInterval, MyData? candle)
        CalculateIndicatorsForInterval(CryptoSymbol symbol, CryptoInterval interval,
            CandleTime openTime, CryptoIntervalPeriod higherIntervalPeriod)
    {
        CryptoSymbolInterval symbolHigherInterval = symbol.GetSymbolInterval(higherIntervalPeriod);
        CryptoInterval higherInterval = symbolHigherInterval.Interval;
        CandleTime targetStart = openTime;

        if (interval.IntervalPeriod != higherIntervalPeriod)
        {
            // Check the candle in the higher interval, is it present?
            var result = IntervalTools.StartOfIntervalCandle3(openTime, interval.Duration, higherInterval.Duration);
            if (!result.targetComplete)
                result.targetStart -= higherInterval.Duration;
            if (!symbolHigherInterval.CandleList.TryGetValue(result.targetStart, out CryptoCandle _))
                return (false, symbolHigherInterval, null);
            targetStart = result.targetStart;
        }

        // Calculate the indicators if needed
        if (!PrepareIndicators(symbol, higherInterval, targetStart))
            return (false, symbolHigherInterval, null);

        // Combine candle + indicator data from the (persistent) higher-interval state.
        if (!symbolHigherInterval.TryGetCandle(targetStart, out MyData? higherCandle))
            return (false, symbolHigherInterval, null);

        return (true, symbolHigherInterval, higherCandle);
    }


    /// <summary>
    /// Ensures <see cref="CryptoSymbolInterval.Data"/> holds the indicator data for
    /// <paramref name="candleOpenTime"/>, filled incrementally via the per-interval QuoteHub.
    /// Returns false when there is not enough history yet.
    /// <para>
    /// <paramref name="calculateCandles"/> asks for a bigger history window than the default 260
    /// (the chart overlays use it so slow indicators are warmed up for every displayed bar). It
    /// forces a full warm-up, because the incremental path only ever appends a single candle.
    /// </para>
    /// </summary>
    public static bool PrepareIndicators(CryptoSymbol symbol, CryptoInterval interval,
        CandleTime candleOpenTime, int calculateCandles = -1)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (symbolInterval.Data.ContainsKey(candleOpenTime))
        {
            PipelineProfiler.RecordPrepCall(alreadyPresent: true);
            return true;
        }

        PipelineProfiler.RecordPrepCall(alreadyPresent: false);
        return PrepareViaHub(symbol, interval, symbolInterval, candleOpenTime, calculateCandles);
    }


    /// <summary>
    /// Feed candles into the per-interval <see cref="IntervalIndicatorHub"/>. A full warm-up (the whole
    /// CollectCandles window) runs on first use, when the hub fell out of sync (a gap), or when an
    /// explicit larger window was requested; otherwise only the single new candle is fed. The latest
    /// CryptoData lands in Data[candleOpenTime].
    /// </summary>
    private static bool PrepareViaHub(CryptoSymbol symbol, CryptoInterval interval,
        CryptoSymbolInterval symbolInterval, CandleTime candleOpenTime, int calculateCandles = -1)
    {
        // Split out instead of written as one condition, because WHICH of them is true is the whole
        // question: a hub that is rebuilt on every candle costs a 260-candle warm-up per candle, and
        // the reason it cannot continue tells you whether that is fixable.
        bool hubNull = symbolInterval.IndicatorHub == null || symbolInterval.IndicatorHubLastAdded == null;
        bool gap = !hubNull && symbolInterval.IndicatorHubLastAdded!.Value + interval.Duration != candleOpenTime;
        bool explicitWindow = calculateCandles > 0;
        // Settings changed since this hub was built: its indicator parameters and its set of
        // plugin extensions are frozen at construction, so rebuild instead of feeding it further.
        bool configChanged = !hubNull && symbolInterval.IndicatorHub!.ConfigVersion != IndicatorConfiguration.Version;

        bool warmup = hubNull || gap || explicitWindow || configChanged;

        if (warmup)
        {
            PipelineProfiler.RecordPrepWarmupReason(hubNull, gap, explicitWindow, configChanged);
            long profWarmupStart = Stopwatch.GetTimestamp();

            long profCollectStart = Stopwatch.GetTimestamp();
            List<IQuote>? history = CollectCandles(symbol, interval, candleOpenTime, out _, calculateCandles);
            PipelineProfiler.RecordPrepCollect(Stopwatch.GetTimestamp() - profCollectStart);
            if (history == null)
            {
                PipelineProfiler.RecordPrepNotEnoughHistory();
                return false;
            }

            // The hub advances its Lux Multi-RSI over the candles it is fed, so on a 15m or 1h hub
            // that value is a Lux over 15m/1h candles — not the 5m value the field promises.
            // ApplyLux replaces it for the candle being analyzed; the warm-up candles behind it never
            // get that treatment, so clear it there rather than leave a wrong-timeframe number behind.
            bool is5m = symbolInterval.IntervalPeriod == CryptoIntervalPeriod.interval5m;

            long profFeedStart = Stopwatch.GetTimestamp();
            var hub = new IntervalIndicatorHub();
            foreach (IQuote quote in history)
            {
                hub.Add(quote);
                if (quote is CryptoCandle candle)
                {
                    CryptoData built = hub.BuildCurrent();
                    if (!is5m)
                        built.Lux5mValue = null;
                    lock (symbolInterval.Data)
                        symbolInterval.Data[candle.OpenTime] = built;
                }
            }
            long profFeedTicks = Stopwatch.GetTimestamp() - profFeedStart;
            symbolInterval.IndicatorHub = hub;
            symbolInterval.IndicatorHubLastAdded = candleOpenTime;
            symbolInterval.IndicatorHubAddCount = history.Count;

            // The band-range tracker is rebuilt alongside the hub, but from the candle list instead
            // of from this 260-candle warm-up window — its statistics need a few hundred more.
            long profBandStart = Stopwatch.GetTimestamp();
            symbolInterval.BandRange = BandRangeTracker.Build(symbolInterval, candleOpenTime);
            long profBandTicks = Stopwatch.GetTimestamp() - profBandStart;

            PipelineProfiler.RecordPrepWarmup(Stopwatch.GetTimestamp() - profWarmupStart,
                profFeedTicks, history.Count, profBandTicks);
        }
        else
        {
            if (!symbolInterval.CandleList.TryGetValue(candleOpenTime, out CryptoCandle candle))
                return false;

            long t0 = Stopwatch.GetTimestamp();
            symbolInterval.IndicatorHub!.Add(candle);
            long t1 = Stopwatch.GetTimestamp();
            CryptoData built = symbolInterval.IndicatorHub.BuildCurrent();
            long t2 = Stopwatch.GetTimestamp();
            lock (symbolInterval.Data)
                symbolInterval.Data[candleOpenTime] = built;
            long t3 = Stopwatch.GetTimestamp();
            symbolInterval.IndicatorHubLastAdded = candleOpenTime;
            symbolInterval.IndicatorHubAddCount++;

            if (symbolInterval.BandRange != null && built.Sma20 != null
                && built.BollingerBandsUpperBand != null && built.BollingerBandsLowerBand != null)
            {
                symbolInterval.BandRange.Add(candle, built.Sma20.Value,
                    built.BollingerBandsUpperBand.Value, built.BollingerBandsLowerBand.Value);
            }

            ApplyLux(symbol, symbolInterval, candleOpenTime);
            long t4 = Stopwatch.GetTimestamp();

            PipelineProfiler.RecordHubIncremental(t1 - t0, t2 - t1, t3 - t2, t4 - t3);
            return true;
        }

        ApplyLux(symbol, symbolInterval, candleOpenTime);
        return true;
    }

    // For the SMA 200 we want at least 200 + 60 (we calculate the last 60 entries)
    //private const int maxCandles = 260;

    /// <summary>
    /// Make a list of candles up to openTime with at least maxCandles(260) candles
    /// </summary>
    public static List<IQuote>? CollectCandles(CryptoSymbol symbol, CryptoInterval interval,
        CandleTime openTime, out string errorstr, int calculateCandles = -1)
    {
        // Retrieve the last candle in the requested interval
        CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandleList intervalCandles = symbolPeriod.CandleList;

        int maxCandles = calculateCandles > 0 ? calculateCandles : 260;
        if (intervalCandles.Count < maxCandles || intervalCandles.Count < 260)
        {
            errorstr = $"{symbol.Name} Not enough candles available for interval {interval.Name} count={intervalCandles.Count} requested={maxCandles}";
            return null;
        }

        // this would normally be enough, but we need to fill in the missing candles (afaics)
        //var x = intervalCandles.Values.TakeLast(maxCandles);

        // A fix for calculating indicators for the barometer symbol..
        uint duration = interval.Duration;
        if (symbol.IsBarometerSymbol())
            duration = 1; // alway's 1m!

        List<IQuote> candlesForHistory = [];

        // The time is already aligned (but it wont hurt to do it again)
        CandleTime periodEndTime = openTime - openTime % duration;
        CandleTime periodStartTime = periodEndTime - (maxCandles - 1) * duration;

        CryptoCandle candleLast = default;
        CandleTime candleLoop = periodStartTime;
        while (candleLoop <= periodEndTime)
        {
            if (intervalCandles.TryGetValue(candleLoop, out CryptoCandle candle))
            {
                candlesForHistory.Add(candle);
            }
            else
            {
                // Generate a dummy for the calculation
                if (candleLast.OpenTime != 0)
                {
                    // Through FitTickDecimals: the previous close can be a price the symbol's own tick
                    // size cannot express in the int a candle keeps its prices in, and then the setter
                    // throws instead of producing a dummy. Same synthesized flat candle as in
                    // SubscriptionKLineCachedTicker's flush timer, so it needs the same guard.
                    candle = new()
                    {
                        TickDecimals = CryptoCandle.FitTickDecimals(symbol.PriceDecimals,
                            candleLast.Close, candleLast.Close, candleLast.Close, candleLast.Close),
                        OpenTime = candleLoop,
                        Open = candleLast.Close,
                        Low = candleLast.Close,
                        High = candleLast.Close,
                        Close = candleLast.Close,
                        Volume = 0,
                    };
                    candlesForHistory.Add(candle);
                }
                //GlobalData.AddTextToLogTab(symbol.Name + " " + interval.Name + " Missing candle information (recreated) " + CandleTools.GetUnixDate(candleLoop.ToDateTime()).ToLocalTime());
            }

            candleLoop += duration;
            candleLast = candle;
        }


        // Its still possible we dont have enough candles
        if (candlesForHistory.Count < maxCandles)
        {
            errorstr = $"{symbol.Name} Not enough candles available for interval {interval.Name} count={candlesForHistory.Count} requested={maxCandles}";
            if (candlesForHistory.Count != 0)
            {
                var x = candlesForHistory[^1]; //.Last();
                errorstr += " last in history = " + x.Timestamp.ToLocalTime().ToString();

                x = intervalCandles.Values.Last();
                errorstr += " last in candlelist = " + x.Timestamp.ToLocalTime().ToString();
            }
            return null;
        }

        errorstr = "";
        return candlesForHistory;
    }


    /// <summary>
    /// Applies the Lux 5m value to the CryptoData of the latest candle. When the interval IS 5m,
    /// BuildCurrent() already set Lux5mValue incrementally — nothing to do. For the other intervals
    /// it reads the value from the 5m Data dictionary, falling back to the full
    /// LuxIndicator.Calculate only when the 5m value is unavailable.
    /// </summary>
    private static void ApplyLux(CryptoSymbol symbol, CryptoSymbolInterval symbolInterval, CandleTime candleOpenTime)
    {
        if (!symbolInterval.Data.TryGetValue(candleOpenTime, out CryptoData? data))
            return;

        // 5m: BuildCurrent() already set Lux5mValue incrementally.
        if (symbolInterval.IntervalPeriod == CryptoIntervalPeriod.interval5m
            && data.Lux5mValue.HasValue)
            return;

        // Non-5m intervals: read the pre-computed value from the 5m Data dictionary. Take the LAST
        // 5m sub-candle that has closed, not the first — see LuxIndicator.LastClosed5mCandle.
        CryptoSymbolInterval si5m = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval5m);
        CandleTime aligned5m = LuxIndicator.LastClosed5mCandle(candleOpenTime, symbolInterval.Interval.Duration);

        // One value per 5m candle, so every candle of the analysed interval that falls inside the
        // same 5m candle gets the same number. Without this the fallback below recomputed it every
        // time: 260 candles times an eleven-deep inner loop, per candle of the analysed interval.
        // On emulator run 240 (24-08-2026, base interval 1m) that was 874.7s of the 2092.9s the whole
        // run measured - the single biggest item, and four out of five of those calls were producing
        // a number that was already known.
        //
        // Safe whatever order the caller walks in: the key IS the 5m candle, so a hit can only ever
        // return the value that belongs to it. Walking backwards just misses and recomputes.
        if (symbolInterval.LuxCachedFor == aligned5m)
        {
            data.Lux5mValue = symbolInterval.LuxCachedValue;
            PipelineProfiler.RecordLux(fromCache: true, from5mData: false);
            return;
        }

        if (si5m.Data.TryGetValue(aligned5m, out CryptoData? data5m) && data5m.Lux5mValue.HasValue)
        {
            data.Lux5mValue = data5m.Lux5mValue;
            symbolInterval.LuxCachedFor = aligned5m;
            symbolInterval.LuxCachedValue = data5m.Lux5mValue;
            PipelineProfiler.RecordLux(fromCache: false, from5mData: true);
            return;
        }

        // Fallback: full recalculation (5m data not yet available). CalculateNew resolves its
        // argument with StartOfIntervalCandle, so passing the target OPEN time lands on that candle.
        LuxIndicator.Calculate(symbol, out int luxOverSold, out int luxOverBought,
            CryptoIntervalPeriod.interval5m, aligned5m);

        int luxValue = 0;
        if (luxOverBought > 0)
            luxValue += luxOverBought;
        if (luxOverSold > 0)
            luxValue -= luxOverSold;
        data.Lux5mValue = (short)luxValue;
        symbolInterval.LuxCachedFor = aligned5m;
        symbolInterval.LuxCachedValue = (short)luxValue;
        PipelineProfiler.RecordLux(fromCache: false, from5mData: false);
    }

}