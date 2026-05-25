using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Helpers;

#if DEBUG
namespace CryptoScanner.Core.Signal.Bbma;

/// <summary>
/// Long variant of the BBMA Omni strategy.
///
/// State classifiers (IsCsd / IsExtreme / IsCsm / IsMlv / IsReentry) are 1-on-1 ports of the
/// "buy" code paths from "BBMA Oma Ally OmniView.mq5". Line numbers in the comments refer
/// to that source file.
///
/// Note on WMA orientation (matches OmniView, NOT the Pine-aligned SignalBbmaLong):
///   - CSD (CSAK Buy)      uses Wma05High / Wma10High   (close must rise above recent highs)
///   - Extreme (Buy)       uses Wma05Low  at LowerBand  (MA poked below band)
///   - Reentry (Buy)       uses Wma05Low / Wma10Low     (pullback into the lows-MA zone)
/// </summary>
public class SignalBbmaOmniLong : SignalBbmaOmniBase
{
    /// <summary>
    /// Classify a candle in OmniView terms. Priority (first match wins):
    ///   Extreme → CSM → CSD → CSAK2 → Cross → CSAA → MLV → Reentry
    /// The Extreme gate on CSM and the CSAK gate on CSAK2 are enforced implicitly
    /// because a preceding match already returns before reaching the next check.
    /// </summary>
    public OmniState GetOmniState(MyData data)
    {
        if (IsExtreme(data)) return OmniState.Extreme;
        if (IsCsm(data)) return OmniState.Csm;
        if (IsCsd(data)) return OmniState.Csd;
        if (IsCsak2(data)) return OmniState.Csak2;
        if (IsCross(data)) return OmniState.Cross;
        if (IsCsaa(data)) return OmniState.Csaa;
        if (IsMlv(data)) return OmniState.Mlv;
        if (IsReentry(data)) return OmniState.Reentry;
        return OmniState.None;
    }


    /// <summary>
    /// CSAK Buy — OmniView lines 787-796.
    ///   single-bar :  open[i] &lt; mid AND close[i] &gt; mid AND close[i] &gt; mahi5 AND close[i] &gt; mahi10
    ///   two-bar    :  open[i-1] &lt; mid[i-1] AND close[i-1] &lt; mid[i-1]
    ///              AND close[i] &gt; mid AND open[i] &gt; mid AND close[i] &gt; mahi5 AND close[i] &gt; mahi10
    /// </summary>
    private bool IsCsd(MyData data)
    {
        decimal open = data.Candle.Open;
        decimal close = data.Candle.Close;
        decimal mid = (decimal)data.CandleData!.Sma20!.Value;
        decimal mahi5 = (decimal)data.CandleData!.Wma05High!.Value;
        decimal mahi10 = (decimal)data.CandleData!.Wma10High!.Value;

        // single-bar form
        if (open < mid && close > mid && close > mahi5 && close > mahi10)
            return true;

        // two-bar form
        if (!GetPrevCandle(data, out MyData? prev) || prev == null)
            return false;
        decimal openPrev = prev.Candle.Open;
        decimal closePrev = prev.Candle.Close;
        decimal midPrev = (decimal)prev.CandleData!.Sma20!.Value;
        return openPrev < midPrev && closePrev < midPrev
            && close > mid && open > mid && close > mahi5 && close > mahi10;
    }


    /// <summary>
    /// CSAK2 Buy — OmniView lines 804-808.
    ///   Both open and close above mid, close beyond mahi5 AND mahi10, close still below UpperBand.
    ///   Gated by "no CSAK on this bar" — enforced implicitly because IsCsd() is evaluated first in
    ///   GetOmniState() and returns early when true.
    /// </summary>
    private bool IsCsak2(MyData data)
    {
        decimal open = data.Candle.Open;
        decimal close = data.Candle.Close;
        decimal mid = (decimal)data.CandleData!.Sma20!.Value;
        decimal mahi5 = (decimal)data.CandleData!.Wma05High!.Value;
        decimal mahi10 = (decimal)data.CandleData!.Wma10High!.Value;
        decimal upperB = (decimal)data.CandleData!.BollingerBandsUpperBand!.Value;

        // Both open and close above mid, close beyond WMA(high) zone, but not yet at upper band
        return open > mid && close > mid && close > mahi5 && close > mahi10 && close < upperB;
    }


    /// <summary>
    /// CSAA Buy — OmniView lines 757-761.
    ///   mahi10 &lt; mid AND mahi5 &lt; mid  (WMA-high zone is below mid — bearish context)
    ///   AND bullish candle (open &lt; close)
    ///   AND close &gt; mahi10 AND close &gt; mahi5  (closed above the WMA zone)
    ///   AND close &lt; mid                          (but still below mid)
    /// This fires when price bounces above the WMA(high) zone while still in a bearish
    /// context (WMA below mid), signalling a potential reversal or accumulation.
    /// </summary>
    private bool IsCsaa(MyData data)
    {
        decimal open = data.Candle.Open;
        decimal close = data.Candle.Close;
        decimal mid = (decimal)data.CandleData!.Sma20!.Value;
        decimal mahi5 = (decimal)data.CandleData!.Wma05High!.Value;
        decimal mahi10 = (decimal)data.CandleData!.Wma10High!.Value;

        return mahi10 < mid && mahi5 < mid
            && open < close
            && close > mahi10 && close > mahi5
            && close < mid;
    }


    /// <summary>
    /// CrossEMA50mBB Buy — OmniView lines 769-772 ("MasterSig").
    ///   BBmCross up   : close[i-1] &lt; mid[i-1] AND close[i] &gt; mid[i] AND close[i] &gt; EMA50[i]
    ///   ema50Cross up : close[i-1] &lt; EMA50[i-1] AND close[i] &gt; EMA50[i] AND close[i] &gt; mid[i]
    /// A breakout above BB-mid that is also above EMA50, or a breakout above EMA50 that is
    /// also above BB-mid — both conditions require dual confirmation.
    /// </summary>
    private bool IsCross(MyData data)
    {
        if (data.CandleData!.Ema50 == null)
            return false;

        decimal close = data.Candle.Close;
        decimal mid = (decimal)data.CandleData!.Sma20!.Value;
        decimal ema50 = (decimal)data.CandleData!.Ema50.Value;

        if (!GetPrevCandle(data, out MyData? prev) || prev == null)
            return false;
        if (prev.CandleData!.Ema50 == null)
            return false;

        decimal closePrev = prev.Candle.Close;
        decimal midPrev = (decimal)prev.CandleData!.Sma20!.Value;
        decimal ema50Prev = (decimal)prev.CandleData!.Ema50.Value;

        // Crossed above BB-mid AND confirmed above EMA50
        bool bbmCrossBuy = closePrev < midPrev && close > mid && close > ema50;
        // Crossed above EMA50 AND confirmed above BB-mid
        bool ema50CrossBuy = closePrev < ema50Prev && close > ema50 && close > mid;

        return bbmCrossBuy || ema50CrossBuy;
    }


    /// <summary>
    /// Extreme Buy — OmniView lines 817-821.
    ///   (malo5 ≤ LB recent[0..2])
    /// AND (current OR prev candle is bullish)
    /// AND (wick rejection of LB current, or prev-wick + current-close-above-LB, or gap-up open-above-LB after prev-close-below-LB)
    /// The OmniView dedup (no ext_buy at i-1 / i-2) is omitted: it is a chart-stacking guard,
    /// not a signal-logic gate.
    /// </summary>
    private bool IsExtreme(MyData data)
    {
        decimal open = data.Candle.Open;
        decimal close = data.Candle.Close;
        decimal low = data.Candle.Low;
        decimal lowerB = (decimal)data.CandleData!.BollingerBandsLowerBand!.Value;
        decimal malo5 = (decimal)data.CandleData!.Wma05Low!.Value;

        // prev / prev-prev — required for the 2-bar lookbacks in the OmniView formula
        if (!GetPrevCandle(data, out MyData? prev) || prev == null) return false;
        if (!GetPrevCandle(prev, out MyData? prev2) || prev2 == null) return false;

        decimal closePrev = prev.Candle.Close;
        decimal openPrev = prev.Candle.Open;
        decimal lowPrev = prev.Candle.Low;
        decimal lowerBPrev = (decimal)prev.CandleData!.BollingerBandsLowerBand!.Value;
        decimal malo5Prev = (decimal)prev.CandleData!.Wma05Low!.Value;
        decimal malo5Prev2 = (decimal)prev2.CandleData!.Wma05Low!.Value;

        bool maPoked = malo5 <= lowerB || malo5Prev <= lowerBPrev || malo5Prev2 <= (decimal)prev2.CandleData!.BollingerBandsLowerBand!.Value;
        if (!maPoked) return false;

        bool bullishCandle = close > open || closePrev > openPrev;
        if (!bullishCandle) return false;

        bool wickRejection =
              (low <= lowerB && close > lowerB)
           || (lowPrev <= lowerBPrev && close > lowerB)
           || (open >= lowerB && closePrev <= lowerBPrev);

        return wickRejection;
    }


    /// <summary>
    /// Momentum Buy / CSM — OmniView lines 904-908.
    ///   close[i] ≥ UpperBand[i] AND no Extreme-Sell at this candle.
    /// For the long-side state classifier we only check close vs upper band — the Extreme-Sell
    /// gate is a long-vs-short priority for arrow plotting, not relevant here.
    /// </summary>
    private bool IsCsm(MyData data)
    {
        decimal close = data.Candle.Close;
        decimal upperB = (decimal)data.CandleData!.BollingerBandsUpperBand!.Value;
        return close >= upperB;
    }


    /// <summary>
    /// MLV / MHV — stateless approximation. The OmniView MHV requires the tpwbuy state machine
    /// plus a fractal pivot, which cannot be reproduced without per-candle persistent state.
    /// We approximate the third phase of the BBMA cycle as: a wick rejection of the lower band
    /// while WmaLow5 is still inside the band (i.e. NOT the "first" Extreme).
    /// </summary>
    private bool IsMlv(MyData data)
    {
        decimal close = data.Candle.Close;
        decimal low = data.Candle.Low;
        decimal lowerB = (decimal)data.CandleData!.BollingerBandsLowerBand!.Value;
        decimal malo5 = (decimal)data.CandleData!.Wma05Low!.Value;

        // Wick rejection of lower band AND WMA5(low) inside the band (not extreme).
        return low <= lowerB && close > lowerB && malo5 > lowerB;
    }


    /// <summary>
    /// Reentry Buy — OmniView lines 925-929.
    ///   (low ≤ malo5 OR low ≤ malo10)
    /// AND (close ≥ malo5 OR close ≥ malo10)
    /// AND close ≥ mid (MiddleBuffer)
    /// </summary>
    private bool IsReentry(MyData data)
    {
        decimal close = data.Candle.Close;
        decimal low = data.Candle.Low;
        decimal mid = (decimal)data.CandleData!.Sma20!.Value;
        decimal malo5 = (decimal)data.CandleData!.Wma05Low!.Value;
        decimal malo10 = (decimal)data.CandleData!.Wma10Low!.Value;

        bool touchedMa = low <= malo5 || low <= malo10;
        bool closedBack = close >= malo5 || close >= malo10;
        return touchedMa && closedBack && close >= mid;
    }


    /// <summary>
    /// HTF validation: looks for a recent CSD, CSM or MLV setup on the higher timeframe that
    /// precedes the current pullback. Returns the first match in priority order: CSM → CSD → MHV.
    /// </summary>
    private bool CheckHtf(CryptoInterval interval, MyData current, out string htfSetup)
    {
        htfSetup = "";

        const int CsmLookback = 20;
        const int CsdLookback = 20;
        const int MlvLookback = 10;
        const int MinGap = 3; // bars required between CSM/CSD and a later MLV (the TPW phase)

        int csmIndex = -1;
        int csdIndex = -1;
        int mlvIndex = -1;

        MyData? cursor = current;
        int max = Math.Max(CsmLookback, Math.Max(CsdLookback, MlvLookback));
        for (int i = 0; i < max; i++)
        {
            if (!GetPrevCandle(interval, cursor, out cursor) || cursor == null)
                break;

            OmniState state = GetOmniState(cursor);
            if (csmIndex < 0 && i < CsmLookback && state == OmniState.Csm) csmIndex = i;
            // CSD, CSAK2, CSAA, and Cross are all treated as "CSD-class" setup signals for HTF validation
            if (csdIndex < 0 && i < CsdLookback && (state == OmniState.Csd || state == OmniState.Csak2
                    || state == OmniState.Csaa || state == OmniState.Cross)) csdIndex = i;
            if (mlvIndex < 0 && i < MlvLookback && state == OmniState.Mlv) mlvIndex = i;
        }

        // Priority 1 — MHV/Reentry: requires an MLV preceded by a CSM with at least MinGap bars in between.
        if (mlvIndex >= 0 && csmIndex > mlvIndex && csmIndex - mlvIndex >= MinGap)
        {
            htfSetup = "MHV";
            return true;
        }

        // Priority 2 — CSM/Reentry: most recent Csm within window, no qualifying MHV.
        if (csmIndex >= 0)
        {
            htfSetup = "CSM";
            return true;
        }

        // Priority 3 — CSD-class/Reentry: most recent Csd / CSAK2 / CSAA / Cross within window.
        if (csdIndex >= 0)
        {
            htfSetup = "CSD";
            return true;
        }

        return false;
    }


    /// <summary>
    /// Invalidate the setup when a Short Extreme prints on the current candle.
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        return GetOmniState(CandleLast) == OmniState.Extreme;
    }


    public override bool IsSignal()
    {
        ExtraText = "";

        // BB width must be at least 1.5% (reusing the Stobb threshold like SignalBbmaLong does)
        if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        {
            ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
            return false;
        }

        // LTF must currently be in Reentry — this is what triggers the signal evaluation
        MyData? candleLtf = CandleLast;
        OmniState stateLtfNow = GetOmniState(candleLtf);
        if (stateLtfNow != OmniState.Reentry)
        {
            ExtraText = $"LTF not in Reentry ({stateLtfNow})";
            return false;
        }

        // Resolve the fixed BBMA 3-TF pair (inherited from SignalBbmaBase)
        if (!GetIntervals(out CryptoIntervalPeriod mtf, out CryptoIntervalPeriod htf))
            return false;

        // Walk back on LTF to find the preceding "trigger" event for the code-match.
        // We accept Extreme / MLV / CSM / CSD. Stop at the first non-None state.
        OmniState stateLtfBack = OmniState.None;
        for (int i = 0; i < 30; i++)
        {
            if (!GetPrevCandle(candleLtf, out candleLtf) || candleLtf == null)
            {
                ExtraText = $"insufficient LTF history for lookback ({i} candles checked)";
                return false;
            }

            stateLtfBack = GetOmniState(candleLtf);
            if (stateLtfBack == OmniState.Extreme || stateLtfBack == OmniState.Mlv
                || stateLtfBack == OmniState.Csm || stateLtfBack == OmniState.Csd
                || stateLtfBack == OmniState.Csak2 || stateLtfBack == OmniState.Csaa
                || stateLtfBack == OmniState.Cross)
                break;
        }

        if (stateLtfBack == OmniState.None || stateLtfBack == OmniState.Reentry)
        {
            ExtraText = $"LTF no preceding setup found (last: {stateLtfBack})";
            return false;
        }

        // --- MTF state at the current candle time ---
        var resultMtf = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, mtf);
        if (!resultMtf.success || resultMtf.candle == null || !IndicatorsOkay(resultMtf.candle))
        {
            ExtraText = $"no data for MTF ({resultMtf.higherInterval.Interval.Name})";
            return false;
        }
        OmniState stateMtf = GetOmniState(resultMtf.candle);

        // --- HTF state at the current candle time ---
        var resultHtf = IndicatorDataList.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, htf);
        if (!resultHtf.success || resultHtf.candle == null || !IndicatorsOkay(resultHtf.candle))
        {
            ExtraText = $"no data for HTF";
            return false;
        }
        OmniState stateHtf = GetOmniState(resultHtf.candle);

        // HTF trend filter: EMA50 below mid-BB AND Wma05Low below mid-BB → bullish bias
        double ema50Htf = resultHtf.candle.CandleData!.Ema50!.Value;
        double midBbHtf = resultHtf.candle.CandleData!.Sma20!.Value;
        double wma05LowHtf = resultHtf.candle.CandleData!.Wma05Low!.Value;
        if (ema50Htf >= midBbHtf || wma05LowHtf >= midBbHtf)
        {
            ExtraText = $"HTF ema50 not below mid-BB — bearish bias";
            return false;
        }

        // HTF must currently be in Reentry as well
        if (stateHtf != OmniState.Reentry)
        {
            ExtraText = $"HTF not in Reentry ({stateHtf})";
            return false;
        }

        // HTF must have a recent CSM, CSD or MHV setup preceding the reentry
        if (!CheckHtf(resultHtf.higherInterval.Interval, resultHtf.candle, out string htfSetup))
        {
            ExtraText = $"HTF no CSM/CSD/MHV setup";
            return false;
        }

        // Code match — order: HTF + MTF + LTF (highest TF first).
        // Rule: HTF must be 'R' (Reentry) and LTF lookback must carry a meaningful preceding
        // event (not '-' = CSD/CSM-unmapped, and not 'R' = another Reentry).
        // This generalises the original hardcoded "RRE/REM/REE/RME" list and automatically
        // extends to new state codes (2=CSAK2, A=CSAA, X=Cross) added later.
        string code = OmniStateCode(stateHtf) + OmniStateCode(stateMtf) + OmniStateCode(stateLtfBack);
        string ltfCode = OmniStateCode(stateLtfBack);
        if (code[0] == 'R' && ltfCode != "-" && ltfCode != "R")
        {
            ExtraText = $"{code} [{htfSetup}] {resultHtf.higherInterval.Interval.Name}/{resultMtf.higherInterval.Interval.Name}/{Interval.Name}";
            return true;
        }

        ExtraText = $"code {code} not valid ({resultHtf.higherInterval.Interval.Name}/{resultMtf.higherInterval.Interval.Name}/{Interval.Name})";
        return false;
    }
}
#endif
