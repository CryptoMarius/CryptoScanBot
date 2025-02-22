using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal;

public static class IndicatorHelper
{

    public static bool GetPrevCandle(this CryptoSymbolInterval symbolInterval, CryptoCandle? oldCandle, out CryptoCandle? newCandle)
    {
        if (oldCandle == null)
        {
            newCandle = null;
            return false;
        }

        if (!symbolInterval.CandleList.TryGetValue(oldCandle.OpenTime - symbolInterval.Interval.Duration, out newCandle))
            return false;

        return true;
    }

}