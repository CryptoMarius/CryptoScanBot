using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Barometer;

internal class CryptoBarometerPrice
{
    // Outlier threshold: symbols with an absurdly high/low percentage are skipped
    // (guards against corrupted candle.Close values caused by race conditions on decimal reads)
    private const decimal OutlierThreshold = 250m;

    public static bool CalculatePriceBarometer(CryptoQuoteData quoteData, SortedList<string, CryptoSymbol> symbols,
        CryptoInterval interval, CandleTime unixCandleLast, BarometerResult result)
    {
        return CalculatePriceBarometer(quoteData, quoteData.SymbolList, interval, unixCandleLast, result);
    }


    /// <summary>
    /// The same measurement over an explicit symbol list instead of the whole quote coin. The
    /// emulator hands in the symbols of its run: during a replay the rest of the quote has no
    /// candles in memory at all, so walking them would be a lookup per symbol per replayed minute
    /// that can only miss.
    /// </summary>
    public static bool CalculatePriceBarometer(CryptoQuoteData quoteData, IReadOnlyList<CryptoSymbol> symbolList,
        CryptoInterval interval, CandleTime unixCandleLast, BarometerResult result)
    {
        // Wat is de candle in het vorige interval
        CandleTime unixCandlePrev = unixCandleLast - interval.Duration;

        // debug
        //DateTime dateCandlePrev = CandleTools.GetUnixDate(unixCandlePrev);
        //DateTime dateCandleLast = CandleTools.GetUnixDate(unixCandleLast);

        // The caller reuses one result object across all measurements of a run, so clear it first.
        // The percentages are collected instead of only summed: median, breadth and spread all come
        // out of that same list afterwards, without a single extra candle lookup.
        result.Reset();

        // Bitcoin does not trade under the same name everywhere - XBT on Kucoin Perpetual, UBTC on
        // HyperLiquid Spot - and ExchangeOptions.PauseSymbol already carries the right name for this
        // exchange. Take its base coin, so the same coin is found against whatever quote this
        // barometer is for (the pause symbol itself is against the default quote).
        string bitcoinBase = "";
        if (GlobalData.ActiveExchange != null &&
            GlobalData.ActiveExchange.TryGetSymbolByPair(Exchange.ExchangeBase.ExchangeOptions.PauseSymbol, out CryptoSymbol? bitcoinSymbol))
            bitcoinBase = bitcoinSymbol.Base;

        for (int i = 0; i < symbolList.Count; i++)
        {
            CryptoSymbol symbol = symbolList[i];

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
                            //GlobalData.AddTextToLogTab($"BAROMETER ANOMALY {symbol.Name} {interval.Name} " +
                            //    $"prev={candlePrev.Close} ({unixCandlePrev.ToLocalTime()}) " +
                            //    $"last={candleLast.Close} ({unixCandleLast.ToLocalTime()}) " +
                            //    $"perc={perc:F2}% (skipped)");
                            result.OutlierCount++; // Count them, so a silent data problem becomes visible
                            continue; // Skip outlier
                        }

                        result.Add(perc);

                        // Bitcoin counts as an ordinary coin above; this only remembers it so it can
                        // be compared against the median afterwards.
                        if (bitcoinBase.Length > 0 && symbol.Base == bitcoinBase)
                            result.SetBitcoin(perc);
                    }
                }
            }
        }

        // Calculate() leaves everything at zero when no coin took part - not -99 because of long/short.
        //GlobalData.AddTextToLogTab($"Barometer {quoteData.Name} ({quoteData.SymbolList.Count}) {interval.Name} {result.Average:N2} {result.SymbolCount}");


        return result.Calculate(); // Met 1 munt krijgen we okay, mhhhh geeft een aardig vertekend beeld in dat geval..
    }

}
