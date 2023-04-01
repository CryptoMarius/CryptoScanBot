﻿using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;


public class SignalSlopeSma50TurningNegative : SignalBase
{
    public SignalSlopeSma50TurningNegative(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalMode = SignalMode.modeLong;
        SignalStrategy = SignalStrategy.slopeSma50TurningNegative;
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.Sma20 == null)
           || (candle.CandleData.Sma50 == null)
           || (candle.CandleData.Sma200 == null)
           || (candle.CandleData.SlopeSma20 == null)
           || (candle.CandleData.SlopeSma50 == null)
           )
            return false;

        return true;
    }

    public override bool IsSignal()
    {
        ExtraText = "";

        //if (CandleLast.CandleData.Slope20 > 0)
        //  return false;

        if (CandleLast.CandleData.SlopeSma50 > 0)
            return false;

        if (CandleLast.CandleData.Sma50 < CandleLast.CandleData.Sma200)
            return false;

        if (!GetPrevCandle(CandleLast, out CryptoCandle prevCandle))
            return false;

        if (prevCandle.CandleData.SlopeSma50 < 0)
            return false;

        return true;
    }



}
