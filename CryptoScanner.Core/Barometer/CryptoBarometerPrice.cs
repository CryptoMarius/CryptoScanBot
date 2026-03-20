using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Barometer;

internal class CryptoBarometerPrice
{
    // Outlier threshold: symbols with an absurdly high/low percentage are skipped
    // (guards against corrupted candle.Close values caused by race conditions on decimal reads)
    private const decimal OutlierThreshold = 200m;

    public static bool CalculatePriceBarometer(CryptoQuoteData quoteData, SortedList<string, CryptoSymbol> symbols,
        CryptoInterval interval, CandleTime unixCandleLast, out decimal barometerPerc)
    {
        // Wat is de candle in het vorige interval
        CandleTime unixCandlePrev = unixCandleLast - interval.Duration;

        // debug
        //DateTime dateCandlePrev = CandleTools.GetUnixDate(unixCandlePrev);
        //DateTime dateCandleLast = CandleTools.GetUnixDate(unixCandleLast);

        decimal sumPerc = 0;
        int coinsMatching = 0;

        for (int i = 0; i < quoteData.SymbolList.Count; i++)
        {
            CryptoSymbol symbol = quoteData.SymbolList[i];

            if (symbol.QuoteData!.FetchCandles && !symbol.IsBarometerSymbol() && symbol.EnoughVolume())
            {
                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(Enums.CryptoIntervalPeriod.interval1m);
                if (symbolInterval.CandleList.TryGetValue(unixCandlePrev, out CryptoCandle candlePrev) &&
                    symbolInterval.CandleList.TryGetValue(unixCandleLast, out CryptoCandle candleLast))
                {
                    //if (candlePrev != null && candleLast != null) // Er worden in kucoin null candles toegevoegd?
                    {
                        decimal perc;
                        decimal diff = candleLast.Close - candlePrev.Close;
                        if (!candlePrev.Close.Equals(0))
                            perc = 100m * (diff / candlePrev.Close);
                        else perc = 0;

                        // Detect and log anomaly (possible corruption due to torn decimal read)
                        // Fix for weird values
                        // Sometimes even 700...900 times higher than x hours ago, what is the base problem?
                        if (Math.Abs(perc) > OutlierThreshold)
                        {
                            GlobalData.AddTextToLogTab($"BAROMETER ANOMALY {symbol.Name} {interval.Name} " +
                                $"prev={candlePrev.Close} ({unixCandlePrev.ToLocalTime()}) " +
                                $"last={candleLast.Close} ({unixCandleLast.ToLocalTime()}) " +
                                $"perc={perc:F2}% (skipped)");
                            continue; // Skip outlier
                        }

                        sumPerc += perc;
                        coinsMatching++;
                    }
                }
            }
        }

        if (coinsMatching > 0)
        {
            decimal result = sumPerc / coinsMatching;
            barometerPerc = decimal.Round(result, 8);
        }
        else
            barometerPerc = 0m; // not -99 because of long/short.
        //GlobalData.AddTextToLogTab($"Barometer {quoteData.Name} ({quoteData.SymbolList.Count}) {interval.Name} {barometerPerc:N2} {coinsMatching}");


        return coinsMatching > 0; // Met 1 munt krijgen we okay, mhhhh geeft een aardig vertekend beeld in dat geval..
    }

}
