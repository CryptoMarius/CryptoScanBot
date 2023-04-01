using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;


public class SignalSlopeSma50TurningPositive : SignalBase
{
    public SignalSlopeSma50TurningPositive(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        SignalMode = SignalMode.modeLong;
        SignalStrategy = SignalStrategy.slopeSma50TurningPositive;
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

        if (SymbolInterval.LastStobbOrdSbmDate == null)
            return false;

        if (CandleLast.CandleData.SlopeSma50 < 0)
            return false;

        if (CandleLast.CandleData.Sma50 > CandleLast.CandleData.Sma200)
            return false;

        CryptoCandle prevCandle;
        if (!Candles.TryGetValue(CandleLast.OpenTime - Interval.Duration, out prevCandle))
        {
            ExtraText = "geen prev candle! " + CandleLast.DateLocal.ToString();
            return false;
        }
        if (!IndicatorsOkay(prevCandle))
            return false;

        if (prevCandle.CandleData.SlopeSma50 > 0)
            return false;

        return true;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        // Na de initiele melding hebben we 3 candles de tijd om in te stappen, maar alleen indien de MACD verbetering laat zien.

        // Er een candle onder de bb opent of sluit
        if (CandleLast.IsBelowBollingerBands(GlobalData.Settings.Signal.SbmUseLowHigh))
        {
            ExtraText = "beneden de bb";
            return false;
        }

        // De markt is nog niet echt positief
        // (maar missen we meldingen hierdoor denk het wel!?)
        if (CandleLast.CandleData.Sma50 >= CandleLast.CandleData.Ema50)
            return false;


        CryptoCandle candlePrev;
        if (!Candles.TryGetValue(CandleLast.OpenTime - Interval.Duration, out candlePrev))
        {
            ExtraText = "No prev1";
            return false;
        }

        // Herstel? Verbeterd de RSI
        if ((CandleLast.CandleData.Rsi < candlePrev.CandleData.Rsi))
        {
            ExtraText = string.Format("De RSI niet herstellend {0:N8} {1:N8} (last.1)", candlePrev.CandleData.Rsi, CandleLast.CandleData.Rsi);
            return false;
        }

        if ((CandleLast.CandleData.Rsi < 55))
        {
            ExtraText = string.Format("De RSI niet herstellend {0:N8} {1:N8} (last.2)", candlePrev.CandleData.Rsi, CandleLast.CandleData.Rsi);
            return false;
        }
        if ((CandleLast.CandleData.StochOscillator < 60))
        {
            ExtraText = string.Format("De Stoch.K is niet hoog genoeg {0:N8}", CandleLast.CandleData.StochOscillator);
            return false;
        }
        if ((CandleLast.CandleData.StochSignal < 60))
        {
            ExtraText = string.Format("De Stoch.D is niet hoog genoeg {0:N8}", CandleLast.CandleData.StochSignal);
            return false;
        }

        //ExtraText = "Alles lijkt goed";
        return true;
    }



    public override bool GiveUp(CryptoSignal signal)
    {
        ExtraText = "";

        // De markt is nog niet echt positief
        // (maar missen we meldingen hierdoor denk het wel!?)
        if (CandleLast.CandleData.Sma50 > CandleLast.CandleData.Ema50)
        {
            ExtraText = "SMA50 > EMA50";
            return true;
        }

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
