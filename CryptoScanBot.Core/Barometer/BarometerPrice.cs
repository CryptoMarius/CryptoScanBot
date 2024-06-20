using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Barometer;

internal class BarometerPrice
{
    public static bool CalculatePriceBarometer(CryptoQuoteData quoteData, SortedList<string, CryptoSymbol> symbols, CryptoInterval interval, long unixCandleLast, out decimal barometerPerc)
    {
        // Wat is de candle in het vorige interval
        long unixCandlePrev = unixCandleLast - interval.Duration;

        // debug
        //DateTime dateCandlePrev = CandleTools.GetUnixDate(unixCandlePrev);
        //DateTime dateCandleLast = CandleTools.GetUnixDate(unixCandleLast);

        decimal sumPerc = 0;
        int coinsMatching = 0;

        for (int i = 0; i < quoteData.SymbolList.Count; i++) // foreach with ToList() is overkill
        {
            CryptoSymbol symbol = quoteData.SymbolList[i];
            if (symbol.CandleList.TryGetValue(unixCandlePrev, out CryptoCandle? candlePrev) && symbol.CandleList.TryGetValue(unixCandleLast, out CryptoCandle? candleLast))
            {
                if (candlePrev != null && candleLast != null) // Er worden in kucoin null candles toegevoegd?
                {
                    decimal perc;
                    decimal diff = candleLast.Close - candlePrev.Close;
                    if (!candlePrev.Close.Equals(0))
                        perc = 100m * (diff / candlePrev.Close);
                    else perc = 0;

                    sumPerc += perc;
                    coinsMatching++;
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

        return coinsMatching > 0; // Met 1 munt krijgen we okay, mhhhh geeft een aardig vertekend beeld in dat geval..
    }

}
