using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Experiment;

public class SignalSma20Crossed50Short : SignalCreateBase
{
    public SignalSma20Crossed50Short(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.Sma20 == null)
           || (candle.CandleData.Sma50 == null)
           )
            return false;

        return true;
    }

    public override bool IsSignal()
    {
        ExtraText = "";

        if (Interval.IntervalPeriod < CryptoIntervalPeriod.interval10m)
            return false;


        if (CandleLast.CandleData!.Sma20 > CandleLast.CandleData.Sma50)
            return false;

        if (!GetPrevCandle(CandleLast, out CryptoCandle? prevCandle))
            return false;

        if (prevCandle!.CandleData!.Sma20 < prevCandle.CandleData.Sma50)
            return false;

        int count = 15;
        double diff = 0;
        CryptoCandle lastCandle = CandleLast;
        while (count-- > 0)
        {
            if (!GetPrevCandle(lastCandle, out prevCandle))
                return false;

            double x = Math.Abs(lastCandle.CandleData!.Sma20!.Value - prevCandle!.CandleData!.Sma50!.Value);
            x = 100 * (x / prevCandle!.CandleData!.Sma50!.Value);
            if (x > diff)
                diff = x;

            lastCandle = prevCandle!;
        }
        if (diff < 0.5)
            return false;

        return true;
    }




}
