using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

//#if EXTRASTRATEGIES
public class SignalSlopeSma50TurningNegative : SignalCreateBase
{
    public SignalSlopeSma50TurningNegative(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.SlopeSma50;
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.Ema50 == null)
           || (candle.CandleData.Sma50 == null)
           || (candle.CandleData.Sma200 == null)
           || (candle.CandleData.SlopeSma50 == null)
           || (candle.CandleData.Rsi == null)
           )
            return false;

        return true;
    }

    public override bool IsSignal()
    {
        ExtraText = "";

        if (CandleLast.CandleData.SlopeSma50 > 0)
            return false;

        if (CandleLast.CandleData.Sma50 < CandleLast.CandleData.Sma200)
            return false;

        if (!GetPrevCandle(CandleLast, out CryptoCandle prevCandle))
            return false;

        if (prevCandle.CandleData.SlopeSma50 < 0)
            return false;

        if (!BarometerHelper.CheckValidBarometer(Symbol.QuoteData, Interval.IntervalPeriod, (decimal.MinValue, -0.5m), out string reaction))
        {
            ExtraText = reaction;
            return false;
        }

        if (HadStobbInThelastXCandles(SignalSide, 0, 60) == null)
        {
            ExtraText = "Geen voorgaande STOB of SBM";
            return false;
        }

        return true;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Na de initiele melding hebben we 3 candles de tijd om in te stappen, maar alleen indien de MACD verbetering laat zien.

        // Er een candle onder de bb opent of sluit
        if (CandleLast.IsAboveBollingerBands(GlobalData.Settings.Signal.Sbm.UseLowHigh))
        {
            ExtraText = "Boven de bb";
            return false;
        }

        // De markt is nog niet echt negatief
        if (CandleLast.CandleData.Sma50 <= CandleLast.CandleData.Ema50)
            return false;


        if (CandleLast.CandleData.Rsi > 45)
        {
            ExtraText = string.Format("De RSI niet herstellend {0:N8} {1:N8} (last.2)", CandleLast.CandleData.Rsi, CandleLast.CandleData.Rsi);
            return false;
        }
        if (CandleLast.CandleData.StochOscillator > 40)
        {
            ExtraText = string.Format("De Stoch.K is niet laag genoeg {0:N8}", CandleLast.CandleData.StochOscillator);
            return false;
        }
        if (CandleLast.CandleData.StochSignal > 40)
        {
            ExtraText = string.Format("De Stoch.D is niet laag genoeg {0:N8}", CandleLast.CandleData.StochSignal);
            return false;
        }

        //ExtraText = "Alles lijkt goed";
        return true;
    }



    public override bool GiveUp(CryptoSignal signal)
    {
        ExtraText = "";

        //// De markt is nog niet echt positief
        //// (maar missen we meldingen hierdoor denk het wel!?)
        //if (CandleLast.CandleData.Sma50 < CandleLast.CandleData.Ema50)
        //{
        //    ExtraText = "SMA50 < EMA50";
        //    return true;
        //}

        // Langer dan 60 candles willen we niet wachten (is 60 niet heel erg lang?)
        if ((CandleLast.OpenTime - signal.EventTime) > 10 * Interval.Duration)
        {
            ExtraText = "Ophouden na 10 candles";
            return true;
        }

        ExtraText = "";
        return false;
    }


}
//#endif