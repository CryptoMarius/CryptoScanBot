using CryptoScanner.Analyzers.Sbm;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Stobb.Signal;

public class SignalStobbShort : SignalStobbBase
{

    public override bool AdditionalChecks(MyData data, out string response)
    {
        if (StobbPlugin.Settings.OnlyIfLux5m)
        {
            int needed = StobbPlugin.Settings.Lux5mPercentage;
            if (CandleLast.CandleData!.Lux5mValue < needed)
            {
                response = $"lux 5m not overbought enough ({CandleLast.CandleData!.Lux5mValue}%, need >= {needed}%)";
                return false;
            }
        }

        // Controle op de ma-lijnen
        if (StobbPlugin.Settings.IncludeSoftSbm)
        {
            // Check ma lines
            if (!CandleLast!.IsSbmConditionsOverbought())
            {
                response = "no sbm conditions";
                return false;
            }
        }

        // Controle op de ma-kruisingen
        if (StobbPlugin.Settings.IncludeSbmPercAndCrossing)
        {
            var sbm = SbmPlugin.Settings;
            if (sbm.CheckMa200AndMa50Percentage &&
                !data.IsPercentageSma200AndSma50OkayOverbought(sbm.Ma200AndMa50Percentage, out response))
                return false;
            if (sbm.CheckMa200AndMa20Percentage &&
                !data.IsPercentageSma200AndSma20OkayOverbought(sbm.Ma200AndMa20Percentage, out response))
                return false;
            if (sbm.CheckMa50AndMa20Percentage &&
                !data.IsPercentageSma50AndSma20OkayOverbought(sbm.Ma50AndMa20Percentage, out response))
                return false;

            if (!CheckMaCrossings(
                sbm.Ma200AndMa20Crossing, sbm.Ma200AndMa20Lookback,
                sbm.Ma200AndMa50Crossing, sbm.Ma200AndMa50Lookback,
                sbm.Ma50AndMa20Crossing, sbm.Ma50AndMa20Lookback,
                out response))
                return false;
        }

        // Controle op de RSI
        if (StobbPlugin.Settings.IncludeRsi && !CandleLast.RsiOverbought())
        {
            response = "rsi not overbought";
            return false;
        }

        if (StobbPlugin.Settings.OnlyIfPreviousStobb && HadStobbInThelastXCandles(SignalSide, 5, 60, StobbPlugin.Settings.UseLowHigh) == null)
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
        var settings = StobbPlugin.Settings;

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(settings.BBMinPercentage, settings.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // Er een data onder de bb opent of sluit
        if (!CandleLast.IsAboveBollingerBands(settings.UseLowHigh))
        {
            ExtraText = "not above bb.upper";
            return false;
        }

        // Sprake van een overbought situatie (beide moeten onder de 20 zitten)
        if (!CandleLast.StochOverbought())
        {
            ExtraText = "stoch not overbought";
            return false;
        }


        // ********************************************************************
        // Dont trade against the trend (only check current interval)
        if (settings.CheckTrendPrimaryDirection && !CheckTrendPrimary(settings.TrendPrimaryDirectionCount))
            return false;
        if (settings.CheckTrendSecondaryDirection && !CheckTrendSecondary(settings.TrendSecondaryDirectionCount))
            return false;

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