using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

#if DEBUG
namespace CryptoScanner.Core.Signal.Bbma;

/// <summary>
/// Long variant of the BBMA Omni strategy.
///
/// State classifiers are 1-on-1 ports of the "buy" code paths from
/// "BBMA Oma Ally OmniView.mq5". Line numbers in comments refer to that source.
///
/// Note on WMA orientation (matches OmniView, NOT the Pine-aligned SignalBbmaLong):
///   - CSD (CSAK Buy)      uses Wma05High / Wma10High   (close must rise above recent highs)
///   - Extreme (Buy)       uses Wma05Low  at LowerBand  (MA poked below band)
///   - Reentry (Buy)       uses Wma05Low / Wma10Low     (pullback into the lows-MA zone)
///
/// New signals added vs. the original stateless MLV approximation:
///   - TPW     : first WMA(low) zone touch after an Extreme Buy (backward-scan approximation).
///   - MHV     : fractal Down pivot at cursor confirmed by next bar; requires IsMhvBuy(cursor, next).
///   - RejectedEMA50 : EMA50 wick rejection with ATR big-body + uptrend context filter.
///   - GAPBBtoEMA50  : EMA50 below LowerBand in last 4 bars, bullish return inside the band.
/// </summary>
public class SignalBbmaOmniLong : SignalBbmaOmniBase
{
    /// <summary>
    /// Classify a candle in OmniView terms. Priority (first match wins):
    ///   Extreme → CSM → CSD → CSAK2 → GapBbEma50 → Cross → CSAA → TPW → RejectedEMA50 → Reentry
    /// MHV is NOT checked here — it requires knowledge of the next bar, so it is called
    /// explicitly as IsMhvBuy(cursor, next) from CheckHtf and the IsSignal LTF walkback.
    /// </summary>
    public OmniState GetOmniState(MyData data)
    {
        if (IsExtreme(data)) return OmniState.Extreme;
        if (IsCsm(data)) return OmniState.Csm;
        if (IsCsd(data)) return OmniState.Csd;
        if (IsCsak2(data)) return OmniState.Csak2;
        if (IsGapBbEma50(data)) return OmniState.GapBbEma50;
        if (IsCross(data)) return OmniState.Cross;
        if (IsCsaa(data)) return OmniState.Csaa;
        if (IsTpw(data)) return OmniState.Tpw;
        if (IsRejectedEma50(data)) return OmniState.RejectedEma50;
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
    /// </summary>
    private bool IsExtreme(MyData data)
    {
        decimal open = data.Candle.Open;
        decimal close = data.Candle.Close;
        decimal low = data.Candle.Low;
        decimal lowerB = (decimal)data.CandleData!.BollingerBandsLowerBand!.Value;
        decimal malo5 = (decimal)data.CandleData!.Wma05Low!.Value;

        if (!GetPrevCandle(data, out MyData? prev) || prev == null)
            return false;
        if (!GetPrevCandle(prev, out MyData? prev2) || prev2 == null)
            return false;

        decimal closePrev = prev.Candle.Close;
        decimal openPrev = prev.Candle.Open;
        decimal lowPrev = prev.Candle.Low;
        decimal lowerBPrev = (decimal)prev.CandleData!.BollingerBandsLowerBand!.Value;
        decimal malo5Prev = (decimal)prev.CandleData!.Wma05Low!.Value;

        bool maPoked = malo5 <= lowerB || malo5Prev <= lowerBPrev
            || (decimal)prev2.CandleData!.Wma05Low!.Value <= (decimal)prev2.CandleData!.BollingerBandsLowerBand!.Value;
        if (!maPoked)
            return false;

        bool bullishCandle = close > open || closePrev > openPrev;
        if (!bullishCandle)
            return false;

        bool wickRejection =
              (low <= lowerB && close > lowerB)
           || (lowPrev <= lowerBPrev && close > lowerB)
           || (open >= lowerB && closePrev <= lowerBPrev);

        return wickRejection;
    }


    /// <summary>
    /// Momentum Buy / CSM — OmniView lines 904-908.
    ///   close[i] ≥ UpperBand[i] AND no Extreme-Sell at this candle.
    /// </summary>
    private bool IsCsm(MyData data)
    {
        decimal close = data.Candle.Close;
        decimal upperB = (decimal)data.CandleData!.BollingerBandsUpperBand!.Value;
        return close >= upperB;
    }


    /// <summary>
    /// TPW Buy — returns true for the bar where tpwbuy transitioned 1→2 in the forward-pass
    /// state machine (MQ5 lines 842-847).
    ///
    /// Uses the pre-built TpwStateCache when available (matches MQ5 exactly).
    /// Falls back to the backward-scan approximation for HTF candles (CheckHtf) that are not
    /// in the LTF cache.
    /// </summary>
    private bool IsTpw(MyData data)
    {
        if (TpwStateCache.TryGetValue(data.Candle.OpenTime, out OmniBarState state))
            return state.IsTpwBuyFired;
        // Cache not built or different interval (HTF) — fall back to backward scan
        return IsTpwBackwardScan(data);
    }


    /// <summary>
    /// Backward-scan fallback for IsTpw — used when TpwStateCache is not available
    /// (e.g. HTF candles in CheckHtf). Less accurate than the forward-pass cache.
    /// </summary>
    private bool IsTpwBackwardScan(MyData data)
    {
        // Current bar must itself be a WMA(high) touch (the "tpwbuy fires" condition)
        decimal high = data.Candle.High;
        decimal mahi5 = (decimal)data.CandleData!.Wma05High!.Value;
        decimal mahi10 = (decimal)data.CandleData!.Wma10High!.Value;
        if (high < mahi5 && high < mahi10)
            return false;

        // Walk back up to 8 bars to find: Extreme Buy before any prior WMA touch
        const int MaxLookback = 8;
        MyData? cursor = data;
        for (int i = 0; i < MaxLookback; i++)
        {
            if (!GetPrevCandle(cursor, out cursor) || cursor == null)
                return false;

            // Reset: if any bar between Extreme and here had high > UpperBand, tpwbuy was reset
            decimal upperBCursor = (decimal)cursor.CandleData!.BollingerBandsUpperBand!.Value;
            if (cursor.Candle.High > upperBCursor)
                return false;

            OmniState state = GetOmniState(cursor);

            // Found Extreme Buy first — current bar IS the first WMA touch → TPW
            if (state == OmniState.Extreme)
                return true;

            // Found another WMA touch before any Extreme → not a fresh TPW
            decimal hi = cursor.Candle.High;
            decimal m5 = (decimal)cursor.CandleData!.Wma05High!.Value;
            decimal m10 = (decimal)cursor.CandleData!.Wma10High!.Value;
            if (hi >= m5 || hi >= m10)
                return false;
        }
        return false;
    }


    /// <summary>
    /// Returns true when a TPW Buy phase is active at <paramref name="from"/>.
    /// Used by IsMhvBuy to gate the MHV fractal check.
    ///
    /// Uses the forward-pass cache (TpwBuyCount &ge; 2) when available.
    /// Falls back to the backward-scan approximation for HTF candles.
    /// </summary>
    private bool IsTpwBuyPhaseActive(MyData from)
    {
        if (TpwStateCache.TryGetValue(from.Candle.OpenTime, out OmniBarState state))
            return state.TpwBuyCount >= 2;
        return IsTpwBuyPhaseActiveBackwardScan(from);
    }


    /// <summary>
    /// Backward-scan fallback for IsTpwBuyPhaseActive.
    /// </summary>
    private bool IsTpwBuyPhaseActiveBackwardScan(MyData from)
    {
        const int MaxBars = 22;

        // Collect history going backward
        var history = new List<MyData>(MaxBars);
        MyData? cursor = from;
        for (int i = 0; i < MaxBars; i++)
        {
            if (!GetPrevCandle(cursor, out cursor) || cursor == null)
                break;
            history.Add(cursor);
        }

        // Find most recent WMA(high) touch (tpwbuy=2 candidate) — index 0 = most recent
        int tpwIdx = -1;
        for (int i = 0; i < history.Count; i++)
        {
            MyData bar = history[i];
            decimal hi = bar.Candle.High;
            decimal m5 = (decimal)bar.CandleData!.Wma05High!.Value;
            decimal m10 = (decimal)bar.CandleData!.Wma10High!.Value;
            if (hi >= m5 || hi >= m10)
            {
                tpwIdx = i;
                break;
            }
        }
        if (tpwIdx < 0)
            return false;

        // Check no reset (high > UpperBand) between index 0 and tpwIdx-1 (bars after the TPW touch, before 'from')
        for (int i = 0; i < tpwIdx; i++)
        {
            MyData bar = history[i];
            if (bar.Candle.High > (decimal)bar.CandleData!.BollingerBandsUpperBand!.Value)
                return false;
        }

        // Check Extreme Buy exists somewhere after tpwIdx (older bars)
        for (int i = tpwIdx + 1; i < history.Count; i++)
        {
            if (GetOmniState(history[i]) == OmniState.Extreme)
                return true;
        }
        return false;
    }


    /// <summary>
    /// Builds the buy-side TPW state cache, oldest candle → newest (forward pass).
    /// Matches MQ5 tpwbuy counter exactly (OmniView.mq5 lines 823-899):
    ///
    ///   Per bar i:
    ///   1. ext_sell[i-1] fires  → tpwbuy = 0  (cross-reset, only when isExtremeSell provided)
    ///   2. ext_buy[i-1]  fires  → tpwbuy = 1  (armed)
    ///   3. tpwbuy==1 AND high[i] &ge; mahi5[i] OR mahi10[i]  → tpwbuy = 2  (TPW fires at i)
    ///   4. tpwbuy&ge;2  AND high[i-1] &gt; UpperBand[i-1]   → tpwbuy = 0  (reset)
    ///
    /// After this call TpwStateCache[openTime].TpwBuyCount / IsTpwBuyFired are available.
    /// Call before any GetOmniState invocations.
    /// </summary>
    /// <param name="indicatorData">Indicator data for this symbol/interval.</param>
    /// <param name="isExtremeSell">
    ///     Optional delegate to the Short classifier's IsExtremeSellBar method.
    ///     When provided, a Sell Extreme on bar i-1 cross-resets tpwbuy to 0 (matches MQ5).
    ///     Pass null to skip cross-reset (IsSignal use-case — no Short classifier available).
    /// </param>
    public void BuildTpwCache(CryptoSymbolInterval indicatorData, Func<MyData, bool>? isExtremeSell = null)
    {
        TpwStateCache.Clear();

        int tpwBuy = 0;
        bool prevExtremeBuy = false;
        bool prevExtremeSell = false;
        decimal prevHigh = 0m;
        decimal prevUpperBand = decimal.MaxValue; // neutral: first bar never triggers reset

        // Forward pass — oldest first (ascending open time)
        foreach (var time in indicatorData.Data.Keys.OrderBy(k => k))
        {
            if (!indicatorData.CandleList.TryGetValue(time, out CryptoCandle candle)
                || !indicatorData.Data.TryGetValue(time, out CryptoData? cd)
                || cd == null)
            {
                // Data gap — reset counter and prev-state tracking
                tpwBuy = 0;
                prevExtremeBuy = false;
                prevExtremeSell = false;
                prevHigh = 0m;
                prevUpperBand = decimal.MaxValue;
                continue;
            }

            // Skip bars before WMA and BB indicators have warmed up
            if (cd.Wma05High == null || cd.Wma10High == null || cd.BollingerBandsUpperBand == null)
            {
                prevExtremeBuy = false;
                prevExtremeSell = false;
                prevHigh = 0m;
                prevUpperBand = decimal.MaxValue;
                continue;
            }

            MyData bar = new() { Candle = candle, CandleData = cd };

            // MQ5 line 823: ext_sell[i-1] → tpwsell=1, tpwbuy=0  (cross-reset)
            if (prevExtremeSell)
                tpwBuy = 0;

            // MQ5 line 829: ext_buy[i-1] → tpwbuy=1, tpwsell=0
            if (prevExtremeBuy)
                tpwBuy = 1;

            // MQ5 line 842: tpwbuy==1 AND high[i] >= mahi5[i] OR mahi10[i] → tpwbuy=2 (TPW fires)
            bool isTpwFiredHere = false;
            if (tpwBuy == 1)
            {
                decimal high = candle.High;
                decimal mahi5 = (decimal)cd.Wma05High.Value;
                decimal mahi10 = (decimal)cd.Wma10High.Value;
                if (high >= mahi5 || high >= mahi10)
                {
                    tpwBuy = 2;
                    isTpwFiredHere = true;
                }
            }

            // MQ5 line 898: tpwbuy>=2 AND high[i-1] > UpperBand[i-1] → tpwbuy=0 (reset)
            if (tpwBuy >= 2 && prevHigh > prevUpperBand)
            {
                tpwBuy = 0;
                isTpwFiredHere = false; // reset cancels the fired flag on the same bar
            }

            TpwStateCache[time] = new OmniBarState
            {
                TpwBuyCount = tpwBuy,
                IsTpwBuyFired = isTpwFiredHere,
            };

            // Store this bar's extremes for the next iteration's [i-1] checks
            prevExtremeBuy = IsExtreme(bar);
            prevExtremeSell = isExtremeSell?.Invoke(bar) ?? false;
            prevHigh = candle.High;
            prevUpperBand = cd.BollingerBandsUpperBand.HasValue
                ? (decimal)cd.BollingerBandsUpperBand.Value
                : decimal.MaxValue;
        }
    }


    /// <summary>
    /// Exposes IsExtreme so Bbma.cs (CryptoScanner project) can pass it as a cross-reset
    /// delegate to SignalBbmaOmniShort.BuildTpwCache.
    /// </summary>
    public bool IsExtremeBuyBar(MyData data) => IsExtreme(data);


    /// <summary>
    /// MHV Buy — OmniView lines 857-875 (placed at cursor = bar[i-1] when next = bar[i] confirms fractal).
    ///
    /// Conditions (all must hold):
    ///   1. IsTpwBuyPhaseActive(cursor) — tpwbuy ≥ 2 in MQ5 terms.
    ///   2. Fractal Down at cursor: prev.Low &gt; cursor.Low AND next.Low &gt; cursor.Low  (barsLeft=1, barsRight=1, strict right).
    ///   3. cursor.Low &lt; cursor.Mid (low of the fractal bar is below BB-mid).
    ///   4. No CSM Sell at cursor or next (mmt_sell guard in MQ5).
    ///      We approximate: GetOmniState(cursor) must not be Csm, and GetOmniState(next) must not be Csm.
    ///      (Csm for long-side = close ≥ UpperBand which would be a sell-side CSM — already excluded.)
    ///
    /// Note: this method is NOT called from GetOmniState because it requires the next bar.
    /// It is called explicitly from CheckHtf and from the IsSignal LTF lookback.
    /// </summary>
    public bool IsMhvBuy(MyData cursor, MyData next)
    {
        // Gate: TPW Buy phase must be active at cursor
        if (!IsTpwBuyPhaseActive(cursor))
            return false;

        // cursor.Low < mid
        decimal midCursor = (decimal)cursor.CandleData!.Sma20!.Value;
        if (cursor.Candle.Low >= midCursor)
            return false;

        // No CSM (mmt_sell) at cursor or next
        // For the long classifier: Csm means close >= UpperBand (buy-side momentum).
        // The MQ5 guard is "no mmt_sell at i and i-1". In C# terms:
        // mmt_sell fires when close <= LowerBand → that's IsCsm of the short classifier.
        // We only have the long classifier here, so we approximate: no Csm at cursor or next
        // (Csm long = close>=UpperBand which would interfere with a downside pivot anyway).
        if (IsCsm(cursor) || IsCsm(next))
            return false;

        // Fractal Down: need the bar before cursor (prev)
        if (!GetPrevCandle(cursor, out MyData? prev) || prev == null)
            return false;

        decimal lowCursor = cursor.Candle.Low;
        decimal lowPrev = prev.Candle.Low;
        decimal lowNext = next.Candle.Low;

        // barsLeft=1: prev.Low must be > cursor.Low (strictly)
        // barsRight=1: next.Low must be > cursor.Low (strictly, per OmniView "dynamic fractal")
        bool fractalDown = lowPrev > lowCursor && lowNext > lowCursor;
        return fractalDown;
    }


    /// <summary>
    /// RejectedEMA50 Buy — OmniView lines 944-945.
    ///   low[i] &lt; EMA50[i] AND close[i] &gt; EMA50[i]
    ///   AND sinceUpTrend &lt; 4 (uptrend was active within the last 4 bars)
    ///   AND sinceBigBody &lt; 6 (a big-body candle in the last 6 bars)
    ///
    /// sinceUpTrend: BarsSinceTrend(isDown=false, limit=4)
    ///   → walks back until low[j] > ema50[j] AND low[j-1] > ema50[j-1] (two-bar uptrend).
    /// sinceBigBody: BarsSinceBigBody(limit=6)
    ///   → walks back until |close-open| > 0.5 * ATR14.
    ///
    /// Returns false when ATR14 is unavailable (null-safe).
    /// </summary>
    private bool IsRejectedEma50(MyData data)
    {
        if (data.CandleData!.Ema50 == null)
            return false;

        decimal low = data.Candle.Low;
        decimal close = data.Candle.Close;
        decimal ema50 = (decimal)data.CandleData!.Ema50.Value;

        if (low >= ema50 || close <= ema50)
            return false;

        int sinceUpTrend = BarsSinceTrend(data, isDown: false, limit: 4);
        if (sinceUpTrend >= 4)
            return false;

        int sinceBigBody = BarsSinceBigBody(data, limit: 6);
        if (sinceBigBody >= 6)
            return false;

        return true;
    }


    /// <summary>
    /// GAPBBtoEMA50 Buy — OmniView lines 967-973.
    ///   (EMA50[i-1..i-3] or EMA50[i]) &lt; LowerBand on any of those bars  (EMA50 outside band in last 4 bars)
    ///   AND close[i] &gt; open[i] AND close[i] &gt; LowerBand[i]             (bullish candle closed inside)
    ///   AND (low[i] ≤ LB[i] OR low[i-1] ≤ LB[i-1] OR low[i-2] ≤ LB[i-2]) (price touched the band in last 3 bars)
    /// </summary>
    private bool IsGapBbEma50(MyData data)
    {
        if (data.CandleData!.Ema50 == null)
            return false;

        decimal close = data.Candle.Close;
        decimal open = data.Candle.Open;
        decimal low = data.Candle.Low;
        decimal lowerB = (decimal)data.CandleData!.BollingerBandsLowerBand!.Value;

        // Bullish candle closed inside the band
        if (close <= open || close <= lowerB)
            return false;

        // Need prev and prev2 for the 4-bar EMA50-below-LB check and the 3-bar low-touch check
        if (!GetPrevCandle(data, out MyData? prev) || prev == null)
            return false;
        if (prev.CandleData!.Ema50 == null)
            return false;
        if (!GetPrevCandle(prev, out MyData? prev2) || prev2 == null)
            return false;
        if (prev2.CandleData!.Ema50 == null)
            return false;
        if (!GetPrevCandle(prev2, out MyData? prev3) || prev3 == null)
            return false;
        if (prev3.CandleData!.Ema50 == null)
            return false;

        decimal lowerBPrev = (decimal)prev.CandleData!.BollingerBandsLowerBand!.Value;
        decimal lowerBPrev2 = (decimal)prev2.CandleData!.BollingerBandsLowerBand!.Value;
        decimal lowerBPrev3 = (decimal)prev3.CandleData!.BollingerBandsLowerBand!.Value;

        // EMA50 below LowerBand on any of i, i-1, i-2, i-3
        bool ema50BelowLb =
              (decimal)data.CandleData!.Ema50.Value < lowerB
           || (decimal)prev.CandleData!.Ema50.Value < lowerBPrev
           || (decimal)prev2.CandleData!.Ema50.Value < lowerBPrev2
           || (decimal)prev3.CandleData!.Ema50.Value < lowerBPrev3;
        if (!ema50BelowLb)
            return false;

        // Price touched the lower band in last 3 bars (i, i-1, i-2)
        bool lowTouched =
              low <= lowerB
           || prev.Candle.Low <= lowerBPrev
           || prev2.Candle.Low <= lowerBPrev2;
        return lowTouched;
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
    /// HTF validation: looks for a recent CSM, CSD-class, TPW or MHV setup on the higher
    /// timeframe that precedes the current candle. Returns the first match in priority order.
    ///
    /// MHV requires knowledge of the next bar — we track nextCursor inside the walk loop.
    /// </summary>
    private bool CheckHtf(CryptoInterval interval, MyData current, out string htfSetup)
    {
        htfSetup = "";

        const int CsmLookback = 20;
        const int CsdLookback = 20;
        const int TpwLookback = 10;
        const int MhvLookback = 10;
        const int MinGap = 3; // bars required between CSM/CSD and a later TPW/MHV (the TPW phase)

        int csmIndex = -1;
        int csdIndex = -1;
        int tpwIndex = -1;
        int mhvIndex = -1;

        MyData? cursor = current;
        MyData? nextCursor = null; // one bar newer than cursor (needed for MHV fractal check)
        int max = Math.Max(CsmLookback, Math.Max(CsdLookback, Math.Max(TpwLookback, MhvLookback)));
        for (int i = 0; i < max; i++)
        {
            nextCursor = cursor;
            if (!GetPrevCandle(interval, cursor, out cursor) || cursor == null)
                break;

            OmniState state = GetOmniState(cursor);
            if (csmIndex < 0 && i < CsmLookback && state == OmniState.Csm) csmIndex = i;
            // CSD, CSAK2, CSAA, and Cross are all treated as "CSD-class" setup signals for HTF validation
            if (csdIndex < 0 && i < CsdLookback && (state == OmniState.Csd || state == OmniState.Csak2
                    || state == OmniState.Csaa || state == OmniState.Cross)) csdIndex = i;
            if (tpwIndex < 0 && i < TpwLookback && state == OmniState.Tpw) tpwIndex = i;

            // MHV: requires the next bar — check only when nextCursor is available
            if (mhvIndex < 0 && i < MhvLookback && nextCursor != null && IsMhvBuy(cursor, nextCursor))
                mhvIndex = i;
        }

        // Priority 1 — MHV: requires a CSM preceded by MHV with MinGap bars in between
        if (mhvIndex >= 0 && csmIndex > mhvIndex && csmIndex - mhvIndex >= MinGap)
        {
            htfSetup = "MHV";
            return true;
        }

        // Priority 2 — TPW: requires a CSM preceded by TPW with MinGap bars in between
        if (tpwIndex >= 0 && csmIndex > tpwIndex && csmIndex - tpwIndex >= MinGap)
        {
            htfSetup = "TPW";
            return true;
        }

        // Priority 3 — CSM/Reentry: most recent Csm within window
        if (csmIndex >= 0)
        {
            htfSetup = "CSM";
            return true;
        }

        // Priority 4 — CSD-class/Reentry
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
        string logPrefix = $"{Symbol.Name} {Interval.Name} bbma.omni {SignalSide} ";

        // Build the forward-pass TPW cache before any GetOmniState calls.
        // No cross-reset here (no Short classifier available in the scanner path).
        BuildTpwCache(SymbolInterval);

        //// BB width must be at least 1.5% (reusing the Stobb threshold like SignalBbmaLong does)
        //if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        //{
        //    ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
        //    return false;
        //}

        // LTF must currently be in Reentry — this is what triggers the signal evaluation
        MyData? candleLtf = CandleLast;
        OmniState stateLtfNow = GetOmniState(candleLtf);
        if (stateLtfNow != OmniState.Reentry)
        {
            ExtraText = $"LTF not in Reentry ({stateLtfNow})";
            return false;
        }

        // From here on we log every step — reaching this point is already meaningful
        //GlobalData.AddTextToLogTab($"{logPrefix} [{CandleLast.Candle.Date.ToLocalTime():yyyy-MM-dd HH:mm}] LTF=Reentry, starting evaluation");

        // Resolve the fixed BBMA 3-TF pair (inherited from SignalBbmaBase)
        if (!GetIntervals(out CryptoIntervalPeriod mtf, out CryptoIntervalPeriod htf))
        {
            GlobalData.AddTextToLogTab($"{logPrefix} GetIntervals failed");
            return false;
        }

        // Walk back on LTF to find the preceding "trigger" event for the code-match.
        // Accept Extreme / Tpw / Mhv / CSM / CSD-class. Stop at the first non-None state.
        // Track candleLtfNext so we can call IsMhvBuy(candleLtf, candleLtfNext).
        OmniState stateLtfBack = OmniState.None;
        MyData? candleLtfNext = null;
        for (int i = 0; i < 30; i++)
        {
            candleLtfNext = candleLtf;
            if (!GetPrevCandle(candleLtf, out candleLtf) || candleLtf == null)
            {
                ExtraText = $"insufficient LTF history for lookback ({i} candles checked)";
                //GlobalData.AddTextToLogTab($"{logPrefix} insufficient LTF history after {i} bars");
                return false;
            }

            // Check MHV first (requires next bar)
            if (candleLtfNext != null && IsMhvBuy(candleLtf, candleLtfNext))
            {
                stateLtfBack = OmniState.Mhv;
                break;
            }

            stateLtfBack = GetOmniState(candleLtf);
            if (stateLtfBack == OmniState.Extreme || stateLtfBack == OmniState.Tpw
                || stateLtfBack == OmniState.RejectedEma50 || stateLtfBack == OmniState.GapBbEma50
                || stateLtfBack == OmniState.Csm || stateLtfBack == OmniState.Csd
                || stateLtfBack == OmniState.Csak2 || stateLtfBack == OmniState.Csaa
                || stateLtfBack == OmniState.Cross)
                break;
        }

        if (stateLtfBack == OmniState.None || stateLtfBack == OmniState.Reentry)
        {
            ExtraText = $"LTF no preceding setup found (last: {stateLtfBack})";
            //GlobalData.AddTextToLogTab($"{logPrefix} LTF walkback: no setup found (last={stateLtfBack})");
            return false;
        }
        //GlobalData.AddTextToLogTab($"{logPrefix} LTF walkback: found {stateLtfBack}");

        // --- MTF state at the current candle time ---
        var resultMtf = IndicatorEngine.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, mtf);
        if (!resultMtf.success || resultMtf.candle == null || !IndicatorsOkay(resultMtf.candle))
        {
            ExtraText = $"no data for MTF ({resultMtf.higherInterval.Interval.Name})";
            //GlobalData.AddTextToLogTab($"{logPrefix} MTF ({resultMtf.higherInterval.Interval.Name}): no data (success={resultMtf.success})");
            return false;
        }
        OmniState stateMtf = GetOmniState(resultMtf.candle);
        //GlobalData.AddTextToLogTab($"{logPrefix} MTF ({resultMtf.higherInterval.Interval.Name})={stateMtf}");

        // --- HTF state at the current candle time ---
        var resultHtf = IndicatorEngine.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, htf);
        if (!resultHtf.success || resultHtf.candle == null || !IndicatorsOkay(resultHtf.candle))
        {
            ExtraText = $"no data for HTF";
            //GlobalData.AddTextToLogTab($"{logPrefix} HTF ({resultHtf.higherInterval.Interval.Name}): no data (success={resultHtf.success})");
            return false;
        }
        OmniState stateHtf = GetOmniState(resultHtf.candle);
        //GlobalData.AddTextToLogTab($"{logPrefix} HTF ({resultHtf.higherInterval.Interval.Name})={stateHtf}");

        // HTF trend filter: EMA50 below mid-BB AND Wma05Low below mid-BB → bullish bias
        double ema50Htf = resultHtf.candle.CandleData!.Ema50!.Value;
        double midBbHtf = resultHtf.candle.CandleData!.Sma20!.Value;
        double wma05LowHtf = resultHtf.candle.CandleData!.Wma05Low!.Value;
        if (ema50Htf >= midBbHtf || wma05LowHtf >= midBbHtf)
        {
            ExtraText = $"HTF ema50 not below mid-BB — bearish bias";
            //GlobalData.AddTextToLogTab($"{logPrefix} HTF trend filter failed: ema50={ema50Htf:F4} wma05Low={wma05LowHtf:F4} mid={midBbHtf:F4}");
            return false;
        }

        // HTF must currently be in Reentry as well
        if (stateHtf != OmniState.Reentry)
        {
            ExtraText = $"HTF not in Reentry ({stateHtf})";
            //GlobalData.AddTextToLogTab($"{logPrefix} HTF not Reentry (={stateHtf})");
            return false;
        }

        // HTF must have a recent CSM, CSD, TPW or MHV setup preceding the reentry
        if (!CheckHtf(resultHtf.higherInterval.Interval, resultHtf.candle, out string htfSetup))
        {
            ExtraText = $"HTF no CSM/CSD/TPW/MHV setup";
            //GlobalData.AddTextToLogTab($"{logPrefix} HTF CheckHtf: no setup found");
            return false;
        }
        //GlobalData.AddTextToLogTab($"{logPrefix} HTF CheckHtf: found {htfSetup}");

        // Code match — order: HTF + MTF + LTF (highest TF first).
        // Rule: HTF must be 'R' (Reentry) and LTF lookback must carry a meaningful preceding
        // event (not '-' = CSD/CSM-unmapped, and not 'R' = another Reentry).
        string code = OmniStateCode(stateHtf) + OmniStateCode(stateMtf) + OmniStateCode(stateLtfBack);
        string ltfCode = OmniStateCode(stateLtfBack);
        if (code[0] == 'R' && ltfCode != "-" && ltfCode != "R")
        {
            ExtraText = $"{code} [{htfSetup}] {resultHtf.higherInterval.Interval.Name}/{resultMtf.higherInterval.Interval.Name}/{Interval.Name}";
            //GlobalData.AddTextToLogTab($"{logPrefix} SIGNAL code={code} [{htfSetup}]");
            return true;
        }

        ExtraText = $"code {code} not valid ({resultHtf.higherInterval.Interval.Name}/{resultMtf.higherInterval.Interval.Name}/{Interval.Name})";
        //GlobalData.AddTextToLogTab($"{logPrefix} code {code} not valid (ltfCode={ltfCode})");
        return false;
    }
}
#endif
