using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Helpers;

public static class IntervalHelper
{
    public static (bool result, CryptoSymbolInterval higherInterval, MyData? candle) 
        CalculateIndicatorsForInterval(this SignalCreateBase myBase, CryptoSymbolInterval symbolInterval, 
        CryptoSymbol symbol, MyData candle, CryptoSymbolInterval higherInterval)
    {
        var (targetComplete, targetStart) = IntervalTools.StartOfIntervalCandle3(candle.Candle.OpenTime, symbolInterval.Interval.Duration, higherInterval.Interval.Duration);
        if (!targetComplete)
            targetStart -= higherInterval.Interval.Duration;

        if (!higherInterval.CandleList.TryGetValue(targetStart, out CryptoCandle _))
            return (false, higherInterval, null);

        // Calculate indicators if needed
        myBase.IndicatorDataList.PrepareIndicators(symbol, higherInterval.Interval, targetStart, out _);
        if (!myBase.IndicatorDataList.TryGetCandle(higherInterval.Interval, targetStart, out MyData? higherCandle))
            return (false, higherInterval, null);

        return (true, higherInterval, higherCandle);
    }



    public static bool MacdRecoveryLong(this SignalCreateBase myBase, CryptoSymbolInterval symbolInterval, MyData? candleLast, int candleCount)
    {
        MyData last = candleLast!;

        while (candleCount-- > 0)
        {
            if (!myBase.GetPrevCandle(symbolInterval.Interval, last, out MyData? prev))
                return false;

            if (last.CandleData?.MacdHistogram <= prev!.CandleData?.MacdHistogram)
                return false;

            last = prev;
        }

        return true;
    }

    public static bool MacdRecoveryShort(this SignalCreateBase myBase, CryptoSymbolInterval symbolInterval, MyData? candleLast, int candleCount)
    {
        MyData last = candleLast!;

        while (candleCount-- > 0)
        {
            if (!myBase.GetPrevCandle(symbolInterval.Interval, last, out MyData? prev))
                return false;

            if (last.CandleData?.MacdHistogram >= prev!.CandleData?.MacdHistogram)
                return false;

            last = prev;
        }

        return true;
    }
}