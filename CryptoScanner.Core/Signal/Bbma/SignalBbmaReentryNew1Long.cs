using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Bbma;

#if DEBUG
/// <summary>
/// BBMA Reentry Long — fires at the actual Reentry candle (Oma Ally method).
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
public class SignalBbmaReentryNew1Long : SignalBbmaBase
{
    // Maximum TF1 candles to wait for a Reentry before giving up
    private const int MaxWaitCandles = 20;

    /// <summary>
    /// Verifies that TF2=Mlv is a genuine MLV phase per the PDF:
    /// Walking backwards from the TF2 candle, price must fade away from BB.Lower
    /// after a prior Extreme (LWMA5(low) below BB.Lower). If price still touches
    /// BB.Lower before the Extreme is found, the MLV is not genuine.
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
            double wma5Low = candle.CandleData!.Wma05Low!.Value;
            double bbLower = candle.CandleData!.BollingerBandsLowerBand!.Value;

            // Prior Extreme found: all candles between it and the Mlv candle already
            // verified not to touch BB.Lower → genuine MLV confirmed
            if (wma5Low < bbLower)
                return true;

            // Price still reaching BB.Lower → not a genuine MLV phase per PDF
            if (candle.Candle.Low <= (decimal)bbLower)
            {
                reason = "TF2 Mlv: price still reaching BB.Lower — MLV not confirmed";
                return false;
            }
        }

        reason = "TF2 Mlv: no prior Extreme found in lookback — not a genuine MLV";
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
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }


        // Step 1: TF1 must currently be in Reentry state — this is the entry moment
        BbmaState stateLtfNow = BbmaStateLong(CandleLast);
        if (stateLtfNow != BbmaState.Reentry)
        {
            ExtraText = $"TF1 not in Reentry ({TfStateCode(stateLtfNow)})";
            return false;
        }

        // Step 2: Resolve fixed BBMA higher timeframe pair
        if (!GetIntervals(out CryptoIntervalPeriod mtf, out CryptoIntervalPeriod htf))
            return false;

        // Step 3: Walk back through TF1 history to find the preceding alert candle
        //   Skip any Reentry candles — the Reentry may have started a few candles ago.
        //   Stop at the first non-Reentry candle; that must be the alert candle.
        MyData? dataLtf = CandleLast;
        for (int i = 0; i < MaxWaitCandles; i++)
        {
            if (!GetPrevCandle(dataLtf, out dataLtf) || dataLtf == null)
            {
                ExtraText = $"insufficient TF1 history for lookback ({i} candles checked)";
                return false;
            }

            BbmaState stateLtf = BbmaStateLong(dataLtf);

            // Still in Reentry — keep walking back to find the alert that preceded it
            if (stateLtf == BbmaState.Reentry)
                continue;

            // Found a non-Reentry candle — it must be an alert state for the setup to be valid
            if (stateLtf != BbmaState.Extreme && stateLtf != BbmaState.MagicExtreme && stateLtf != BbmaState.Mlv)
            {
                ExtraText = $"no valid alert before this Reentry (found {TfStateCode(stateLtf)} at -{i + 1} candles)";
                return false;
            }

            // Step 4: Check TF3 state at the time of the historical alert candle
            var resultHtf = IndicatorDataList.CalculateIndicatorsForInterval(
                Symbol, Interval, dataLtf.Candle.OpenTime, htf);

            if (!resultHtf.success || resultHtf.candle == null || !IndicatorsOkay(resultHtf.candle))
            {
                ExtraText = $"no data for TF3 ({resultHtf.higherInterval.Interval.Name}) at alert candle";
                return false;
            }

            // Trend filter on TF3: EMA50 below mid-BB (SMA20) = bullish bias
            double ema50Tf3 = resultHtf.candle.CandleData!.Ema50!.Value;
            double midBbTf3 = resultHtf.candle.CandleData!.Sma20!.Value;
            if (ema50Tf3 >= midBbTf3)
            {
                ExtraText = $"TF3 EMA50 ({ema50Tf3:N6}) not below mid-BB at alert time — bearish bias, no Long";
                return false;
            }

            BbmaState stateHtf = BbmaStateLong(resultHtf.candle, allowWickDetection: false);
            if (stateHtf != BbmaState.Reentry)
            {
                ExtraText = $"TF3 ({resultHtf.higherInterval.Interval.Name}) not Reentry at alert time ({TfStateCode(stateHtf)})";
                return false;
            }

            // Step 5: Check TF2 state at the time of the historical alert candle
            var resultMtf = IndicatorDataList.CalculateIndicatorsForInterval(
                Symbol, Interval, dataLtf.Candle.OpenTime, mtf);

            if (!resultMtf.success || resultMtf.candle == null || !IndicatorsOkay(resultMtf.candle))
            {
                ExtraText = $"no data for TF2 ({resultMtf.higherInterval.Interval.Name}) at alert candle";
                return false;
            }

            BbmaState stateMtf = BbmaStateLong(resultMtf.candle, allowWickDetection: false);

            // If TF2 was in MLV phase at the alert time, verify it was genuine per the PDF
            if (stateMtf == BbmaState.Mlv)
            {
                if (!CheckMlv(resultMtf.higherInterval.Interval, resultMtf.candle, out string mlvReason))
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
            string code = TfStateCode(stateHtf) + TfStateCode(stateMtf) + TfStateCode(stateLtf);
            if (code == "REM" || code == "RRE" || code == "REE" || code == "RMEE")
            {
                ExtraText = $"{code} {resultHtf.higherInterval.Interval.Name}/{resultMtf.higherInterval.Interval.Name}/{Interval.Name} (alert {i + 1} candle(s) ago)";
                return true;
            }
        }

        ExtraText = $"no valid alert found within {MaxWaitCandles} candle lookback";
        return false;
    }
}
#endif
