namespace CryptoScanBot.Core.Barometer;

// Old methods calculating the volume barometer
// This is still interesting (if we can get it to work properly without killing the cpu)

internal class BarometerVolume
{
    //private decimal CheckVolume(CryptoSymbol symbol, CryptoInterval interval, long unixDate)
    //{
    //    CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);

    //    // Als het eerder berekend is geef deze dan terug
    //    decimal volume;
    //    if (symbolPeriod.BarometerVolumeList.TryGetValue(unixDate, out volume))
    //        return volume;

    //    // Bestaat het voorgaande volume? Dan gebruiken we die
    //    // (volume van eerste candle eraf en volume van laatste candle erbij)
    //    if (symbolPeriod.BarometerVolumeList.TryGetValue(unixDate - 60, out volume))
    //    {
    //        CryptoCandle candleLast, candleFirst;
    //        if ((symbol.CandleList.TryGetValue(unixDate, out candleLast)) && (symbol.CandleList.TryGetValue(unixDate - interval.Duration + 60, out candleFirst)))
    //        {
    //            volume = volume - candleFirst.Volume + candleLast.Volume;
    //            symbolPeriod.BarometerVolumeList.Add(unixDate, volume);
    //            return volume;
    //        }
    //    }


    //    // Het sluit niet aan, berekenen uit de voorgaande candles (cpu intensief, maar is als het goed is eenmalig)
    //    // het gevaar bestaat dat we een niet correcte volume retourneren als de exchange een storing of onderhoud
    //    // heeft gehad waardoor er een onderbreking in candles is geweest (dan gaat per ongeluk goed denk ik..)
    //    volume = 0;            
    //    for (long intervalLoop = 0; intervalLoop < interval.Duration; intervalLoop += 60)
    //    {
    //        CryptoCandle candle;
    //        if (symbol.CandleList.TryGetValue(unixDate - intervalLoop, out candle))
    //            volume += candle.Volume;
    //    }
    //    symbolPeriod.BarometerVolumeList.Add(unixDate, volume);
    //    return volume;
    //}

    //private bool CalculateVolumeBarometer(CryptoQuoteData quoteData, SortedList<string, CryptoSymbol> symbols, CryptoInterval interval, long unixCandleLast, out decimal barometerPerc)
    //{
    //    //Wat is de candle in het vorige interval
    //    long unixCandlePrev = unixCandleLast - interval.Duration;

    //    // Ter debug van de intervallen (unix datetime's zijn slecht leesbaar)
    //    //DateTime dateCandlePrev = CandleTools.GetUnixDate(unixCandlePrev);
    //    //DateTime dateCandleLast = CandleTools.GetUnixDate(unixCandleLast);

    //    //if (quoteData.Name == "BTC")
    //    //    GlobalData.AddTextToLogTab(string.Format("Calculating volume barometer charts start for {0} {1}", quoteData.Name, interval.Name));

    //    decimal sumPerc = 0;
    //    int coinsMatching = 0;
    //    foreach (var symbol in quoteData.SymbolList)
    //    {
    //        CryptoCandle candlePrev, candleLast;
    //        if (symbol.CandleList.TryGetValue(unixCandlePrev, out candlePrev) && symbol.CandleList.TryGetValue(unixCandleLast, out candleLast))
    //        {
    //            decimal volumePrev = CheckVolume(symbol, interval, unixCandlePrev);
    //            decimal volumeLast = CheckVolume(symbol, interval, unixCandleLast);

    //            decimal perc;
    //            decimal diff = (volumeLast - volumePrev);
    //            if (!volumePrev.Equals(0))
    //                perc = 100m * (diff / volumePrev);
    //            else perc = 0;

    //            sumPerc += perc;
    //            coinsMatching++;
    //        }
    //    }

    //    if (coinsMatching > 0)
    //    {
    //        decimal result = sumPerc / coinsMatching;
    //        barometerPerc = decimal.Round(result, 8);
    //    }
    //    else
    //        barometerPerc = 0m;

    //    // Met 1 munt krijgen we dus een true, mhhhh
    //    return coinsMatching > 0;
    //}

}
