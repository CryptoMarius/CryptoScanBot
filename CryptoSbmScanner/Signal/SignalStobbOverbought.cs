using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

public class SignalStobbOverbought : SignalBase
{
    public SignalStobbOverbought(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        ReplaceSignal = true;
        SignalMode = SignalMode.modeShort;
        SignalStrategy = SignalStrategy.stobbOverbought;
    }

    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if ((candle == null)
           || (candle.CandleData == null)
           || (candle.CandleData.Sma20 == null)
           || (candle.CandleData.StochSignal == null)
           || (candle.CandleData.StochOscillator == null)
           || (candle.CandleData.BollingerBandsDeviation == null)
           )
            return false;

        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.StobbBBMinPercentage, GlobalData.Settings.Signal.StobbBBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        // Er een candle onder de bb opent of sluit
        if (!CandleLast.IsAboveBollingerBands(GlobalData.Settings.Signal.StobbUseLowHigh))
        {
            ExtraText = "niet boven de bb";
            return false;
        }

        // Sprake van een oversold situatie (beide moeten onder de 20 zitten)
        if (!CandleLast.IsStochOverbought())
        {
            ExtraText = "stoch niet overbought";
            return false;
        }

        if (GlobalData.Settings.Signal.StobIncludeRsi && !CandleLast.IsRsiOverbought())
        {
            ExtraText = "rsi niet overbought";
            return false;
        }

        if (GlobalData.Settings.Signal.StobIncludeSoftSbm && !CandleLast.IsSbmConditionsOverbought(false))
        {
            ExtraText = "geen soft sbm condities";
            return false;
        }

        return true;
    }


    public override bool AllowStepIn(CryptoSignal signal)
    {
        return true;
    }



    public override bool GiveUp(CryptoSignal signal)
    {
        return false;
    }


}
