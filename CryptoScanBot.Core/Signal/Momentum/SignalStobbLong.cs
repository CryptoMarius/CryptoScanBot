using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Signal.Momentum;

public class SignalStobbLong : SignalSbmBaseLong
{
    public SignalStobbLong(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle candle) : base(symbol, interval, candle)
    {
        // nothing
    }


    public override bool IndicatorsOkay(CryptoCandle candle)
    {
        if (candle == null
           || candle.CandleData == null
           || candle.CandleData.Sma20 == null
           || candle.CandleData.StochSignal == null
           || candle.CandleData.StochOscillator == null
           || candle.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    public override string DisplayText()
    {
        return string.Format("stoch.oscillator={0:N8} stoch.signal={1:N8}",
            CandleLast!.CandleData!.StochOscillator,
            CandleLast!.CandleData!.StochSignal
        );
    }



    public override bool AdditionalChecks(CryptoCandle candle, out string response)
    {
        if (GlobalData.Settings.Signal.StoRsi.OnlyIfLux5m)
        {
            if (CandleLast.CandleData!.Lux5mValue > -50)
            {
                response = $"lux 5m not oversold enough ({CandleLast.CandleData!.Lux5mValue}%)";
                return false;
            }
        }

        // Controle op de ma-lijnen
        if (GlobalData.Settings.Signal.Stobb.IncludeSoftSbm)
        {
            if (!CandleLast!.SbmConditionsOversold(false))
            {
                response = "no sbm conditions";
                return false;
            }
        }

        // Controle op de ma-kruisingen
        if (GlobalData.Settings.Signal.Stobb.IncludeSbmPercAndCrossing)
        {
            if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa50Percentage &&
                !candle.Sma200AndSma50OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                return false;
            if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa20Percentage &&
                !candle.Sma200AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                return false;
            if (GlobalData.Settings.Signal.Sbm.CheckMa50AndMa20Percentage &&
                !candle.Sma50AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
                return false;

            if (!CheckMaCrossings(out response))
                return false;
        }

        // Controle op de RSI
        if (GlobalData.Settings.Signal.Stobb.IncludeRsi && !CandleLast.RsiOversold())
        {
            response = "rsi not oversold";
            return false;
        }

        if (GlobalData.Settings.Signal.Stobb.OnlyIfPreviousStobb && HadStobbInThelastXCandles(SignalSide, 5, 60) == null)
        {
            response = "no previous stobb found";
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
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // Er een candle onder de bb opent of sluit
        if (!CandleLast.IsBelowBollingerBands(GlobalData.Settings.Signal.Stobb.UseLowHigh))
        {
            ExtraText = "not below bb.lower";
            return false;
        }

        // Sprake van een oversold situatie (beide moeten onder de 20 zitten)
        if (!CandleLast.StochOversold())
        {
            ExtraText = "stoch not oversold";
            return false;
        }

        return true;
    }


}

