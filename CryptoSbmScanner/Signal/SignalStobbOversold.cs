using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Signal;

public class SignalStobbOversold : SignalSbmBaseOversold // inherit from sbm because of 1 call there
{
    public SignalStobbOversold(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        //ReplaceSignal = true;
        SignalSide = CryptoTradeSide.Long;
        SignalStrategy = CryptoSignalStrategy.Stobb;
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


    public override string DisplayText()
    {
        return string.Format("stoch.oscillator={0:N8} stoch.signal={1:N8}",
            CandleLast.CandleData.StochOscillator,
            CandleLast.CandleData.StochSignal
        );
    }


    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        // Controle op de ma-lijnen
        if (GlobalData.Settings.Signal.Stobb.IncludeSoftSbm)
        {
            if (!CandleLast.IsSbmConditionsOversold(false))
            {
                response = "geen sbm condities";
                return false;
            }
        }

        // Controle op de ma-kruisingen
        if (GlobalData.Settings.Signal.Stobb.IncludeSbmPercAndCrossing)
        {
            if (!candle.IsSma200AndSma50OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                return false;
            if (!candle.IsSma200AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                return false;
            if (!candle.IsSma50AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
                return false;

            if (!CheckMaCrossings(out response))
                return false;
        }

        // Controle op de RSI
        if (GlobalData.Settings.Signal.Stobb.IncludeRsi && !CandleLast.IsRsiOversold())
        {
            response = "rsi niet oversold";
            return false;
        }


        response = "";
        return true;
    }

    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = "bb.width te klein " + CandleLast.CandleData.BollingerBandsPercentage?.ToString("N2");
            return false;
        }

        // Er een candle onder de bb opent of sluit
        if (!CandleLast.IsBelowBollingerBands(GlobalData.Settings.Signal.Stobb.UseLowHigh))
        {
            ExtraText = "niet beneden de bb.lower";
            return false;
        }

        // Sprake van een oversold situatie (beide moeten onder de 20 zitten)
        if (!CandleLast.IsStochOversold())
        {
            ExtraText = "stoch niet oversold";
            return false;
        }

        return true;
    }

}

