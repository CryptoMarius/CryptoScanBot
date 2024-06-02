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
    public static void CalculateOrg(CryptoSymbol symbol, out int luxOverSold, out int luxOverBought)
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


        for (int j = symbolInterval.CandleList.Count - 30; j < symbolInterval.CandleList.Count; j++)
        {
            candlePrev = candleLast;
            if (j < 1) //out of range, not sure if skipping 0 was intentional?
                continue;
            candlePrev = candleLast;
            candleLast = symbolInterval.CandleList.Values[j];
            if (candlePrev == null)
                continue;

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

    public static void Calculate(CryptoSymbol symbol, out int luxOverSold, out int luxOverBought, CryptoIntervalPeriod cryptoIntervalPeriod, long lastOpenTime)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(cryptoIntervalPeriod);
        long candleIntervalOpenTimeStart = CandleTools.StartOfIntervalCandle(symbolInterval.Interval, lastOpenTime);
        long candleIntervalOpenTimeEnd = candleIntervalOpenTimeStart - 30 * symbolInterval.Interval.Duration;

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
        long loop = candleIntervalOpenTimeEnd;
        while (loop <= candleIntervalOpenTimeStart)
        {
            candlePrev = candleLast;
            if (symbolInterval.CandleList.TryGetValue(loop, out candleLast) && candlePrev != null)
            {
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



        int luxOverSold2 = 0;
        int luxOverBought2 = 0;
        CalculateOrg(symbol, out luxOverSold2, out luxOverBought2);


        if (luxOverSold != luxOverSold2 || luxOverBought != luxOverBought2)
            luxOverBought2 = 1234;
    }
}
