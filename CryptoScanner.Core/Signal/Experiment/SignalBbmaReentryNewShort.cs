using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.Core.Signal.Experiment;

#if DEBUG
/// <summary>
/// BBMA Multi-Timeframe Short setup signal (Oma Ally method).
///
/// Fires when a valid BBMA MTF code pattern is detected across the 3-TF system.
/// Based on the BBMA MTF table (chapter 7), reading top-to-bottom = highest TF to lowest TF.
///
/// Valid BBMA Short patterns (TF3 → TF2 → TF1):
///   REM  : TF3=Reentry, TF2=Extreme,  TF1=MHV        (most common)
///   RRE  : TF3=Reentry, TF2=Reentry,  TF1=Extreme    (strong confirmation)
///   REE  : TF3=Reentry, TF2=Extreme,  TF1=Extreme    (double extreme)
///   RMEE : TF3=Reentry, TF2=MHV,      TF1=MagicExtrm (strongest, rarest)
///
/// IMPORTANT: TF1 (signal TF) is NEVER "R" in a valid BBMA setup code.
/// The signal fires BEFORE the CSD on TF1, when TF1 is still in M, Extreme, or MagicExtreme phase.
/// After the signal, the user waits for the CSD on TF1 and enters on the 510 sell zone.
///
/// State classifications per timeframe (Short, using High indicators):
///   MagicExtreme = Magic Extreme : both LWMA5(high) and LWMA10(high) are above BB.Upper
///   Extreme  = Extreme       : LWMA5(high) above BB.Upper, or wick/EMA50 rejection
///   M  = MHV phase     : LWMA5(high) below BB.Upper but still above LWMA10(high)
///   R  = Reentry       : active bearish CSD (LWMA5 < LWMA10) + close in the 510 sell zone
///
/// Fixed BBMA timeframe pairs:
///   5m→15m→1h,  15m→1h→4h,  1h→4h→1d,  4h→1d→1w
/// </summary>
public class SignalBbmaReentryNewShort : SignalBbmaBase
{
    private enum BbmaTfState { None, M, Extreme, MagicExtreme, R }


    public override bool IndicatorsOkay(MyData data)
    {
        if (data == null
           || data.Candle.OpenTime == 0
           || data.CandleData == null
           || data.CandleData.Ema50 == null
           || data.CandleData.Wma05High == null
           || data.CandleData.Wma10High == null
           || data.CandleData.BollingerBandsDeviation == null
           )
            return false;

        return true;
    }


    /// <summary>
    /// Returns the display string for a BBMA state in the MTF code.
    /// MagicExtreme (Magic Extreme) maps to "MagicExtreme" so RMEE is correctly shown as a 4-character code.
    /// </summary>
    private static string TfStateCode(BbmaTfState state) => state switch
    {
        BbmaTfState.MagicExtreme => "EE",
        BbmaTfState.Extreme => "E",
        BbmaTfState.M => "M",
        BbmaTfState.R => "R",
        _ => "-"
    };


    /// <summary>
    /// Classifies the current BBMA state of a candle for Short setups.
    /// Uses LWMA5(high), LWMA10(high), BB.Upper, and candle OHLC.
    /// Priority order: MagicExtreme → Extreme (Type A) → Extreme (Type B) → Extreme (Advance) → R → M → None
    ///
    /// allowWickDetection: when false (used for TF2/TF3), wick-based detections (Type B,
    /// Advance Extreme, MA Retest) are skipped because higher-TF candles are still forming
    /// and their wicks are not yet final — MA-position checks remain reliable.
    /// </summary>
    private BbmaTfState ClassifyStateShort(MyData data, bool allowWickDetection = true)
    {
        double? wma5High  = data.CandleData!.Wma05High;
        double? wma10High = data.CandleData!.Wma10High;
        double? bbUpper   = data.CandleData!.BollingerBandsUpperBand;

        if (wma5High == null || wma10High == null || bbUpper == null)
            return BbmaTfState.None;

        decimal high  = data.Candle.High;
        decimal close = data.Candle.Close;
        decimal open  = data.Candle.Open;

        // MagicExtreme (Magic Extreme): both MAs are above BB.Upper
        if (wma5High > bbUpper && wma10High > bbUpper)
            return BbmaTfState.MagicExtreme;

        // Extreme (Extreme Type A): LWMA5(high) is above BB.Upper
        if (wma5High > bbUpper)
            return BbmaTfState.Extreme;

        if (allowWickDetection)
        {
            decimal bbUpperDec = (decimal)bbUpper;

            // Extreme (Extreme Type B): wick rejection of BB.Upper (high above, close + open below)
            if (high > bbUpperDec && close < bbUpperDec && open < bbUpperDec)
                return BbmaTfState.Extreme;

            // Extreme (Extreme Advance): wick rejection of EMA50 (High above EMA50, Close + Open below EMA50)
            double? ema50adv = data.CandleData!.Ema50;
            if (ema50adv != null)
            {
                decimal ema50AdvDec = (decimal)ema50adv;
                if (high > ema50AdvDec && close < ema50AdvDec && open < ema50AdvDec)
                    return BbmaTfState.Extreme;
            }
        }

        // R (Reentry): bearish CSD has occurred + price reached the 510 sell zone
        // Two variants are accepted:
        //   Standard  : close at or above WMA5High (price is in or beyond the zone)
        //   MA Retest : wick spiked above WMA5High AND close recovered below WMA10High
        //               (per BBMA community: "best entry — high above MA5, close below MA10")
        //               MA Retest only checked when wick data is reliable (TF1).
        // Per PDF: R is valid when price reaches the 510 zone OR goes beyond it (e.g. touches EMA50)
        if (wma5High < wma10High)
        {
            decimal wma5Dec  = (decimal)wma5High;
            decimal wma10Dec = (decimal)wma10High;
            bool priceInZone = close >= wma5Dec;
            bool maRetest    = allowWickDetection && high > wma5Dec && close < wma10Dec;
            if (priceInZone || maRetest)
                return BbmaTfState.R;
        }

        // M (MHV phase): LWMA5(high) below BB.Upper but still above LWMA10(high)
        if (wma5High <= bbUpper && wma5High > wma10High)
            return BbmaTfState.M;

        return BbmaTfState.None;
    }


    /// <summary>
    /// Returns true if the given candle qualifies as an Extreme for a Short setup.
    ///   Type A   : LWMA5(high) is above BB.Upper
    ///   Type B   : wick rejection of BB.Upper (High above BB, Close + Open below BB)
    ///   Advance  : wick rejection of EMA50 (High above EMA50, Close + Open below EMA50)
    /// </summary>
    private static bool IsExtremeCandleShort(MyData data)
    {
        double? wma5High = data.CandleData!.Wma05High;
        double? bbUpper  = data.CandleData!.BollingerBandsUpperBand;

        if (wma5High == null || bbUpper == null)
            return false;

        // Type A: LWMA5(high) is above BB.Upper
        if (wma5High > bbUpper)
            return true;

        decimal high  = data.Candle.High;
        decimal close = data.Candle.Close;
        decimal open  = data.Candle.Open;

        // Type B: wick rejection of BB.Upper
        decimal bbUpperDec = (decimal)bbUpper;
        if (high > bbUpperDec && close < bbUpperDec && open < bbUpperDec)
            return true;

        // Advance: wick rejection of EMA50 (optional — only when EMA50 is available)
        double? ema50 = data.CandleData!.Ema50;
        if (ema50 != null)
        {
            decimal ema50Dec = (decimal)ema50;
            if (high > ema50Dec && close < ema50Dec && open < ema50Dec)
                return true;
        }

        return false;
    }


    /// <summary>
    /// Scans back from CandleLast to verify a prior Extreme candle exists within maxLookback candles.
    /// Used to confirm that the current MHV (M) phase follows a real Extreme and is not just
    /// a situation where LWMA5 happens to be above LWMA10 without an Extreme preceding it.
    /// </summary>
    private bool HadRecentExtremeShort(int maxLookback)
    {
        MyData? candle = CandleLast;
        for (int i = 0; i < maxLookback; i++)
        {
            if (!GetPrevCandle(candle, out MyData? prev))
                return false;

            if (IsExtremeCandleShort(prev!))
                return true;

            candle = prev;
        }
        return false;
    }


    /// <summary>
    /// Returns an interval-appropriate lookback depth for HadRecentExtremeShort.
    /// On higher timeframes each candle covers more time, so a smaller lookback
    /// is sufficient to cover a meaningful historical window without picking up
    /// extremes that are too far in the past to still be relevant.
    /// </summary>
    private int GetExtremeLookback() => Interval.IntervalPeriod switch
    {
        CryptoIntervalPeriod.interval1m  => 30,
        CryptoIntervalPeriod.interval2m  => 30,
        CryptoIntervalPeriod.interval3m  => 30,
        CryptoIntervalPeriod.interval5m  => 30,
        CryptoIntervalPeriod.interval10m => 25,
        CryptoIntervalPeriod.interval15m => 20,
        CryptoIntervalPeriod.interval30m => 20,
        CryptoIntervalPeriod.interval1h  => 15,
        CryptoIntervalPeriod.interval2h  => 12,
        CryptoIntervalPeriod.interval3h  => 12,
        CryptoIntervalPeriod.interval4h  => 10,
        CryptoIntervalPeriod.interval6h  => 10,
        CryptoIntervalPeriod.interval8h  => 10,
        CryptoIntervalPeriod.interval12h => 8,
        _                                => 15
    };


    public override bool IsSignal()
    {
        ExtraText = "";

        // De breedte van de bb is ten minste 1.5%
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, GlobalData.Settings.Signal.Stobb.BBMaxPercentage))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // Step 1: Classify TF1 (signal TF) state
        // Per the BBMA MTF table, the signal TF must be in M, Extreme, or MagicExtreme state
        // TF1 is NEVER "R" in any valid BBMA code — the signal fires BEFORE the CSD on TF1
        BbmaTfState state1 = ClassifyStateShort(CandleLast);

        if (state1 != BbmaTfState.M && state1 != BbmaTfState.Extreme && state1 != BbmaTfState.MagicExtreme)
        {
            ExtraText = $"TF1 ({Interval.Name}) not in setup state (is {TfStateCode(state1)})";
            return false;
        }

        // EMA50 trend filter: EMA50 must be above mid-BB (SMA20) to confirm a bearish trend
        // Per PDF: EMA50 position relative to mid-BB determines trade direction
        double? ema50Tf1 = CandleLast.CandleData!.Ema50;
        double? midBbTf1 = CandleLast.CandleData!.Sma20;
        if (ema50Tf1 == null || midBbTf1 == null)
        {
            ExtraText = "EMA50 or mid-BB not available for trend filter";
            return false;
        }
        if (ema50Tf1 <= midBbTf1)
        {
            ExtraText = $"EMA50 ({ema50Tf1:N6}) not above mid-BB ({midBbTf1:N6}) — bullish bias, no Short setup";
            return false;
        }

        // Step 2: For MHV (M) state on TF1, verify a recent Extreme occurred before it
        // Extreme and MagicExtreme states are themselves the Extreme — no additional lookback needed
        if (state1 == BbmaTfState.M && !HadRecentExtremeShort(GetExtremeLookback()))
        {
            ExtraText = $"TF1 ({Interval.Name}) is M (MHV) but no preceding Extreme found";
            return false;
        }

        // Step 3: Resolve fixed BBMA higher timeframe pair
        if (!GetIntervals(out CryptoIntervalPeriod period2, out CryptoIntervalPeriod period3))
            return false;

        // Step 4: Classify TF2 state
        // Wick-based detection is disabled for TF2/TF3: higher-TF candles are still forming,
        // so their wicks are not yet final. MA-position checks remain reliable.
        var result2 = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, period2);

        if (!result2.success || result2.candle == null || !IndicatorsOkay(result2.candle))
        {
            ExtraText = $"no data for TF2 ({result2.higherInterval.Interval.Name})";
            return false;
        }

        BbmaTfState state2 = ClassifyStateShort(result2.candle, allowWickDetection: false);

        // Step 5: Classify TF3 state
        // Wick-based detection disabled for the same reason as TF2.
        var result3 = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, period3);

        if (!result3.success || result3.candle == null || !IndicatorsOkay(result3.candle))
        {
            ExtraText = $"no data for TF3 ({result3.higherInterval.Interval.Name})";
            return false;
        }

        BbmaTfState state3 = ClassifyStateShort(result3.candle, allowWickDetection: false);

        // Step 6: TF3 (HTF) must be in Reentry state — key BBMA table requirement
        // All 4 valid patterns (REM, RRE, REE, RMEE) have TF3=R
        if (state3 != BbmaTfState.R)
        {
            ExtraText = $"TF3 ({result3.higherInterval.Interval.Name}) not in R state (is {TfStateCode(state3)})";
            return false;
        }

        // Step 7: Build the BBMA code — read TF3→TF2→TF1 (highest to lowest, top-to-bottom per table)
        string code = TfStateCode(state3) + TfStateCode(state2) + TfStateCode(state1);

        // Only the 4 valid BBMA MTF codes are accepted (PDF chapter 7): REM, RRE, REE, RMEE
        // Note: some community sources mention an RMR pattern (TF3=R, TF2=M, TF1=R), but this
        // contradicts the core BBMA rule that TF1 is never R before the signal fires. RMR excluded.
        if (code != "REM" && code != "RRE" && code != "REE" && code != "RMEE")
        {
            ExtraText = $"invalid code {code} [{result3.higherInterval.Interval.Name}/{result2.higherInterval.Interval.Name}/{Interval.Name}]";
            return false;
        }

        // ExtraText shows the code and the next action needed
        string action = state1 == BbmaTfState.M ? "wait CSD" : "Extreme active";
        ExtraText = $"{code} [{result3.higherInterval.Interval.Name}/{result2.higherInterval.Interval.Name}/{Interval.Name}] {action}";
        return true;
    }
}
#endif
