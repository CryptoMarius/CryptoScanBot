using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Bbma;

#if DEBUG
/// <summary>
/// BBMA Reentry Short — fires at the actual Reentry candle (Oma Ally method).
///
/// IsSignal (entry detection):
///   Fires when TF1 is currently in Reentry state AND a valid MTF alert code
///   (REM / RRE / REE / RMEE) was present within the last MaxWaitCandles candles.
///   The lookback walks back through TF1 history, skips any Reentry candles,
///   then validates TF2/TF3 states at the historical alert candle's OpenTime.
///
///   Valid alert codes (TF3→TF2→TF1):
///     REM  — TF3=Reentry, TF2=Extreme,     TF1=Mlv
///     RRE  — TF3=Reentry, TF2=Reentry,     TF1=Extreme
///     REE  — TF3=Reentry, TF2=Extreme,     TF1=Extreme
///     RMEE — TF3=Reentry, TF2=Mlv,         TF1=MagicExtreme
///
/// AllowStepIn:
///   Always true — the signal fires at the exact Reentry candle.
///
/// Fixed BBMA timeframe pairs:
///   5m→15m→1h,  15m→1h→4h,  1h→4h→1d,  4h→1d→1w
/// </summary>
public class SignalBbmaReentryNew1Short : SignalBbmaBase
{
    // Maximum TF1 candles to wait for a Reentry before giving up
    private const int MaxWaitCandles = 20;


    /// <summary>
    /// Verifies that TF2=Mlv is a genuine MHV (bearish mirror of MLV) phase per the PDF:
    /// Walking backwards from the TF2 candle, price must fade away from BB.Upper
    /// after a prior Extreme (LWMA5(high) above BB.Upper). If price still touches
    /// BB.Upper before the Extreme is found, the MHV is not genuine.
    /// </summary>
    private bool CheckMlv(CryptoInterval tf2Interval, MyData tf2Candle, out string reason)
    {
        reason = "";
        const int lookback = 15;

        MyData? candle = tf2Candle;
        for (int i = 0; i < lookback; i++)
        {
            if (!GetPrevCandle(tf2Interval, candle, out MyData? prev))
            {
                reason = $"TF2 Mlv: insufficient history ({i} candles checked)";
                return false;
            }

            candle = prev!;
            double wma5High = candle.CandleData!.Wma05High!.Value;
            double bbUpper = candle.CandleData!.BollingerBandsUpperBand!.Value;

            // Prior Extreme found: all candles between it and the Mlv candle already
            // verified not to touch BB.Upper → genuine MHV confirmed
            if (wma5High > bbUpper)
                return true;

            // Price still reaching BB.Upper → not a genuine MHV phase per PDF
            if (candle.Candle.High >= (decimal)bbUpper)
            {
                reason = "TF2 Mlv: price still reaching BB.Upper — MHV not confirmed";
                return false;
            }
        }

        reason = "TF2 Mlv: no prior Extreme found in lookback — not a genuine MHV";
        return false;
    }


    /// <summary>
    /// Fires when TF1 is currently in Reentry state AND a valid MTF alert code was present
    /// within the last MaxWaitCandles candles. Walks back through TF1 history, skipping any
    /// Reentry candles, then validates TF2/TF3 states at the historical alert candle's OpenTime.
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

        // Step 1: TF1 must currently be in Reentry state — this is the entry moment
        BbmaState state1Now = BbmaStateShort(CandleLast);
        if (state1Now != BbmaState.Reentry)
        {
            ExtraText = $"TF1 not in Reentry ({TfStateCode(state1Now)})";
            return false;
        }

        // Step 2: Resolve fixed BBMA higher timeframe pair
        if (!GetIntervals(out CryptoIntervalPeriod period2, out CryptoIntervalPeriod period3))
            return false;

        // Step 3: Walk back through TF1 history to find the preceding alert candle
        //   Skip any Reentry candles — the Reentry may have started a few candles ago.
        //   Stop at the first non-Reentry candle; that must be the alert candle.
        MyData? tf1 = CandleLast;
        for (int i = 0; i < MaxWaitCandles; i++)
        {
            if (!GetPrevCandle(tf1, out tf1))
            {
                ExtraText = $"insufficient TF1 history for lookback ({i} candles checked)";
                return false;
            }

            BbmaState state1 = BbmaStateShort(tf1!);

            // Still in Reentry — keep walking back to find the alert that preceded it
            if (state1 == BbmaState.Reentry)
                continue;

            // Found a non-Reentry candle — it must be an alert state for the setup to be valid
            if (state1 != BbmaState.Extreme && state1 != BbmaState.MagicExtreme && state1 != BbmaState.Mlv)
            {
                ExtraText = $"no valid alert before this Reentry (found {TfStateCode(state1)} at -{i + 1} candles)";
                return false;
            }

            // Step 4: Check TF3 state at the time of the historical alert candle
            var result3 = IndicatorDataList.CalculateIndicatorsForInterval(
                Symbol, Interval, tf1.Candle.OpenTime, period3);

            if (!result3.success || result3.candle == null || !IndicatorsOkay(result3.candle))
            {
                ExtraText = $"no data for TF3 ({result3.higherInterval.Interval.Name}) at alert candle";
                return false;
            }

            // Trend filter on TF3: EMA50 above mid-BB (SMA20) = bearish bias
            double ema50Tf3 = result3.candle.CandleData!.Ema50!.Value;
            double midBbTf3 = result3.candle.CandleData!.Sma20!.Value;
            if (ema50Tf3 <= midBbTf3)
            {
                ExtraText = $"TF3 EMA50 ({ema50Tf3:N6}) not above mid-BB at alert time — bullish bias, no Short";
                return false;
            }

            BbmaState state3 = BbmaStateShort(result3.candle, allowWickDetection: false);
            if (state3 != BbmaState.Reentry)
            {
                ExtraText = $"TF3 ({result3.higherInterval.Interval.Name}) not Reentry at alert time ({TfStateCode(state3)})";
                return false;
            }

            // Step 5: Check TF2 state at the time of the historical alert candle
            var result2 = IndicatorDataList.CalculateIndicatorsForInterval(
                Symbol, Interval, tf1.Candle.OpenTime, period2);

            if (!result2.success || result2.candle == null || !IndicatorsOkay(result2.candle))
            {
                ExtraText = $"no data for TF2 ({result2.higherInterval.Interval.Name}) at alert candle";
                return false;
            }

            BbmaState state2 = BbmaStateShort(result2.candle, allowWickDetection: false);

            // If TF2 was in MHV/MLV phase at the alert time, verify it was genuine per the PDF
            if (state2 == BbmaState.Mlv)
            {
                if (!CheckMlv(result2.higherInterval.Interval, result2.candle, out string mlvReason))
                {
                    ExtraText = mlvReason;
                    return false;
                }
            }

            // Step 6: Build the MTF alert code (TF3→TF2→TF1) and validate
            //   REM  — TF3=R, TF2=E, TF1=M   (MLV on TF1 after Extreme on TF2)
            //   RRE  — TF3=R, TF2=R, TF1=E   (Extreme on TF1, mid-TF already in reentry)
            //   REE  — TF3=R, TF2=E, TF1=E   (Extreme on both TF1 and TF2)
            //   RMEE — TF3=R, TF2=M, TF1=EE  (MagicExtreme on TF1, MLV on TF2)
            string code = TfStateCode(state3) + TfStateCode(state2) + TfStateCode(state1);
            if (code == "REM" || code == "RRE" || code == "REE" || code == "RMEE")
            {
                ExtraText = $"{code} (alert {i + 1} candle(s) ago) [{result3.higherInterval.Interval.Name}/{result2.higherInterval.Interval.Name}/{Interval.Name}]";
                //GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} REENTRY {ExtraText}");
                return true;
            }
        }

        ExtraText = $"no valid alert found within {MaxWaitCandles} candle lookback";
        return false;
    }
}
#endif
