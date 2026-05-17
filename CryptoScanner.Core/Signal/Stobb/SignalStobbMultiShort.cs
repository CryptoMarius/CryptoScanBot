using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Sbm;

namespace CryptoScanner.Core.Signal.Stobb;

public class SignalStobbMultiShort : SignalSbmBase
{


    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Sma20 == null
           || data.CandleData.StochSignal == null
           || data.CandleData.StochOscillator == null
           || data.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    public override bool AdditionalChecks(MyData data, out string response)
    {
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
                !data.IsPercentageSma200AndSma50OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa50Percentage, out response))
                return false;
            if (GlobalData.Settings.Signal.Sbm.CheckMa200AndMa20Percentage &&
                !data.IsPercentageSma200AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma200AndMa20Percentage, out response))
                return false;
            if (GlobalData.Settings.Signal.Sbm.CheckMa50AndMa20Percentage &&
                !data.IsPercentageSma50AndSma20OkayOverbought(GlobalData.Settings.Signal.Sbm.Ma50AndMa20Percentage, out response))
                return false;

            if (!CheckMaCrossings(out response))
                return false;
        }

        // Controle op de RSI
        if (GlobalData.Settings.Signal.Stobb.IncludeRsi && !CandleLast.RsiOverbought())
        {
            response = "rsi niet overbought";
            return false;
        }

        if (GlobalData.Settings.Signal.Stobb.OnlyIfPreviousStobb && HadStobbInThelastXCandles(SignalSide, 5, 60) == null)
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

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }


        CandleTime openTime = CandleLast.Candle.OpenTime;

        // Is it a signal valid over 4 intervals (multistorsi)
        int okay = 4;
        ExtraText = "";
        CryptoIntervalPeriod intervalPeriod = Interval.IntervalPeriod;
        for (int count = 6; count > 0; count--)
        {
            var result = IndicatorDataList.CalculateIndicatorsForInterval(Symbol, Interval, openTime, intervalPeriod);
            if (!result.success)
                return false;

            if (IndicatorsOkay(result.candle!) && result.candle!.StochOverbought()
                && result.candle!.IsAboveBollingerBands(GlobalData.Settings.Signal.Stobb.UseLowHigh))
            {
                if (ExtraText != "")
                    ExtraText += ',';
                ExtraText += result.higherInterval.Interval.Name;

                okay--;
                if (okay == 0)
                    return true;
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


        // ********************************************************************
        // Dont trade against the trend (only check current interval)
        if (GlobalData.Settings.Signal.Stobb.CheckTrendPrimaryDirection && !CheckTrendPrimary())
            return false;
        if (GlobalData.Settings.Signal.Stobb.CheckTrendSecondaryDirection && !CheckTrendSecondary())
            return false;
        //// close date shouw be in the lower part of the bb
        //if (!InLowerPartOfBollingerBands(1, 10.0m))
        //    return false;

        ExtraText = "";
        return false;
    }


}

