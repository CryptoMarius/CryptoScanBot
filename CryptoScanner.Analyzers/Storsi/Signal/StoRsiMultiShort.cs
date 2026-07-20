using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Storsi.Signal;

// WGHM - Wave Generation High Momentum

// https://www.tradingview.com/script/0F1sNM49-WGHBM/
// Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.

public class StoRsiMultiShort : StoRsiBase
{
    public override bool AdditionalChecks(MyData data, out string response)
    {
        var settings = StorsiPlugin.Settings;
        if (settings.OnlyIfLux5m)
        {
            int needed = settings.Lux5mPercentage;
            if (CandleLast.CandleData!.Lux5mValue < needed)
            {
                response = $"lux 5m not overbought enough ({CandleLast.CandleData!.Lux5mValue}%, need >= {needed}%)";
                return false;
            }
        }

        if (settings.CheckBollingerBandsCondition)
        {
            if (!InUpperPartOfBollingerBands(3, 5.0m, false))
            {
                response = "not in upper part of bb";
                return false;
            }
        }

        if (settings.SkipFirstSignal)
        {
            if (HadStorsiInThelastXCandles(SignalSide, 1, 3) == null)
            {
                response = "skip first storsi";
                return false;
            }
        }

        response = "";
        return true;
    }


    public override bool IsSignal()
    {
        ExtraText = "";
        var settings = StorsiPlugin.Settings;

        if (!CandleLast.CheckBollingerBandsWidth(settings.BBMinPercentage, settings.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }
        CandleTime openTime = CandleLast.Candle.OpenTime;

        // Requires both Stoch AND RSI to be overbought simultaneously across 4 consecutive timeframes.
        // This is intentionally strict: signals only fire during broad market rallies where
        // overbought conditions have propagated from low to high timeframes.
        // Compare with stobb.multi which uses IsAboveBollingerBands instead of RsiOverbought —
        // a price condition that persists longer across timeframes, so that variant fires more often.
        int okay = 4;
        int addRsiAmount = 0;
        int addStochAmount = 0;
        CryptoIntervalPeriod intervalPeriod = Interval.IntervalPeriod;
        for (int count = 6; count > 0; count--)
        {
            var result = IndicatorEngine.CalculateIndicatorsForInterval(Symbol, Interval, openTime, intervalPeriod);
            if (!result.success)
                return false;

            if (IndicatorsOkay(result.candle!) && result.candle!.StochOverbought(addStochAmount)
                && result.candle!.RsiOverbought(settings.AddRsiAmount + addRsiAmount))
            {
                if (ExtraText != "")
                    ExtraText += ',';
                ExtraText += result.higherInterval.Interval.Name;

                okay--;
                if (okay == 0)
                {
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
                        ExtraText += " " + zoneInfo;

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
            addRsiAmount -= 2;
            addStochAmount -= 2;
        }


        return false;
    }

}
