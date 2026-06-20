using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Stobb;

public class SignalStobbLong : SignalStobbBase
{

    public override bool AdditionalChecks(MyData data, out string response)
    {
        if (GlobalData.Settings.Signal.Stobb.OnlyIfLux5m)
        {
            int needed = GlobalData.Settings.Signal.Stobb.Lux5mPercentage;
            if (CandleLast.CandleData!.Lux5mValue > -needed)
            {
                response = $"lux 5m not oversold enough ({CandleLast.CandleData!.Lux5mValue}%, need <= -{needed}%)";
                return false;
            }
        }

        // Controle op de ma-lijnen
        if (GlobalData.Settings.Signal.Stobb.IncludeSoftSbm)
        {
            // Check ma lines
            if (!CandleLast!.IsSbmConditionsOverbought())
            {
                response = "no sbm conditions";
                return false;
            }
        }

        // Controle op de ma-kruisingen
        if (GlobalData.Settings.Signal.Stobb.IncludeSbmPercAndCrossing)
        {
            if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa50Percentage &&
                !data.IsPercentageSma200AndSma50OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                return false;
            if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa20Percentage &&
                !data.IsPercentageSma200AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                return false;
            if (GlobalData.Settings.Signal.Sbm.CheckMa50AndMa20Percentage &&
                !data.IsPercentageSma50AndSma20OkayOversold(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
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
        var settings = GlobalData.Settings.Signal.Stobb;

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(settings.BBMinPercentage, settings.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // Er een data onder de bb opent of sluit
        if (!CandleLast.IsBelowBollingerBands(settings.UseLowHigh))
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


        // ********************************************************************
        // Dont trade against the trend (only check current interval)
        if (settings.CheckTrendPrimaryDirection)
        {
            if (!CheckTrendPrimary(settings.TrendPrimaryDirectionCount))
                return false;
        }

        if (settings.CheckTrendSecondaryDirection)
        {
            if (!CheckTrendSecondary(settings.TrendSecondaryDirectionCount))
                return false;
        }


        // Optional zone-rejection confirmation (DLZ / FVG / SMC). OR over enabled types.
        if (!CheckEnabledZoneRejections(out string zoneInfo))
        {
            ExtraText = zoneInfo;
            return false;
        }
        if (zoneInfo.Length > 0)
            ExtraText = $"stobb+{zoneInfo}";

        return true;
    }
}
