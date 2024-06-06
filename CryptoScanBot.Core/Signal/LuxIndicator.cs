using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
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

        // Dat array van 10 (nu globaal)
        decimal[] num = new decimal[10];
        decimal[] den = new decimal[10];
        for (int j = 0; j < 10; j++)
        {
            num[j] = 0m;
            den[j] = 0m;
        }

        // Gefixeerde getallen
        int min = 10;
        int max = 20;
        int oversold = 30;
        int overbought = 70;
        //decimal N = max - min + 1;

        int overbuy = 0;
        int oversell = 0;
        CryptoCandle? candlePrev;
        CryptoCandle? candleLast = null;

        int count = 0;
        for (int j = symbolInterval.CandleList.Count - 30; j < symbolInterval.CandleList.Count; j++)
        {
            if (j < 1) //out of range, not sure if skipping 0 was intentional?
                continue;
            candlePrev = candleLast;
            candleLast = symbolInterval.CandleList.Values[j];
            if (candlePrev == null)
                continue;
            count++;

            int k = 0;
            decimal avg = 0m;
            overbuy = 0;
            oversell = 0;
            decimal diff = candleLast.Close - candlePrev.Close;

            for (int i = min; i < max; i++)
            {
                decimal alpha = 1 / (decimal)i;

                decimal num_rma = alpha * diff + (1m - alpha) * num[k];
                decimal den_rma = alpha * Math.Abs(diff) + (1m - alpha) * den[k];

                decimal rsi;
                if (den_rma == 0)
                    rsi = 50m;
                else
                    rsi = 50m * num_rma / den_rma + 50m;

                avg += rsi;

                if (rsi > overbought)
                    overbuy++;
                if (rsi < oversold)
                    oversell++;


                num[k] = num_rma;
                den[k] = den_rma;
                k++;
            }
        }

        luxOverSold = 10 * oversell;
        luxOverBought = 10 * overbuy;
    }

    public static void CalculateNew(CryptoSymbol symbol, out int luxOverSold, out int luxOverBought, CryptoIntervalPeriod cryptoIntervalPeriod, long candleCloseTime)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(cryptoIntervalPeriod);
        long candleIntervalOpenTimeEnd = CandleTools.StartOfIntervalCandle(symbolInterval.Interval, candleCloseTime);
        if (!symbolInterval.CandleList.ContainsKey(candleIntervalOpenTimeEnd))
            candleIntervalOpenTimeEnd -= symbolInterval.Interval.Duration;


        long candleIntervalOpenTimeStart = candleIntervalOpenTimeEnd - 29 * symbolInterval.Interval.Duration;

        // Dat array van 10 (nu globaal)
        decimal[] num = new decimal[10];
        decimal[] den = new decimal[10];
        for (int j = 0; j < 10; j++)
        {
            num[j] = 0m;
            den[j] = 0m;
        }

        // Gefixeerde getallen
        int min = 10;
        int max = 20;
        int oversold = 30;
        int overbought = 70;
        //decimal N = max - min + 1;

        int overbuy = 0;
        int oversell = 0;
        CryptoCandle? candlePrev;
        CryptoCandle? candleLast = null;


        //for (int j = candles.Count - 30; j < candles.Count; j++)
        int count = 0;
        long loop = candleIntervalOpenTimeStart;
        while (loop <= candleIntervalOpenTimeEnd)
        {
            candlePrev = candleLast;
            if (symbolInterval.CandleList.TryGetValue(loop, out candleLast) && candlePrev != null)
            {
                count++;
                //if (j < 1) out of range, not sure if skipping 0 was intentional?
                //    continue;
                //candlePrev = candleLast;
                //candleLast = candles.Values[j];
                //if (candlePrev == null)
                //    continue;

                int k = 0;
                decimal avg = 0m;
                overbuy = 0;
                oversell = 0;
                decimal diff = candleLast.Close - candlePrev.Close;

                for (int i = min; i < max; i++)
                {
                    decimal alpha = 1 / (decimal)i;

                    decimal num_rma = alpha * diff + (1m - alpha) * num[k];
                    decimal den_rma = alpha * Math.Abs(diff) + (1m - alpha) * den[k];

                    decimal rsi;
                    if (den_rma == 0)
                        rsi = 50m;
                    else
                        rsi = 50m * num_rma / den_rma + 50m;

                    avg += rsi;

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

        luxOverSold = 10 * oversell;
        luxOverBought = 10 * overbuy;


    }

    public static void Calculate(CryptoSymbol symbol, out int luxOverSold, out int luxOverBought, CryptoIntervalPeriod cryptoIntervalPeriod, long lastOpenTime)
    {
        CalculateNew(symbol, out luxOverSold, out luxOverBought, cryptoIntervalPeriod, lastOpenTime);

        // Debug, are old have the samen results?
        CalculateOld(symbol, out int luxOverSold2, out int luxOverBought2);


        if (luxOverSold != luxOverSold2 || luxOverBought != luxOverBought2)
        {
            CalculateOld(symbol, out luxOverSold2, out luxOverBought2);
            GlobalData.AddTextToLogTab($"LuxIndicator.CalculateOld {luxOverSold2} {luxOverBought2}");
            
            CalculateNew(symbol, out luxOverSold, out luxOverBought, cryptoIntervalPeriod, lastOpenTime);
            GlobalData.AddTextToLogTab($"LuxIndicator.CalculateNew {luxOverSold} {luxOverBought}");
        }
    }
}
