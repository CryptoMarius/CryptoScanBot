using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Experiment;

#if DEBUG
/// <summary>
/// BBMA Reentry Short — Alert + Reentry two-phase approach (Oma Ally method).
///
/// Phase 1 — IsSignal (alert detection):
///   Fires when the MTF structure shows a BBMA alert on TF1, so we can start monitoring
///   for the actual Reentry entry. TF1 must be in an alert state (E / EE / M) that signals
///   a potential reversal is building up.
///
///   Valid alert codes (TF3→TF2→TF1):
///     REM  — TF3=Reentry, TF2=Extreme,     TF1=Mlv
///     RRE  — TF3=Reentry, TF2=Reentry,     TF1=Extreme
///     REE  — TF3=Reentry, TF2=Extreme,     TF1=Extreme
///     RMEE — TF3=Reentry, TF2=Mlv,         TF1=MagicExtreme
///
/// Phase 2 — AllowStepIn (entry):
///   Waits until TF1 transitions to Reentry state (price pulls back into the 510 sell
///   zone). Only then is the actual trade entry allowed per the PDF (chapter 6).
///
/// Phase 3 — GiveUp (expiry):
///   Abandons the signal when the setup expires:
///     - More than 20 TF1 candles elapsed without a Reentry, or
///     - CSD is still active (wma5 &lt; wma10) but price closed above SMA20
///       (the bearish reversal has failed).
///
/// Fixed BBMA timeframe pairs:
///   5m→15m→1h,  15m→1h→4h,  1h→4h→1d,  4h→1d→1w
/// </summary>
public class SignalBbmaReentryNew1Short : SignalBbmaBase
{
    // Maximum TF1 candles to wait for a Reentry before giving up
    private const int MaxWaitCandles = 20;

    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Ema50 == null
           || data.CandleData.Wma05High == null
           || data.CandleData.Wma10High == null
           || data.CandleData.BollingerBandsDeviation == null
           || data.CandleData.Sma20 == null
           || data.CandleData.BollingerBandsPercentage == null
           )
            return false;

        return true;
    }


    /// <summary>
    /// Classifies the BBMA state of a candle for Short setups (uses LWMA5/10 on highs).
    /// Priority: MagicExtreme → Extreme(TypeA) → Extreme(TypeB) → Extreme(Advance) → Reentry → Mlv → None
    ///
    /// allowWickDetection: disable for TF2/TF3 because their candles are still forming —
    /// wick levels are not yet final, but MA positions are reliable.
    /// </summary>
    private BbmaTfState ClassifyState(MyData data, bool allowWickDetection = true)
    {
        double wma5High = data.CandleData!.Wma05High!.Value;
        double wma10High = data.CandleData!.Wma10High!.Value;
        double bbUpper = data.CandleData!.BollingerBandsUpperBand!.Value;

        // MagicExtreme (EE): both MAs above BB.Upper
        if (wma5High > bbUpper && wma10High > bbUpper)
            return BbmaTfState.MagicExtreme;

        // Extreme (Type A): LWMA5(high) above BB.Upper
        if (wma5High > bbUpper)
            return BbmaTfState.Extreme;

        decimal high = data.Candle.High;
        decimal close = data.Candle.Close;
        decimal open = data.Candle.Open;

        if (allowWickDetection)
        {
            // Extreme (Type B): wick rejection of BB.Upper
            decimal bbUpperDec = (decimal)bbUpper;
            if (high > bbUpperDec && close < bbUpperDec && open < bbUpperDec)
                return BbmaTfState.Extreme;

            // Extreme (Advance): wick rejection of EMA50
            decimal ema50 = (decimal)data.CandleData!.Ema50!.Value;
            if (high > ema50 && close < ema50 && open < ema50)
                return BbmaTfState.Extreme;
        }

        // Reentry: bearish CSD active + price reached the 510 sell zone
        //   Standard : close at or above LWMA5(high) — in or beyond the zone
        //   MA Retest: wick spiked above LWMA5(high), close recovered below LWMA10(high)
        if (wma5High < wma10High)
        {
            decimal wma5Dec = (decimal)wma5High;
            decimal wma10Dec = (decimal)wma10High;
            bool priceInZone = close >= wma5Dec;
            bool maRetest = allowWickDetection && high > wma5Dec && close < wma10Dec;
            if (priceInZone || maRetest)
                return BbmaTfState.Reentry;
        }

        // Mlv (MHV phase): LWMA5(high) below BB.Upper but above LWMA10(high) — pre-CSD
        if (wma5High <= bbUpper && wma5High > wma10High)
            return BbmaTfState.Mlv;

        return BbmaTfState.None;
    }


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
    /// Phase 1: Fire when the MTF alert code is REM / RRE / REE / RMEE.
    /// TF1 is in an alert state (Extreme, MagicExtreme, or Mlv). The actual entry
    /// is deferred to AllowStepIn, which waits for TF1 to reach Reentry.
    /// </summary>
    public override bool IsSignal()
    {
        ExtraText = "";

        // Step 1: TF1 must be in an alert state — the setup is building up
        BbmaTfState state1 = ClassifyState(CandleLast);
        if (state1 != BbmaTfState.Extreme && state1 != BbmaTfState.MagicExtreme && state1 != BbmaTfState.Mlv)
        {
            ExtraText = $"TF1 not in alert state ({TfStateCode(state1)})";
            return false;
        }

        // Step 2: Resolve fixed BBMA higher timeframe pair
        if (!GetIntervals(out CryptoIntervalPeriod period2, out CryptoIntervalPeriod period3))
            return false;

        // Step 3: TF3 — HTF directional anchor must be Reentry + bearish EMA50 filter
        var result3 = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, period3);

        if (!result3.success || result3.candle == null || !IndicatorsOkay(result3.candle))
        {
            ExtraText = $"no data for TF3 ({result3.higherInterval.Interval.Name})";
            //GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        // Trend filter on TF3: EMA50 above mid-BB (SMA20) = bearish bias
        double ema50Tf3 = result3.candle.CandleData!.Ema50!.Value;
        double midBbTf3 = result3.candle.CandleData!.Sma20!.Value;
        if (ema50Tf3 <= midBbTf3)
        {
            ExtraText = $"TF3 EMA50 ({ema50Tf3:N6}) not above mid-BB — bullish bias on HTF, no Short";
            return false;
        }

        BbmaTfState state3 = ClassifyState(result3.candle, allowWickDetection: false);
        if (state3 != BbmaTfState.Reentry)
        {
            ExtraText = $"TF3 ({result3.higherInterval.Interval.Name}) not Reentry ({TfStateCode(state3)})";
            //GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        // Step 4: TF2 state (wick detection disabled — candle still forming on higher TF)
        var result2 = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, period2);

        if (!result2.success || result2.candle == null || !IndicatorsOkay(result2.candle))
        {
            ExtraText = $"no data for TF2 ({result2.higherInterval.Interval.Name})";
            //GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        BbmaTfState state2 = ClassifyState(result2.candle, allowWickDetection: false);

        // If TF2 is in MHV/MLV phase, verify it is genuine per the PDF
        if (state2 == BbmaTfState.Mlv)
        {
            if (!CheckMlv(result2.higherInterval.Interval, result2.candle, out string mlvReason))
            {
                ExtraText = mlvReason;
                //GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
                return false;
            }
        }

        // Step 5: Build the MTF alert code (TF3→TF2→TF1) and validate
        //   REM  — TF3=R, TF2=E, TF1=M   (MLV on TF1 after Extreme on TF2)
        //   RRE  — TF3=R, TF2=R, TF1=E   (Extreme on TF1, mid-TF already in reentry)
        //   REE  — TF3=R, TF2=E, TF1=E   (Extreme on both TF1 and TF2)
        //   RMEE — TF3=R, TF2=M, TF1=EE  (MagicExtreme on TF1, MLV on TF2)
        string code = TfStateCode(state3) + TfStateCode(state2) + TfStateCode(state1);
        if (code == "REM" || code == "RRE" || code == "REE" || code == "RMEE")
        {
            ExtraText = $"{code} [{result3.higherInterval.Interval.Name}/{result2.higherInterval.Interval.Name}/{Interval.Name}]";
            //GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} ALERT {ExtraText}");
            return true;
        }

        ExtraText = $"invalid alert code {code} [{result3.higherInterval.Interval.Name}/{result2.higherInterval.Interval.Name}/{Interval.Name}]";
        return false;
    }


    /// <summary>
    /// Phase 2: Allow entry only once TF1 has reached Reentry state.
    /// Called on every new candle after the alert signal was created.
    /// </summary>
    public override bool AllowStepIn(CryptoSignal signal)
    {
        BbmaTfState state1 = ClassifyState(CandleLast);
        if (state1 != BbmaTfState.Reentry)
        {
            ExtraText = $"waiting Reentry — TF1 currently {TfStateCode(state1)}";
            return false;
        }

        ExtraText = "Reentry reached — entry allowed";
        return true;
    }


    /// <summary>
    /// Phase 3: Abandon the signal when the setup has expired.
    ///   - More than MaxWaitCandles elapsed without a Reentry, or
    ///   - CSD still active (wma5 &lt; wma10) but price closed above SMA20
    ///     (the bearish reversal has definitively failed).
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        ExtraText = "";

        // Too many candles elapsed without a Reentry
        if (CandleTime.FromDateTime(signal.CloseDate).Minutes + MaxWaitCandles * Interval.Duration < CandleLast?.Candle.OpenTime.Minutes)
        {
            ExtraText = $"Stop after {GlobalData.Settings.Trading.EntryRemoveTime} candles";
            return true;
        }

        // Pattern invalidated: CSD still active but price closed above SMA20
        // — the reversal move has failed and a genuine Reentry will not follow
        double wma5High = CandleLast.CandleData!.Wma05High!.Value;
        double wma10High = CandleLast.CandleData!.Wma10High!.Value;
        double sma20 = CandleLast.CandleData!.Sma20!.Value;
        if (wma5High < wma10High && (double)CandleLast.Candle.Close > sma20)
        {
            ExtraText = "GiveUp: CSD active but close above SMA20 — bearish reversal failed";
            return true;
        }

        return false;
    }
}
#endif
