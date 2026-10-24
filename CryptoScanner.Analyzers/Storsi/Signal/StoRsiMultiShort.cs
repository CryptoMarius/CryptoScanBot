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

        // Requires both Stoch AND RSI to be overbought simultaneously on the signal interval and MultiHigherTimeframes higher timeframes.
        // This is intentionally strict: signals only fire during broad market rallies where
        // overbought conditions have propagated from low to high timeframes.
        // Compare with stobb.multi which uses IsAboveBollingerBands instead of RsiOverbought —
        // a price condition that persists longer across timeframes, so that variant fires more often.
        // The signal interval plus MultiHigherTimeframes higher intervals must match (default 3, so 4 in total).
        // The search window is the signal interval plus 5 higher ones, widened when more are required.
        int addRsiAmount = 0;
        int addStochAmount = 0;
        int okay = 1 + Math.Max(1, settings.MultiHigherTimeframes);
        int higherIntervalCount = Math.Max(6, okay);
        CryptoIntervalPeriod intervalPeriod = Interval.IntervalPeriod;
        for (int count = higherIntervalCount; count > 0; count--)
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
                    return true;
                }
            }
            else
            {
                // first interval needs to be a signal
                if (count == window)
                    return false;
            }

            if (intervalPeriod == CryptoIntervalPeriod.interval1w)
                return false;
            intervalPeriod++;
            // Higher timeframes slightly LIGHTER on overbought, by MultiCorrectionStep (default 2)
            addRsiAmount -= Math.Max(0, settings.MultiCorrectionStep);
            addStochAmount -= Math.Max(0, settings.MultiCorrectionStep);
        }


        return false;
    }

}
