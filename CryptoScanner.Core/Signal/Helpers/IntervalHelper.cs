using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Helpers;

public static class IntervalHelper
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



    public static (bool result, CryptoSymbolInterval higherInterval, CryptoCandle? candle) 
        CalculateIndicatorsForInterval(this CryptoSymbolInterval symbolInterval, 
        CryptoSymbol symbol, CryptoCandle candle, CryptoSymbolInterval higherInterval)
    {
        var (targetComplete, targetStart) = IntervalTools.StartOfIntervalCandle3(candle.OpenTime, symbolInterval.Interval.Duration, higherInterval.Interval.Duration);
        if (!targetComplete)
            targetStart -= higherInterval.Interval.Duration;



        //long candleOpenTime = IntervalTools.StartOfIntervalCandle2(candle.OpenTime, symbolInterval.Interval.Duration, higherInterval.Interval.Duration);
        if (!higherInterval.CandleList.TryGetValue(targetStart, out CryptoCandle? higherCandle))
            return (false, higherInterval, null);

        // Calculate indicators if needed
        if (higherCandle.CandleData == null)
        {
            List<CryptoCandle>? history = CandleIndicatorData.CollectCandles(symbol, higherInterval.Interval, targetStart, out string _);
            if (history == null)
                return (false, higherInterval, higherCandle);
            CandleIndicatorData.CalculateIndicators(symbol, higherInterval.Interval, history);
        }

        return (true, higherInterval, higherCandle);
    }



    public static bool MacdRecoveryLong(this CryptoSymbolInterval symbolInterval, CryptoCandle? candleLast, int candleCount)
    {
        CryptoCandle last = candleLast!;

        while (candleCount-- > 0)
        {
            if (!symbolInterval.GetPrevCandle(last, out CryptoCandle? prev))
                return false;

            if (last.CandleData?.MacdHistogram <= prev!.CandleData?.MacdHistogram)
                return false;

            last = prev;
        }

        return true;
    }

    public static bool MacdRecoveryShort(this CryptoSymbolInterval symbolInterval, CryptoCandle? candleLast, int candleCount)
    {
        CryptoCandle last = candleLast!;

        while (candleCount-- > 0)
        {
            if (!symbolInterval.GetPrevCandle(last, out CryptoCandle? prev))
                return false;

            if (last.CandleData?.MacdHistogram >= prev!.CandleData?.MacdHistogram)
                return false;

            last = prev;
        }

        return true;
    }
}