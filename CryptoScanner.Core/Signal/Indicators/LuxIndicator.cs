using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Indicators;

public class LuxIndicator
{
    /// <summary>
    /// Based on the "RSI Multi Length [LuxAlgo]"
    /// We use the luxOverSold of luxOverBought values as extra text in the signal
    /// </summary>
    //public static void CalculateOld(CryptoSymbol symbol, out int luxOverSold, out int luxOverBought)
    //{
    //    CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval5m);

    //    // Array of 10 elements
    //    decimal[] num = new decimal[10];
    //    decimal[] den = new decimal[10];


    //    int min = 10;
    //    int max = 20;
    //    //int min = 05;
    //    //int max = 22;
    //    int overbuy = 0;
    //    int oversell = 0;
    //    int oversold = 30;
    //    int overbought = 70;
    //    CryptoCandle? candlePrev;
    //    CryptoCandle? candleLast = null;

    //    if (symbolInterval.CandleList.Count > 30)
    //    {
    //        long unixLast = symbolInterval.CandleList.Keys.Last();
    //        long unixLoop = unixLast - 29 * symbolInterval.Interval.Duration;

    //        while (unixLoop <= unixLast)
    //        {
    //            candlePrev = candleLast;
    //            if (candlePrev == null)
    //                continue;
    //            if (symbolInterval.CandleList.TryGetValue(unixLoop, out candleLast))
    //            {
    //                //count++;

    //                int k = 0;
    //                overbuy = 0;
    //                oversell = 0;
    //                decimal diff = candleLast.Close - candlePrev.Close;

    //                // Calculate with RMA
    //                for (int i = min; i <= max; i++)
    //                {
    //                    decimal alpha = 1m / (decimal)i;

    //                    // RMA - numerator .... num[k]=α⋅diff+(1−α)⋅num[k−1]
    //                    decimal num_rma = alpha * diff + (1m - alpha) * num[k];
    //                    // RMA - denominator ..... den[k]=α⋅∣diff∣+(1−α)⋅den[k−1]
    //                    decimal den_rma = alpha * Math.Abs(diff) + (1m - alpha) * den[k];

    //                    decimal rsi;
    //                    if (den_rma == 0)
    //                        rsi = 50m;
    //                    else
    //                        rsi = 50m * num_rma / den_rma + 50m;

    //                    if (rsi > overbought)
    //                        overbuy++;
    //                    if (rsi < oversold)
    //                        oversell++;


    //                    num[k] = num_rma;
    //                    den[k] = den_rma;
    //                    k++;
    //                }
    //            }
    //            unixLoop += symbolInterval.Interval.Duration;
    //        }
    //    }
    //    luxOverSold = 10 * oversell;
    //    luxOverBought = 10 * overbuy;
    //}

    /// <summary>
    /// Warm-up bars before the bar being reported, counted in RMA STEPS. The walk covers
    /// WarmupBars + 1 candles (its first iteration only seeds candlePrev), so N here means N price
    /// differences over N+1 candles — the old 99 was 99 steps across 100 candles.
    /// <para>
    /// The Pine original keeps <c>var</c> arrays for the whole chart: it has NO window and starts at
    /// the first bar. Replaying that here is not practical, so we approximate it — but the
    /// approximation has to be the same everywhere. It used to be 99 here while the incremental
    /// IntervalIndicatorHub warmed up on 260 candles, and on the reference series a 99-bar warm-up
    /// disagrees with the windowless original on 1.1% of the candles. Matching the hub's 260 removes
    /// that discrepancy for every candle measured.
    /// </para>
    /// </summary>
    public const int WarmupBars = 260;

    public static void CalculateNew(CryptoSymbol symbol, out int luxOverSold, out int luxOverBought,
        CryptoIntervalPeriod cryptoIntervalPeriod, CandleTime candleCloseTime)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(cryptoIntervalPeriod);
        CandleTime candleIntervalOpenTimeEnd = IntervalTools.StartOfIntervalCandle(candleCloseTime, symbolInterval.Interval.Duration);
        if (!symbolInterval.CandleList.ContainsKey(candleIntervalOpenTimeEnd))
            candleIntervalOpenTimeEnd -= symbolInterval.Interval.Duration;
        CandleTime candleIntervalOpenTimeStart = candleIntervalOpenTimeEnd - WarmupBars * symbolInterval.Interval.Duration;



        int min = 10;
        int max = 20;
        int overbuy = 0;
        int oversell = 0;
        var N = max - min + 1;
        // Uses double instead of decimal: the RMA is a smoothing average and the result is only an
        // integer count (overbought/oversold, 0..100), so the extra precision of decimal is wasted
        // while decimal arithmetic is ~10-20x slower. A borderline RSI may very occasionally land on
        // the other side of the 70/30 threshold, so this can differ by ±1 in rare cases — accepted.
        double[] num = new double[N];
        double[] den = new double[N];

        CryptoCandle candlePrev = default;
        CryptoCandle candleLast = default;

        CandleTime loop = candleIntervalOpenTimeStart;
        while (loop <= candleIntervalOpenTimeEnd)
        {
            candlePrev = candleLast;
            if (symbolInterval.CandleList.TryGetValue(loop, out candleLast) && candlePrev.OpenTime != 0)
            {
                int k = 0;
                overbuy = 0;
                oversell = 0;
                double diff = (double)(candleLast!.Close - candlePrev.Close);

                for (int i = min; i <= max; i++)
                {
                    double alpha = 1.0 / i;
                    double num_rma = alpha * diff + (1.0 - alpha) * num[k];
                    double den_rma = alpha * Math.Abs(diff) + (1.0 - alpha) * den[k];

                    double rsi;
                    if (den_rma == 0.0)
                        rsi = 50.0;
                    else
                        rsi = 50.0 * num_rma / den_rma + 50.0;

                    if (rsi > 70)
                        overbuy++;
                    if (rsi < 30)
                        oversell++;

                    num[k] = num_rma;
                    den[k] = den_rma;
                    k++;
                }
            }
            loop += symbolInterval.Interval.Duration;
        }

        luxOverSold = (int)(100.0 * oversell / N);
        luxOverBought = (int)(100.0 * overbuy / N);
    }

    /// <summary>
    /// Single-pass batch variant: computes the Lux value at each candle's own close for the
    /// last <paramref name="count"/> candles ending at <paramref name="endOpenTime"/>.
    /// Walks (WarmupBars + count − 1) candles once, sharing the RMA warmup across all output points.
    /// Output arrays are indexed 0..count−1, where [count−1] corresponds to endOpenTime.
    /// </summary>
    public static void CalculateRange(CryptoSymbol symbol, CryptoIntervalPeriod cryptoIntervalPeriod,
        CandleTime endOpenTime, int count, out int[] overSoldHistory, out int[] overBoughtHistory)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(cryptoIntervalPeriod);
        uint duration = symbolInterval.Interval.Duration;

        // Walk needs WarmupBars warmup bars before the first recorded bar, plus (count − 1) extra to
        // reach endOpenTime. Total span = (WarmupBars + count − 1) bars before endOpenTime.
        CandleTime startOpenTime = endOpenTime - (uint)(WarmupBars + count - 1) * duration;

        int min = 10;
        int max = 20;
        int N = max - min + 1;
        decimal[] num = new decimal[N];
        decimal[] den = new decimal[N];

        overSoldHistory = new int[count];
        overBoughtHistory = new int[count];

        CryptoCandle candlePrev = default;
        CryptoCandle candleLast = default;

        // recordOffset = barIndex value at which we start writing into the output arrays.
        // barIndex WarmupBars (the first with a fully-warmed RMA) maps to output[0].
        const int recordOffset = WarmupBars;

        CandleTime loop = startOpenTime;
        int barIndex = 0;
        while (loop <= endOpenTime)
        {
            candlePrev = candleLast;
            if (symbolInterval.CandleList.TryGetValue(loop, out candleLast) && candlePrev.OpenTime != 0)
            {
                int k = 0;
                int overbuy = 0;
                int oversell = 0;
                decimal diff = candleLast!.Close - candlePrev.Close;

                for (int i = min; i <= max; i++)
                {
                    decimal alpha = 1.0m / i;
                    decimal num_rma = alpha * diff + (1m - alpha) * num[k];
                    decimal den_rma = alpha * Math.Abs(diff) + (1m - alpha) * den[k];

                    decimal rsi;
                    if (den_rma == 0)
                        rsi = 50m;
                    else
                        rsi = 50m * num_rma / den_rma + 50m;

                    if (rsi > 70) overbuy++;
                    if (rsi < 30) oversell++;

                    num[k] = num_rma;
                    den[k] = den_rma;
                    k++;
                }

                int outIdx = barIndex - recordOffset;
                if (outIdx >= 0 && outIdx < count)
                {
                    overSoldHistory[outIdx] = (int)(100m * oversell / N);
                    overBoughtHistory[outIdx] = (int)(100m * overbuy / N);
                }
            }
            loop += duration;
            barIndex++;
        }
    }


    /// <summary>
    /// The open time of the last 5m candle that has fully closed when the candle at
    /// <paramref name="candleOpenTime"/> (of <paramref name="intervalDuration"/> minutes) closes.
    /// <para>
    /// The Pine original ("RSI Multi Length [LuxAlgo]") runs on every bar and the value it shows is
    /// the state after that bar — there is no window and no bar is skipped. So the 5m value that
    /// belongs to a 15m candle is the value at its LAST 5m sub-candle, not its first: the two
    /// sub-candles in between carry the price movement that makes the RSI count move at all.
    /// </para>
    /// <para>
    /// Both callers used to get this wrong, and differently: IndicatorEngine.ApplyLux took the FIRST
    /// sub-candle (dropping 2 of 3 on a 15m candle, 11 of 12 on a 1h) and SignalCreate took the
    /// second, so the value shown in the grid and the one stored on the signal described different
    /// moments in time.
    /// </para>
    /// </summary>
    public static CandleTime LastClosed5mCandle(CandleTime candleOpenTime, uint intervalDuration)
    {
        // A 5m candle has closed once its open + 5 has been reached, so the newest closed one is
        // the 5m candle containing (closeTime - 5). Works for intervals below 5m too: a 1m candle
        // closing at 08:04 maps back to the 5m candle 07:55-08:00.
        CandleTime closeTime = candleOpenTime + intervalDuration;
        const uint duration5m = 5;
        if (closeTime < duration5m)
            return candleOpenTime;
        return IntervalTools.StartOfIntervalCandle(closeTime - duration5m, duration5m);
    }


    public static void Calculate(CryptoSymbol symbol, out int luxOverSold, out int luxOverBought,
        CryptoIntervalPeriod cryptoIntervalPeriod, CandleTime candleCloseTime)
    {
        //luxOverBought = 0;
        //luxOverSold = 0;
        //return;

        CalculateNew(symbol, out luxOverSold, out luxOverBought, cryptoIntervalPeriod, candleCloseTime);

        //// Debug, same results for old and new? No?
        //CalculateOld(symbol, out int luxOverSold2, out int luxOverBought2);


        //if (luxOverSold != luxOverSold2 || luxOverBought != luxOverBought2)
        //{
        //    CalculateOld(symbol, out luxOverSold2, out luxOverBought2);
        //    GlobalData.AddTextToLogTab($"LuxIndicator.CalculateOld {luxOverSold2} {luxOverBought2}");

        //    CalculateNew(symbol, out luxOverSold, out luxOverBought, cryptoIntervalPeriod, candleCloseTime);
        //    GlobalData.AddTextToLogTab($"LuxIndicator.CalculateNew {luxOverSold} {luxOverBought}");
        //}
    }
}
