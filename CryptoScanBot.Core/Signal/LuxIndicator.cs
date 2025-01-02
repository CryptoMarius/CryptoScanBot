using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal;

public class LuxIndicator
{
    /// <summary>
    /// Based on the "RSI Multi Length [LuxAlgo]"
    /// We use the luxOverSold of luxOverBought values as extra text in the signal
    /// </summary>
    public static void CalculateOld(CryptoSymbol symbol, out int luxOverSold, out int luxOverBought)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval5m);

        // Array of 10 elements
        decimal[] num = new decimal[10];
        decimal[] den = new decimal[10];


        //int min = 10;
        //int max = 20;
        int min = 05;
        int max = 22;
        int overbuy = 0;
        int oversell = 0;
        int oversold = 30;
        int overbought = 70;
        CryptoCandle? candlePrev;
        CryptoCandle? candleLast = null;

        if (symbolInterval.CandleList.Count > 30)
        {
            long unixLast = symbolInterval.CandleList.Keys.Last();
            long unixLoop = unixLast - 29 * symbolInterval.Interval.Duration;

            while (unixLoop <= unixLast)
            {
                candlePrev = candleLast;
                if (candlePrev == null)
                    continue;
                if (symbolInterval.CandleList.TryGetValue(unixLoop, out candleLast))
                {
                    //count++;

                    int k = 0;
                    overbuy = 0;
                    oversell = 0;
                    decimal diff = candleLast.Close - candlePrev.Close;

                    // Calculate with RMA
                    for (int i = min; i < max; i++)
                    {
                        decimal alpha = 1 / (decimal)i;

                        // RMA - numerator .... num[k]=α⋅diff+(1−α)⋅num[k−1]
                        decimal num_rma = alpha * diff + (1m - alpha) * num[k];
                        // RMA - denominator ..... den[k]=α⋅∣diff∣+(1−α)⋅den[k−1]
                        decimal den_rma = alpha * Math.Abs(diff) + (1m - alpha) * den[k];

                        decimal rsi;
                        if (den_rma == 0)
                            rsi = 50m;
                        else
                            rsi = 50m * num_rma / den_rma + 50m;

                        if (rsi > overbought)
                            overbuy++;
                        if (rsi < oversold)
                            oversell++;


                        num[k] = num_rma;
                        den[k] = den_rma;
                        k++;
                    }
                }
                unixLoop += symbolInterval.Interval.Duration;
            }
        }
        luxOverSold = 10 * oversell;
        luxOverBought = 10 * overbuy;
    }

    public static void CalculateNew(CryptoSymbol symbol, out int luxOverSold, out int luxOverBought, CryptoIntervalPeriod cryptoIntervalPeriod, long candleCloseTime)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(cryptoIntervalPeriod);
        long candleIntervalOpenTimeEnd = IntervalTools.StartOfIntervalCandle(candleCloseTime, symbolInterval.Interval.Duration);
        if (!symbolInterval.CandleList.ContainsKey(candleIntervalOpenTimeEnd))
            candleIntervalOpenTimeEnd -= symbolInterval.Interval.Duration;
        long candleIntervalOpenTimeStart = candleIntervalOpenTimeEnd - 29 * symbolInterval.Interval.Duration;



        int min = 10;
        int max = 20;
        int overbuy = 0;
        int oversell = 0;
        int oversold = 30;
        int overbought = 70;
        var N = max - min + 1;
        decimal[] num = new decimal[N];
        decimal[] den = new decimal[N];

        CryptoCandle? candlePrev;
        CryptoCandle? candleLast = null;

        long loop = candleIntervalOpenTimeStart;
        while (loop <= candleIntervalOpenTimeEnd)
        {
            candlePrev = candleLast;
            if (symbolInterval.CandleList.TryGetValue(loop, out candleLast) && candlePrev != null)
            {
                int k = 0;
                overbuy = 0;
                oversell = 0;
                decimal diff = candleLast.Close - candlePrev.Close;

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

                    if (rsi > overbought)
                        overbuy++;
                    if (rsi < oversold)
                        oversell++;

                    num[k] = num_rma;
                    den[k] = den_rma;
                    k++;
                }
            }
            loop += symbolInterval.Interval.Duration;
        }

        luxOverSold = (int)(100 * (decimal)oversell / N);
        luxOverBought = (int)(100 * (decimal)overbuy / N); 
    }

    public static void Calculate(CryptoSymbol symbol, out int luxOverSold, out int luxOverBought, CryptoIntervalPeriod cryptoIntervalPeriod, long candleCloseTime)
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
