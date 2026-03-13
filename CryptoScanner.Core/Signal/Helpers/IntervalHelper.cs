using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Helpers;

public static class IntervalHelper
{
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