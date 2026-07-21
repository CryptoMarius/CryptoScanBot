using CryptoScanner.Analyzers.Sbm;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Stobb.Signal;

public class SignalStobbMultiShort : SignalStobbBase
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
            response = "rsi niet overbought";
            return false;
        }

        if (StobbPlugin.Settings.OnlyIfPreviousStobb && HadStobbInThelastXCandles(SignalSide, 5, 60, StobbPlugin.Settings.UseLowHigh) == null)
        {
            response = "geen voorgaande stobb gevonden";
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


        CandleTime openTime = CandleLast.Candle.OpenTime;

        // Is it a signal valid over 4 intervals (multistorsi)
        int okay = 4;
        CryptoIntervalPeriod intervalPeriod = Interval.IntervalPeriod;
        for (int count = 6; count > 0; count--)
        {
            var result = IndicatorEngine.CalculateIndicatorsForInterval(Symbol, Interval, openTime, intervalPeriod);
            if (!result.success)
                return false;

            if (IndicatorsOkay(result.candle!) && result.candle!.StochOverbought()
                && result.candle!.IsAboveBollingerBands(StobbPlugin.Settings.UseLowHigh))
            {
                if (ExtraText != "")
                    ExtraText += ',';
                ExtraText += result.higherInterval.Interval.Name;

                okay--;
                if (okay == 0)
                {
                    return true;
                }
            }
            else
            {
                // first interval needs to be a signal
                if (count == 6)
                    return false;
            }

            if (intervalPeriod == CryptoIntervalPeriod.interval1w)
                return false;
            intervalPeriod++;
        }

        return false;
    }
}
