using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.Core.Trend;

/// <summary>
/// The market trend of minutes that have already passed, for the stretch the scanner was not running.
/// <para>
/// <see cref="MarketTrend"/> can only answer "what is the trend right now", so every restart leaves a
/// hole in the barometer graph as wide as the downtime - the value is stored in the $BMX candles and
/// comes back with them, but the minutes in between were never measured by anybody. This fills those
/// minutes in afterwards.
/// </para>
/// <para>
/// It can, because the ZigZag the trend rests on is built candle by candle: feed it up to a moment
/// and read the interpretation, and you have the trend as it stood at that moment. That is exactly
/// what the live scanner does once per minute; this does the same thing for a series of minutes in
/// one pass, and then averages over the coins the way MarketTrend does.
/// </para>
/// <para>
/// Two deliberate differences with a value the live scanner measured itself, both in the direction of
/// "rather no invention than a convincing one":
/// </para>
/// <para>
/// 1. Only CLOSED candles take part. The live scanner feeds the candle of the interval in progress as
/// well, with whatever it held at the moment its timer happened to run - a moment that cannot be
/// reconstructed afterwards. Leaving it out is reproducible and lags at most one candle of the
/// interval concerned, which the ZigZag treats as its mutable tail anyway.
/// </para>
/// <para>
/// 2. Its own ZigZag instance, never the cached one on the symbol. That cache is fed forward by the
/// analysis threads and cannot be rewound, so borrowing it would both give a wrong answer here and
/// disturb the trend the scanner trades on.
/// </para>
/// </summary>
public static class MarketTrendBackfill
{
    /// <summary>
    /// The market trend per minute over <paramref name="from"/>..<paramref name="to"/> inclusive.
    /// <para>
    /// A minute is only in the result when at least <paramref name="minimumSymbols"/> coins produced
    /// a percentage for it, the same floor <see cref="MarketTrend"/> applies - and per trend
    /// separately, because a coin can have too little history for one zigzag setting and enough for
    /// the other.
    /// </para>
    /// </summary>
    public static SortedList<CandleTime, (decimal? Primary, decimal? Secondary)> Measure(
        IReadOnlyList<CryptoSymbol> symbolList, CandleTime from, CandleTime to, int minimumSymbols)
    {
        return Measure(symbolList, from, to, 1, minimumSymbols);
    }


    /// <summary>
    /// The market trend of every whole hour over <paramref name="from"/>..<paramref name="to"/>
    /// inclusive (both on a whole hour) - the days of history behind the barometer graph, see
    /// BarometerHourHistory.
    /// <para>
    /// The same value <see cref="Measure(IReadOnlyList{CryptoSymbol}, CandleTime, CandleTime, int)"/>
    /// gives for that minute, at a fraction of the cost: the zigzag is still fed every candle, but
    /// the trend is only read on the whole hours instead of after every candle, which is where the
    /// time goes over days of 1m to 30m candles.
    /// </para>
    /// </summary>
    public static SortedList<CandleTime, (decimal? Primary, decimal? Secondary)> MeasureHourly(
        IReadOnlyList<CryptoSymbol> symbolList, CandleTime from, CandleTime to, int minimumSymbols)
    {
        return Measure(symbolList, from, to, 60, minimumSymbols);
    }


    /// <summary>The market trend at <paramref name="from"/> and every <paramref name="step"/>
    /// minutes after it, up to and including <paramref name="to"/>.</summary>
    private static SortedList<CandleTime, (decimal? Primary, decimal? Secondary)> Measure(
        IReadOnlyList<CryptoSymbol> symbolList, CandleTime from, CandleTime to, uint step, int minimumSymbols)
    {
        SortedList<CandleTime, (decimal?, decimal?)> result = [];
        if (to < from)
            return result;

        int count = (int)((to.Minutes - from.Minutes) / step) + 1;
        decimal[] sumPrimary = new decimal[count];
        decimal[] sumSecondary = new decimal[count];
        int[] countPrimary = new int[count];
        int[] countSecondary = new int[count];

        // Indexed and not foreach, for the same reason as MarketTrend.Measure: the symbol list of a
        // quote coin grows while the scanner runs.
        for (int i = 0; i < symbolList.Count; i++)
        {
            CryptoSymbol symbol = symbolList[i];

            if (symbol.QuoteData == null || !symbol.QuoteData.FetchCandles || symbol.IsBarometerSymbol() || !symbol.EnoughVolume())
                continue;

            AddSeries(symbol, GlobalData.Settings.Trend.Primary, from, to, step, sumPrimary, countPrimary);
            AddSeries(symbol, GlobalData.Settings.Trend.Secondary, from, to, step, sumSecondary, countSecondary);
        }

        for (int index = 0; index < count; index++)
        {
            decimal? primary = countPrimary[index] >= minimumSymbols
                ? decimal.Round(sumPrimary[index] / countPrimary[index], 8) : null;
            decimal? secondary = countSecondary[index] >= minimumSymbols
                ? decimal.Round(sumSecondary[index] / countSecondary[index], 8) : null;

            if (primary.HasValue || secondary.HasValue)
                result.Add(from + (uint)index * step, (primary, secondary));
        }

        return result;
    }


    /// <summary>
    /// Add the trend percentage of one coin, per minute, to the running totals.
    /// <para>
    /// The percentage is the same weighted sum <see cref="SymbolTrend"/> computes: every interval
    /// counts for its own duration, bullish adds and bearish subtracts, and the weekly interval is
    /// left out. A coin that has an interval without candles produces nothing at all - that is the
    /// bail-out the live calculation makes as well, and doing it differently here would give the
    /// backlog a systematically different market than the minutes around it.
    /// </para>
    /// </summary>
    private static void AddSeries(CryptoSymbol symbol, SettingsZigZag trendSettings,
        CandleTime from, CandleTime to, uint step, decimal[] sum, int[] count)
    {
        int minutes = sum.Length;
        long[] weightSum = new long[minutes];
        long weightMax = 0;

        foreach (CryptoInterval interval in GlobalData.IntervalList)
        {
            if (interval.IntervalPeriod == CryptoIntervalPeriod.interval1w)
                continue;

            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
            if (symbolInterval.CandleList.LastCandle.OpenTime == 0)
                return; // no candles for this interval - the live calculation gives up here too

            if (step == 1)
                AddInterval(symbol, interval, trendSettings, from, to, weightSum);
            else
                AddIntervalSampled(symbol, interval, trendSettings, from, step, weightSum);
            weightMax += interval.Duration;
        }

        if (weightMax == 0)
            return;

        for (int index = 0; index < minutes; index++)
        {
            sum[index] += 100m * weightSum[index] / weightMax;
            count[index]++;
        }
    }


    /// <summary>
    /// Weigh one interval of one coin into <paramref name="weightSum"/>, minute by minute.
    /// <para>
    /// The ZigZag is first fed everything that had already closed at <paramref name="from"/>, which
    /// gives the state the graph starts at. After that the candles are fed one at a time, and what
    /// the interpretation says after a candle holds from the moment that candle CLOSED until the next
    /// one closes - a trend is known no earlier than the candle it is read from.
    /// </para>
    /// </summary>
    private static void AddInterval(CryptoSymbol symbol, CryptoInterval interval, SettingsZigZag trendSettings,
        CandleTime from, CandleTime to, long[] weightSum)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (!symbolInterval.CandleList.TryGetFirstCandle(out CryptoCandle firstCandle))
            return;

        // The first candle that had NOT yet closed at 'from'; everything before it is the warm-up.
        CandleTime firstOpen = firstCandle.OpenTime;
        CandleTime stepFrom = IntervalTools.StartOfIntervalCandle(from, interval.Duration);
        while (stepFrom + interval.Duration <= from)
            stepFrom += interval.Duration;
        if (stepFrom < firstOpen)
            stepFrom = firstOpen;

        ZigZagIndicator indicator = new(trendSettings.TrendType, trendSettings.UseHighLow, 1.0);

        // Warm-up in one batch - the interpretation in between is of no interest, only the state it
        // ends in. Feeding it per candle would cost an OptimizeList per candle for nothing.
        if (stepFrom > firstOpen)
            TrendTools.AddCandlesToIndicatorsAsync(indicator, symbol, interval, firstOpen, stepFrom - interval.Duration).Wait();

        int sign = SignOf(TrendInterval.InterpretZigZagPoints(indicator, null));
        int index = 0;
        long duration = interval.Duration;

        for (CandleTime open = stepFrom; open + interval.Duration <= to; open += interval.Duration)
        {
            CandleTime closed = open + interval.Duration;

            // Everything before this candle closed still carries the previous state.
            int until = (int)(closed.Minutes - from.Minutes);
            if (until > weightSum.Length)
                until = weightSum.Length;
            while (index < until)
                weightSum[index++] += sign * duration;

            // One candle, then read the trend - the same step the scanner takes once a minute.
            TrendTools.AddCandlesToIndicatorsAsync(indicator, symbol, interval, open, open).Wait();
            sign = SignOf(TrendInterval.InterpretZigZagPoints(indicator, null));

            if (index >= weightSum.Length)
                return;
        }

        while (index < weightSum.Length)
            weightSum[index++] += sign * duration;
    }


    /// <summary>
    /// <see cref="AddInterval"/> for a series with a point every <paramref name="step"/> minutes
    /// instead of every minute. Same warm-up, same rule - a point carries the trend after every
    /// candle that had closed at that moment - but the candles in between two points go into the
    /// zigzag in one batch and the trend is read once per point. Over days of 1m to 30m candles that
    /// is the difference between reading it thousands of times per coin and a few hundred.
    /// </summary>
    private static void AddIntervalSampled(CryptoSymbol symbol, CryptoInterval interval, SettingsZigZag trendSettings,
        CandleTime from, uint step, long[] weightSum)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        if (!symbolInterval.CandleList.TryGetFirstCandle(out CryptoCandle firstCandle))
            return;

        // The first candle that had NOT yet closed at 'from'; everything before it is the warm-up.
        CandleTime firstOpen = firstCandle.OpenTime;
        CandleTime nextOpen = IntervalTools.StartOfIntervalCandle(from, interval.Duration);
        while (nextOpen + interval.Duration <= from)
            nextOpen += interval.Duration;
        if (nextOpen < firstOpen)
            nextOpen = firstOpen;

        ZigZagIndicator indicator = new(trendSettings.TrendType, trendSettings.UseHighLow, 1.0);
        if (nextOpen > firstOpen)
            TrendTools.AddCandlesToIndicatorsAsync(indicator, symbol, interval, firstOpen, nextOpen - interval.Duration).Wait();
        int sign = SignOf(TrendInterval.InterpretZigZagPoints(indicator, null));

        long duration = interval.Duration;
        for (int index = 0; index < weightSum.Length; index++)
        {
            CandleTime point = from + (uint)index * step;

            // Every candle that had closed at this point and is not in the zigzag yet
            if (nextOpen + interval.Duration <= point)
            {
                CandleTime lastOpen = nextOpen;
                while (lastOpen + interval.Duration + interval.Duration <= point)
                    lastOpen += interval.Duration;
                TrendTools.AddCandlesToIndicatorsAsync(indicator, symbol, interval, nextOpen, lastOpen).Wait();
                nextOpen = lastOpen + interval.Duration;
                sign = SignOf(TrendInterval.InterpretZigZagPoints(indicator, null));
            }

            weightSum[index] += sign * duration;
        }
    }


    /// <summary>What a trend contributes to the weighted sum. Unknown weighs nothing, but its
    /// interval does count towards the maximum - exactly as in SymbolTrend.</summary>
    private static int SignOf(CryptoTrendIndicator trend)
    {
        if (trend == CryptoTrendIndicator.Bullish)
            return 1;
        if (trend == CryptoTrendIndicator.Bearish)
            return -1;
        return 0;
    }
}
