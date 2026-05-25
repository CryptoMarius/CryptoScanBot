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

    public static void CalculateNew(CryptoSymbol symbol, out int luxOverSold, out int luxOverBought,
        CryptoIntervalPeriod cryptoIntervalPeriod, CandleTime candleCloseTime)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(cryptoIntervalPeriod);
        CandleTime candleIntervalOpenTimeEnd = IntervalTools.StartOfIntervalCandle(candleCloseTime, symbolInterval.Interval.Duration);
        if (!symbolInterval.CandleList.ContainsKey(candleIntervalOpenTimeEnd))
            candleIntervalOpenTimeEnd -= symbolInterval.Interval.Duration;
        CandleTime candleIntervalOpenTimeStart = candleIntervalOpenTimeEnd - 99 * symbolInterval.Interval.Duration;



        int min = 10;
        int max = 20;
        int overbuy = 0;
        int oversell = 0;
        var N = max - min + 1;
        decimal[] num = new decimal[N];
        decimal[] den = new decimal[N];

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

        luxOverSold = (int)(100m * oversell / N);
        luxOverBought = (int)(100m * overbuy / N);
    }

    /// <summary>
    /// Single-pass batch variant: computes the Lux value at each candle's own close for the
    /// last <paramref name="count"/> candles ending at <paramref name="endOpenTime"/>.
    /// Walks (99 + count − 1) candles once, sharing the RMA warmup across all output points.
    /// Output arrays are indexed 0..count−1, where [count−1] corresponds to endOpenTime.
    /// </summary>
    public static void CalculateRange(CryptoSymbol symbol, CryptoIntervalPeriod cryptoIntervalPeriod,
        CandleTime endOpenTime, int count, out int[] overSoldHistory, out int[] overBoughtHistory)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(cryptoIntervalPeriod);
        uint duration = symbolInterval.Interval.Duration;

        // Walk needs 99 warmup bars before the first recorded bar, plus (count − 1) extra to
        // reach endOpenTime. Total span = (99 + count − 1) bars before endOpenTime.
        CandleTime startOpenTime = endOpenTime - (uint)(99 + count - 1) * duration;

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
        // barIndex 99 (= 100th iteration, first with a fully-warmed RMA) maps to output[0].
        const int recordOffset = 99;

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


    public static void Calculate(CryptoSymbol symbol, out int luxOverSold, out int luxOverBought,
        CryptoIntervalPeriod cryptoIntervalPeriod, CandleTime candleCloseTime)
    {
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
