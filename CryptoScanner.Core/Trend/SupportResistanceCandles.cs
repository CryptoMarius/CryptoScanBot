using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Trend;

/// <summary>
/// Helpers around <see cref="SupportResistance"/> for the places that use it inside the scanner and
/// the emulator: collecting the candles to scan, and whether the short filter is switched on anywhere.
/// </summary>
public static class SupportResistanceCandles
{
    /// <summary>
    /// Up to <paramref name="count"/> candles of <paramref name="interval"/>, oldest first, ending with
    /// the candle that opened at <paramref name="lastOpen"/>. The first missing candle ends the window.
    /// </summary>
    public static List<CryptoCandle> Collect(CryptoSymbol symbol, CryptoInterval interval, CandleTime lastOpen, int count)
    {
        CryptoCandleList list = symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;
        List<CryptoCandle> candles = new(count);
        CandleTime time = lastOpen;
        while (candles.Count < count && list.TryGetValue(time, out CryptoCandle candle))
        {
            candles.Add(candle);
            if (time < interval.Duration)
                break;
            time -= interval.Duration;
        }
        candles.Reverse();
        return candles;
    }


    /// <summary>
    /// Whether SkipShortAboveSupport is on in the trader's entry conditions or in those of any
    /// strategy. The emulator keeps a deeper 1h history when it is (see IndicatorWarmup.WarmupDepth),
    /// the same 500 candles the live scanner keeps, so both see the same levels.
    /// </summary>
    public static bool ShortFilterActive()
    {
        if (GlobalData.Settings.Trading.EntryConditions.SkipShortAboveSupport)
            return true;
        foreach (var (_, entry) in GlobalData.StrategiesSettings)
        {
            if (entry.strategySettings.EntryConditions?.SkipShortAboveSupport == true)
                return true;
        }
        return false;
    }
}
