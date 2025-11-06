using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Signal.Other;

public class SignalSlopeSma20Short : SignalCreateBase
{
    public SignalSlopeSma20Short(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.SlopeSma20 == null)
           || (candle.CandleData.Rsi == null)
           )
            return false;

        return true;
    }

    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        if (HadStobbInThelastXCandles(SignalSide, 0, 40) == null && HadStorsiInThelastXCandles(SignalSide, 0, 40) == null)
        {
            response = "No previous stobb/storsi";
            return false;
        }

        response = "";
        return true;
    }
    
    
    public override bool IsSignal()
    {
        ExtraText = "";

        int count = 20;
        bool slopeChanged = false;
        bool slopeLevelReached = false;
        CryptoCandle lastCandle = CandleLast;
        var slopeLastSma = CandleLast.CandleData!.SlopeSma20;

        while (count-- > 0)
        {
            if (lastCandle.CandleData!.SlopeSma20 <= -slopeLastSma)
                slopeLevelReached = true;

            if (!GetPrevCandle(lastCandle, out CryptoCandle? prevCandle))
                return false;

            if (lastCandle.CandleData!.SlopeSma20 > 0 && prevCandle!.CandleData!.SlopeSma20 < 0)
                slopeChanged = true;

            lastCandle = prevCandle!;
        }

        if (!slopeChanged || !slopeLevelReached)
            return false;

        return true;
    }
}
