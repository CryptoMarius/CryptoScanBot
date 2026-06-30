using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;
using CryptoScanner.Core.Signal.Sbm;

namespace CryptoScanner.Core.Signal.StochDir;

/// <summary>
/// Stochastic Directional Short strategy — fires at the actual lower-TF entry candle.
///
/// IsSignal (entry detection):
///   Fires when the lower-interval stoch %K crosses back below 70 after having been
///   overbought (≥ 80), AND the higher-interval shows a valid bearish MACD + stoch setup.
///
///   Lower-interval conditions (entry trigger):
///     - Current %K &lt; 70  (just crossed below the entry threshold)
///     - Previous %K ≥ 70  (confirms this is the exact crossing candle)
///     - Was overbought (both %K and %D ≥ 80) within the last LowerTfOverboughtLookback candles
///
///   Higher-interval conditions (checked at the current moment):
///     - MACD histogram currently negative (&lt; 0)
///     - MACD histogram was positive within the last MacdCrossoverLookback higher-TF candles
///       (ensures the bearish crossover is recent and not stale)
///     - Stoch %K in zone 40–70: has left overbought, downward move still has room
///     - Stoch %K (blue) below %D (red): bearish alignment confirmed
///     - Stoch not yet oversold (move not exhausted)
///
/// AllowStepIn:
///   Always true — the signal fires at the exact lower-TF entry moment.
///
/// Higher-interval mapping (~12x ratio, same convention as BBMA):
///   5m → 1h,  15m → 4h,  1h → 1d,  etc.
/// </summary>
public class SignalStochDirShort : SignalSbmBase
{
    // Stoch %K zone boundaries on the higher TF (bearish momentum range)
    private const double StochZoneLow = 30.0;
    private const double StochZoneHigh = 70.0;

    // Lower-TF entry: %K must cross back below this level after being overbought
    private const double LowerTfEntryThreshold = 70.0;

    // Max lower-TF candles to look back for a recent overbought condition
    private const int LowerTfOverboughtLookback = 15;

    // Max higher-TF candles to look back for the MACD zero-line crossover
    private const int MacdCrossoverLookback = 5;


    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
            || data.CandleData == null
            || data.CandleData.StochSignal == null
            || data.CandleData.StochOscillator == null)
            return false;

        return true;
    }


    /// <summary>
    /// Fires when the lower-TF stoch has just exited overbought AND the higher-TF shows
    /// a valid bearish setup (MACD recently crossed below zero, stoch in 40–70, %K below %D).
    /// Signal fires at the actual entry moment — statistics are measured from here.
    /// </summary>
    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // ── Step 1: lower-TF %K must have just crossed back below 70 ─────────────────────

        double stochKCurrent = CandleLast.CandleData!.StochOscillator!.Value;
        if (stochKCurrent >= LowerTfEntryThreshold)
        {
            ExtraText = $"lower TF %K ({stochKCurrent:N1}) not yet below {LowerTfEntryThreshold}";
            return false;
        }

        // Previous candle must have been at or above 70 — confirms this is the crossing candle
        if (!GetPrevCandle(CandleLast, out MyData? prevLow))
        {
            ExtraText = "no previous candle for lower TF threshold crossing check";
            return false;
        }
        if (prevLow!.CandleData.StochOscillator < LowerTfEntryThreshold)
        {
            ExtraText = $"lower TF %K did not just cross {LowerTfEntryThreshold} (prev was {prevLow.CandleData.StochOscillator:N1})";
            return false;
        }

        // Confirm there was a proper overbought situation before this crossing
        MyData? loop = prevLow;
        bool wasOverbought = false;
        for (int i = 0; i < LowerTfOverboughtLookback; i++)
        {
            if (!GetPrevCandle(loop, out loop))
                break;
            if (loop!.StochOverbought())
            {
                wasOverbought = true;
                break;
            }
        }
        if (!wasOverbought)
        {
            ExtraText = $"lower TF stoch was not overbought in last {LowerTfOverboughtLookback} candles before the crossing";
            return false;
        }

        // ── Step 2: resolve the higher directional interval ──────────────────────────────

        if (!StochHelper.GetStochDirHigherInterval(Interval.IntervalPeriod, out CryptoIntervalPeriod higherIntervalPeriod))
        {
            ExtraText = $"no valid higher interval for {Interval.Name}";
            return false;
        }

        var result = IndicatorEngine.CalculateIndicatorsForInterval(Symbol, Interval, CandleLast.Candle.OpenTime, higherIntervalPeriod);
        if (!result.success || result.candle == null || !IndicatorsOkay(result.candle))
        {
            ExtraText = $"no data for {result.higherInterval.Interval.Name}";
            return false;
        }

        MyData higherData = result.candle;
        CryptoInterval higherInterval = result.higherInterval.Interval;

        // ── Step 3: higher-TF MACD histogram must be negative ────────────────────────────

        if (higherData.CandleData!.MacdHistogram == null || higherData.CandleData.MacdHistogram >= 0)
        {
            ExtraText = $"{higherInterval.Name} MACD histogram not negative ({higherData.CandleData!.MacdHistogram:N4})";
            return false;
        }

        // MACD crossover must be recent: within the last MacdCrossoverLookback higher-TF candles
        // there must be at least one candle with histogram >= 0 (the crossover just happened)
        bool crossoverFound = false;
        MyData? walkHigher = higherData;
        for (int i = 0; i < MacdCrossoverLookback; i++)
        {
            if (walkHigher!.StochOversold())
            {
                return false;
            }
            if (!GetPrevCandle(higherInterval, walkHigher, out walkHigher))
                break;
            if (walkHigher!.CandleData.MacdHistogram >= 0)
            {
                crossoverFound = true;
                break;
            }
        }
        if (!crossoverFound)
        {
            ExtraText = $"{higherInterval.Name} MACD crossover not within last {MacdCrossoverLookback} candles — setup stale";
            return false;
        }

        // ── Step 4: higher-TF stoch zone and alignment checks ────────────────────────────

        // Stoch %K must be in zone 40–70 (has left overbought, downward move still has room)
        double stochK = higherData.CandleData!.StochOscillator!.Value;
        //if (stochK < StochZoneLow || stochK > StochZoneHigh)
        //{
        //    ExtraText = $"{higherInterval.Name} stoch %K ({stochK:N1}) outside zone [{StochZoneLow}–{StochZoneHigh}]";
        //    return false;
        //}

        // %K (blue) must be below %D (red) — bearish alignment
        double stochD = higherData.CandleData!.StochSignal!.Value;
        if (stochK >= stochD)
        {
            ExtraText = $"{higherInterval.Name} stoch %K ({stochK:N1}) not below %D ({stochD:N1})";
            return false;
        }

        // Higher TF stoch must not have reached oversold — downward move likely exhausted
        if (higherData.StochOverbought() || higherData.StochOversold())
        {
            ExtraText = $"{higherInterval.Name} stoch reached oversold — move likely done";
            return false;
        }

        ExtraText = $"{Interval.Name}/{higherInterval.Name}";
        return true;
    }

}
