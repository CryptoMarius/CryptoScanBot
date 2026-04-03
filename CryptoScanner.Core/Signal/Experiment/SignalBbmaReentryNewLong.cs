using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Experiment;

#if DEBUG
/// <summary>
/// BBMA Reentry Long entry signal (Oma Ally method).
///
/// Fires when the actual ENTRY condition is met on TF1, confirmed by the multi-timeframe
/// structure. Per the PDF (chapter 6): after a CSD on TF1, wait for price to correct back
/// into the 510 buy zone — THAT is the trade entry.
///
/// Entry condition (TF1=Reentry):
///   - Bullish CSD is active: LWMA5(low) > LWMA10(low)
///   - Price has pulled back to the 510 buy zone: close ≤ LWMA5(low)
///     OR wick retest: low below LWMA5, close above LWMA10
///
/// MTF structure required:
///   TF3 = Reentry (HTF directional anchor — already in reentry after its own CSD)
///   TF2 = any meaningful state (Reentry, Extreme, Mlv, MagicExtreme — confirms setup active on mid TF)
///   TF1 = Reentry (entry condition — the actual trade entry per PDF)
///
/// Fixed BBMA timeframe pairs:
///   5m→15m→1h,  15m→1h→4h,  1h→4h→1d,  4h→1d→1w
/// </summary>
public class SignalBbmaReentryNewLong : SignalBbmaBase
{
    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Ema50 == null
           || data.CandleData.Wma05Low == null
           || data.CandleData.Wma10Low == null
           || data.CandleData.BollingerBandsDeviation == null
           || data.CandleData.Sma20 == null
           || data.CandleData.BollingerBandsPercentage == null
           )
            return false;

        return true;
    }


    /// <summary>
    /// Classifies the BBMA state of a candle for Long setups (uses LWMA5/10 on lows).
    /// Priority: MagicExtreme → Extreme(TypeA) → Extreme(TypeB) → Extreme(Advance) → Reentry → Mlv → None
    ///
    /// allowWickDetection: disable for TF2/TF3 because their candles are still forming —
    /// wick levels are not yet final, but MA positions are reliable.
    /// </summary>
    private BbmaTfState ClassifyState(MyData data, bool allowWickDetection = true)
    {
        double wma5Low = data.CandleData!.Wma05Low!.Value;
        double wma10Low = data.CandleData!.Wma10Low!.Value;
        double bbLower = data.CandleData!.BollingerBandsLowerBand!.Value;

        // MagicExtreme (Magic Extreme): both MAs below BB.Lower
        if (wma5Low < bbLower && wma10Low < bbLower)
            return BbmaTfState.MagicExtreme;

        // Extreme (Type A): LWMA5(low) below BB.Lower
        if (wma5Low < bbLower)
            return BbmaTfState.Extreme;


        decimal low = data.Candle.Low;
        decimal close = data.Candle.Close;
        decimal open = data.Candle.Open;

        if (allowWickDetection)
        {
            // Extreme (Type B): wick rejection of BB.Lower
            decimal bbLowerDec = (decimal)bbLower;
            if (low < bbLowerDec && close > bbLowerDec && open > bbLowerDec)
                return BbmaTfState.Extreme;

            // Extreme (Advance): wick rejection of EMA50
            decimal ema50 = (decimal)data.CandleData!.Ema50!.Value;
            if (low < ema50 && close > ema50 && open > ema50)
                return BbmaTfState.Extreme;
        }

        // Reentry: bullish CSD active + price reached the 510 buy zone
        //   Standard : close at or below LWMA5(low) — in or beyond the zone
        //   MA Retest: wick dipped below LWMA5(low), close recovered above LWMA10(low)
        if (wma5Low > wma10Low)
        {
            decimal wma5Dec = (decimal)wma5Low;
            decimal wma10Dec = (decimal)wma10Low;
            bool priceInZone = close <= wma5Dec;
            bool maRetest = allowWickDetection && low < wma5Dec && close > wma10Dec;
            if (priceInZone || maRetest)
                return BbmaTfState.Reentry;
        }

        // Mlv (Market Loss Volume): LWMA5(low) above BB.Lower but below LWMA10(low) — pre-CSD
        if (wma5Low >= bbLower && wma5Low < wma10Low)
            return BbmaTfState.Mlv;

        return BbmaTfState.None;
    }


    /// <summary>
    /// Verifies that TF1 has recently transitioned through a BBMA alert phase (Mlv/E/EE)
    /// before the current Reentry. Per the PDF: entry is only valid when TF1 previously had
    /// an alert (Extreme or MLV). The pattern can repeat (M→R→M→R...) and expires as soon
    /// as price closes below SMA20 while CSD is still active (bullish reversal has failed).
    /// GetPrevCandle calls IndicatorsOkay internally — no manual null checks needed.
    /// </summary>
    private bool CheckTf1AlertHistory(out string reason)
    {
        reason = "";
        const int lookback = 20;

        MyData? candle = CandleLast;
        for (int i = 0; i < lookback; i++)
        {
            if (!GetPrevCandle(candle, out MyData? prev))
            {
                reason = $"TF1 alert: insufficient history ({i} candles checked)";
                return false;
            }

            candle = prev!;
            BbmaTfState state = ClassifyState(candle);

            // Found prior alert phase (Mlv/Extreme/MagicExtreme) — Reentry is valid
            if (state == BbmaTfState.Mlv || state == BbmaTfState.Extreme || state == BbmaTfState.MagicExtreme)
                return true;

            // CSD active (wma5 > wma10) but price closed below SMA20 — bullish reversal expired
            if (candle.CandleData!.Wma05Low!.Value > candle.CandleData!.Wma10Low!.Value
                && (double)candle.Candle.Close < candle.CandleData!.Sma20!.Value)
            {
                reason = "TF1 alert: pattern expired (CSD active but close below SMA20)";
                return false;
            }
        }

        reason = "TF1 alert: no prior Mlv/E/EE alert phase found in lookback";
        return false;
    }


    /// <summary>
    /// Verifies that the BB is flattening (narrowing) on TF1.
    /// Per Forex Nexus: the BB should be almost horizontal on the lowest interval at the
    /// moment of a trend reversal — indicating the explosive move is losing steam.
    /// Checks that BollingerBandsPercentage is lower now than N candles ago.
    /// </summary>
    private bool CheckBbFlattening(out string reason)
    {
        reason = "";
        const int lookback = 5;

        double currentBbPct = CandleLast.CandleData!.BollingerBandsPercentage!.Value;

        MyData? candle = CandleLast;
        for (int i = 0; i < lookback; i++)
        {
            if (!GetPrevCandle(candle, out MyData? prev))
            {
                reason = $"BB flattening: insufficient history ({i} candles)";
                return false;
            }
            candle = prev!;
        }

        double olderBbPct = candle.CandleData!.BollingerBandsPercentage!.Value;
        if (currentBbPct >= olderBbPct)
        {
            reason = $"BB not flattening: {currentBbPct:N2}% >= {olderBbPct:N2}% ({lookback} candles ago)";
            return false;
        }

        return true;
    }


    /// <summary>
    /// Verifies that TF2=Mlv is a genuine MLV (Market Loss Volume) phase per the PDF.
    /// Walking backwards from the current TF2 candle via GetPrevCandle:
    ///   - Each candle must have its low above BB.Lower (price fading away — "no longer makes it to BB").
    ///   - Once a prior Extreme is found (LWMA5(low) below BB.Lower), all candles between
    ///     that Extreme and the current Mlv candle have already been verified → MLV confirmed.
    ///   - If price touches BB.Lower before an Extreme is found, reject.
    ///   - If no Extreme is found within the lookback window, reject.
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

            // Prior Extreme found (Type A: LWMA5 below BB.Lower):
            // All candles between the current Mlv candle and this Extreme were already
            // verified not to touch BB.Lower → genuine MLV confirmed.
            if (wma5Low < bbLower)
                return true;

            // Price still reaching BB.Lower → not a genuine MLV phase per PDF.
            if (candle.Candle.Low <= (decimal)bbLower)
            {
                reason = "TF2 Mlv: price still reaching BB.Lower — MLV not confirmed";
                return false;
            }
        }

        reason = "TF2 Mlv: no prior Extreme found in lookback — not a genuine MLV";
        return false;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        //// BB width filter
        //if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        //{
        //    ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
        //    return false;
        //}

        // Step 1: TF1 must be in Reentry state — this IS the entry condition per PDF chapter 6:
        // CSD has occurred on TF1 and price has pulled back into the 510 buy zone.
        BbmaTfState state1 = ClassifyState(CandleLast);
        if (state1 != BbmaTfState.Reentry)
        {
            ExtraText = $"TF1 ({Interval.Name}) not in reentry state ({TfStateCode(state1)})";
            //GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        // Step 2: Verify TF1 history — there must have been a prior alert phase (Mlv/E/EE)
        // before this Reentry. Detects both valid setups and expired patterns (close below SMA20).
        if (!CheckTf1AlertHistory(out string alertReason))
        {
            ExtraText = alertReason;
            GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        // Step 3: BB must be flattening on TF1 — momentum fading on entry interval
        // Per Forex Nexus: BB nearly horizontal signals trend exhaustion / reversal point.
        //if (!CheckBbFlattening(out string bbReason))
        //{
        //    ExtraText = bbReason;
        //    GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
        //    return false;
        //}

        // Step 4: Resolve fixed BBMA higher timeframe pair
        if (!GetIntervals(out CryptoIntervalPeriod period2, out CryptoIntervalPeriod period3))
            return false;

        // Step 5: TF3 state — HTF directional anchor (computed first: trend is judged on highest TF)
        var result3 = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, period3);

        if (!result3.success || result3.candle == null || !IndicatorsOkay(result3.candle))
        {
            ExtraText = $"no data for TF3 ({result3.higherInterval.Interval.Name})";
            GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        // Trend filter on TF3 (highest TF): EMA50 below mid-BB (SMA20) = bullish bias
        // Per PDF: trend direction is determined on the highest timeframe, not on TF1.
        double ema50Tf3 = result3.candle.CandleData!.Ema50!.Value;
        double midBbTf3 = result3.candle.CandleData!.Sma20!.Value;
        if (ema50Tf3 >= midBbTf3)
        {
            ExtraText = $"TF3 EMA50 ({ema50Tf3:N6}) not below mid-BB — bearish bias on HTF, no Long";
            return false;
        }

        BbmaTfState state3 = ClassifyState(result3.candle, allowWickDetection: false);
        if (state3 != BbmaTfState.Reentry)
        {
            ExtraText = $"TF3 ({result3.higherInterval.Interval.Name}) not in Reentry state ({TfStateCode(state3)})";
            GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        // Step 6: TF2 state (wick detection disabled — candle still forming on higher TF)
        var result2 = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, period2);

        if (!result2.success || result2.candle == null || !IndicatorsOkay(result2.candle))
        {
            ExtraText = $"no data for TF2 ({result2.higherInterval.Interval.Name})";
            GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        BbmaTfState state2 = ClassifyState(result2.candle, allowWickDetection: false);
        if (state2 == BbmaTfState.None)
        {
            ExtraText = $"TF2 ({result2.higherInterval.Interval.Name}) has no clear BBMA state";
            GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
            return false;
        }

        // If TF2 is in MLV phase, verify it is genuine per the PDF:
        // a prior Extreme must exist and price must have faded from BB.Lower since then.
        if (state2 == BbmaTfState.Mlv)
        {
            if (!CheckMlv(result2.higherInterval.Interval, result2.candle, out string mlvReason))
            {
                ExtraText = mlvReason;
                GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
                return false;
            }
        }

        // MTF code: TF3→TF2→TF1 (highest to lowest).
        // Because TF1 is always R (entry condition) and TF3 is always R (HTF anchor),
        // the entry-phase codes are the PDF alert codes with TF1 replaced by R:
        //   PDF alert RRE  → entry code RRR  (TF2=Reentry)
        //   PDF alert REM  → entry code RER  (TF2=Extreme, from M alert)
        //   PDF alert REE  → entry code RER  (TF2=Extreme, from E alert)
        //   PDF alert RMEE → entry code RMR  (TF2=MLV, from MagicExtreme alert)
        string code = TfStateCode(state3) + TfStateCode(state2) + TfStateCode(state1);
        if (code == "RRR" || code == "RER" || code == "RMR")
        {
            ExtraText = $"{code} [{result3.higherInterval.Interval.Name}/{result2.higherInterval.Interval.Name}/{Interval.Name}]";
            return true;
        }

        ExtraText = $"invalid MTF code {code} [{result3.higherInterval.Interval.Name}/{result2.higherInterval.Interval.Name}/{Interval.Name}]";
        GlobalData.AddTextToLogTab($"BBMA {Symbol.Name} {Interval.Name} {SignalSide} {ExtraText}");
        return false;
    }
}
#endif
