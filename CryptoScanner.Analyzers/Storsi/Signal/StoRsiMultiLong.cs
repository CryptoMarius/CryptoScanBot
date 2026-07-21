using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Analyzers.Storsi.Signal;

// WGHM - Wave Generation High Momentum

// https://www.tradingview.com/script/0F1sNM49-WGHBM/
// Momentum indicator that shows arrows when the Stochastic and the RSI are at the same time in the oversold or overbought area.

public class StoRsiMultiLong : StoRsiBase
{
    public override bool AdditionalChecks(MyData data, out string response)
    {
        var settings = StorsiPlugin.Settings;

        if (settings.CheckBollingerBandsCondition)
        {
            if (!InLowerPartOfBollingerBands(3, 5.0m, false))
            {
                response = "not in lower part of bb";
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

        // Requires both Stoch AND RSI to be oversold simultaneously across 4 consecutive timeframes.
        // This is intentionally strict: signals only fire during broad market selloffs where
        // oversold conditions have propagated from low to high timeframes.
        // Compare with stobb.multi which uses IsBelowBollingerBands instead of RsiOversold —
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

            if (IndicatorsOkay(result.candle!) && result.candle!.StochOversold(addStochAmount)
                && result.candle!.RsiOversold(settings.AddRsiAmount + addRsiAmount))
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
            // Make higher timeframes slightly LIGHTER on oversold (raises the threshold).
            // RsiOversold(corr) compares against (Oversold - corr); a negative correction
            // therefore relaxes the bar. Mirrors SignalStoRsiMultiShort which already runs
            // negative for the same "lighter on higher TFs" intent.
            addRsiAmount -= 2;
            addStochAmount -= 2;
        }


        return false;
    }

}
