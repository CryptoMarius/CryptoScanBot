using CryptoScanner.Analyzers.Sbm;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Stobb.Signal;

public class SignalStobbLong : SignalStobbBase
{

    public override bool AdditionalChecks(MyData data, out string response)
    {
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
                !data.IsPercentageSma200AndSma50OkayOversold(sbm.Ma200AndMa50Percentage, out response))
                return false;
            if (sbm.CheckMa200AndMa20Percentage &&
                !data.IsPercentageSma200AndSma20OkayOversold(sbm.Ma200AndMa20Percentage, out response))
                return false;
            if (sbm.CheckMa50AndMa20Percentage &&
                !data.IsPercentageSma50AndSma20OkayOversold(sbm.Ma50AndMa20Percentage, out response))
                return false;

            if (!CheckMaCrossings(
                sbm.Ma200AndMa20Crossing, sbm.Ma200AndMa20Lookback,
                sbm.Ma200AndMa50Crossing, sbm.Ma200AndMa50Lookback,
                sbm.Ma50AndMa20Crossing, sbm.Ma50AndMa20Lookback,
                out response))
                return false;
        }

        // Controle op de RSI
        if (StobbPlugin.Settings.IncludeRsi && !CandleLast.RsiOversold())
        {
            response = "rsi not oversold";
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


        return true;
    }
}
