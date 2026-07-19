using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.Bbma.Signal;

/// <summary>
/// Short variant of the BBMA Omni strategy. Mirror of <see cref="SignalBbmaOmniLong"/> —
/// state classifiers are 1-on-1 ports of the "sell" code paths from
/// "BBMA Oma Ally OmniView.mq5". Line numbers in comments refer to that source.
///
/// Note on WMA orientation (matches OmniView):
///   - CSD (CSAK Sell)     uses Wma05Low  / Wma10Low    (close must fall below recent lows)
///   - Extreme (Sell)      uses Wma05High at UpperBand  (MA poked above band)
///   - Reentry (Sell)      uses Wma05High / Wma10High   (pullback into the highs-MA zone)
/// </summary>
public class SignalBbmaOmniShort : SignalBbmaOmniBase
{
    /// <summary>
    /// Computes all sell-side signal buffers for this bar — the direct equivalent of evaluating
    /// every independent MQL5 buffer (csak_sell[i], csak2_sell[i], ext_sell[i], mmt_sell[i], ...)
    /// unconditionally. Only Csak2 keeps its source-level gate against Csd (OmniView.mq5 line
    /// 799: "csak_sell[i]==EMPTY_VALUE") — every other buffer is independent.
    /// </summary>
    public OmniBar GetOmniBar(MyData data)
    {
        bool csd = IsCsd(data);
        return new OmniBar
        {
            Extreme = IsExtreme(data),
            Csm = IsCsm(data),
            Csd = csd,
            Csak2 = !csd && IsCsak2(data),
            Csaa = IsCsaa(data),
            Cross = IsCross(data),
            Tpw = IsTpw(data),
            RejectedEma50 = IsRejectedEma50(data),
            GapBbEma50 = IsGapBbEma50(data),
            Reentry = IsReentry(data),
        };
    }

    /// <summary>
    /// Derives a single display label from <see cref="GetOmniBar"/> — see
    /// <see cref="SignalBbmaOmniBase.DeriveLabel"/> for why this must NEVER be used for gating.
    /// MHV is not included — it requires the next bar (IsMhvSell(cursor, next)), called
    /// explicitly from CheckHtf and the IsSignal LTF walkback.
    /// </summary>
    public OmniState GetOmniState(MyData data) => DeriveLabel(GetOmniBar(data));


    /// <summary>
    /// CSAK Sell — OmniView lines 776-786 (sell variant).
    ///   single-bar :  open[i] &gt; mid AND close[i] &lt; mid AND close[i] &lt; malo5 AND close[i] &lt; malo10
    ///   two-bar    :  open[i-1] &gt; mid[i-1] AND close[i-1] &gt; mid[i-1]
    ///              AND close[i] &lt; mid AND open[i] &lt; mid AND close[i] &lt; malo5 AND close[i] &lt; malo10
    /// </summary>
    private bool IsCsd(MyData data)
    {
        decimal open = data.Candle.Open;
        decimal close = data.Candle.Close;
        decimal mid = (decimal)data.CandleData!.Sma20!.Value;
        decimal malo5 = (decimal)data.CandleData!.Wma05Low!.Value;
        decimal malo10 = (decimal)data.CandleData!.Wma10Low!.Value;

        // single-bar form
        if (open > mid && close < mid && close < malo5 && close < malo10)
            return true;

        // two-bar form
        if (!GetPrevCandle(data, out MyData? prev) || prev == null)
            return false;
        decimal openPrev = prev.Candle.Open;
        decimal closePrev = prev.Candle.Close;
        decimal midPrev = (decimal)prev.CandleData!.Sma20!.Value;
        return openPrev > midPrev && closePrev > midPrev
            && close < mid && open < mid && close < malo5 && close < malo10;
    }


    /// <summary>
    /// CSAK2 Sell — OmniView lines 799-803.
    ///   Both open and close below mid, close beyond malo5 AND malo10, close still above LowerBand.
    ///   Gated by "no CSAK on this bar" — enforced implicitly because IsCsd() is evaluated first.
    /// </summary>
    private bool IsCsak2(MyData data)
    {
        decimal open = data.Candle.Open;
        decimal close = data.Candle.Close;
        decimal mid = (decimal)data.CandleData!.Sma20!.Value;
        decimal malo5 = (decimal)data.CandleData!.Wma05Low!.Value;
        decimal malo10 = (decimal)data.CandleData!.Wma10Low!.Value;
        decimal lowerB = (decimal)data.CandleData!.BollingerBandsLowerBand!.Value;

        // Both open and close below mid, close beyond WMA(low) zone, but not yet at lower band
        return open < mid && close < mid && close < malo5 && close < malo10 && close > lowerB;
    }


    /// <summary>
    /// CSAA Sell — OmniView lines 752-756.
    ///   malo10 &gt; mid AND malo5 &gt; mid  (WMA-low zone is above mid — bullish context)
    ///   AND bearish candle (open &gt; close)
    ///   AND close &lt; malo10 AND close &lt; malo5  (closed below the WMA zone)
    ///   AND close &gt; mid                         (but still above mid)
    /// </summary>
    private bool IsCsaa(MyData data)
    {
        decimal open = data.Candle.Open;
        decimal close = data.Candle.Close;
        decimal mid = (decimal)data.CandleData!.Sma20!.Value;
        decimal malo5 = (decimal)data.CandleData!.Wma05Low!.Value;
        decimal malo10 = (decimal)data.CandleData!.Wma10Low!.Value;

        return malo10 > mid && malo5 > mid
            && open > close
            && close < malo10 && close < malo5
            && close > mid;
    }


    /// <summary>
    /// CrossEMA50mBB Sell — OmniView lines 764-768 ("MasterSig").
    ///   BBmCross down   : close[i-1] &gt; mid[i-1] AND close[i] &lt; mid[i] AND close[i] &lt; EMA50[i]
    ///   ema50Cross down : close[i-1] &gt; EMA50[i-1] AND close[i] &lt; EMA50[i] AND close[i] &lt; mid[i]
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

        // Crossed below BB-mid AND confirmed below EMA50
        bool bbmCrossSell = closePrev > midPrev && close < mid && close < ema50;
        // Crossed below EMA50 AND confirmed below BB-mid
        bool ema50CrossSell = closePrev > ema50Prev && close < ema50 && close < mid;

        return bbmCrossSell || ema50CrossSell;
    }


    /// <summary>
    /// Memoization cache for the gated Extreme Sell result, keyed by candle open time.
    /// See <see cref="SignalBbmaOmniLong"/>'s analogous cache for why this is needed
    /// (the anti-repeat guard recurses into i-1/i-2 and would otherwise be exponential).
    /// </summary>
    private readonly Dictionary<CandleTime, bool> _extremeCache = [];

    /// <summary>
    /// Extreme Sell — OmniView lines 811-815, INCLUDING the anti-repeat guard
    /// (ext_sell[i-1]==EMPTY_VALUE && ext_sell[i-2]==EMPTY_VALUE) that the raw conditions
    /// in <see cref="ComputeExtreme"/> do not enforce on their own.
    /// </summary>
    private bool IsExtreme(MyData data)
    {
        if (_extremeCache.TryGetValue(data.Candle.OpenTime, out bool cached))
            return cached;

        bool result = ComputeExtreme(data);
        _extremeCache[data.Candle.OpenTime] = result;
        return result;
    }

    /// <summary>
    /// Gated Extreme Sell computation — raw condition (<see cref="IsExtremeRaw"/>) plus the
    /// MQ5 anti-repeat guard: no Extreme Sell already recorded at i-1 or i-2.
    /// </summary>
    private bool ComputeExtreme(MyData data)
    {
        if (!IsExtremeRaw(data))
            return false;

        if (!GetPrevCandle(data, out MyData? prev) || prev == null)
            return false;
        if (!GetPrevCandle(prev, out MyData? prev2) || prev2 == null)
            return false;

        // ext_sell[i-1]==EMPTY_VALUE && ext_sell[i-2]==EMPTY_VALUE (OmniView.mq5 line 811)
        if (IsExtreme(prev) || IsExtreme(prev2))
            return false;

        return true;
    }

    /// <summary>
    /// Extreme Sell raw condition — OmniView lines 811-815 minus the anti-repeat guard.
    ///   (mahi5 ≥ UB recent[0..2])
    /// AND (current OR prev candle is bearish)
    /// AND (wick rejection of UB current, or prev-wick + current-close-below-UB, or gap-down open-below-UB after prev-close-above-UB)
    /// </summary>
    private bool IsExtremeRaw(MyData data)
    {
        decimal open = data.Candle.Open;
        decimal close = data.Candle.Close;
        decimal high = data.Candle.High;
        decimal upperB = (decimal)data.CandleData!.BollingerBandsUpperBand!.Value;
        decimal mahi5 = (decimal)data.CandleData!.Wma05High!.Value;

        if (!GetPrevCandle(data, out MyData? prev) || prev == null)
            return false;
        if (!GetPrevCandle(prev, out MyData? prev2) || prev2 == null)
            return false;

        decimal closePrev = prev.Candle.Close;
        decimal openPrev = prev.Candle.Open;
        decimal highPrev = prev.Candle.High;
        decimal upperBPrev = (decimal)prev.CandleData!.BollingerBandsUpperBand!.Value;
        decimal upperBPrev2 = (decimal)prev2.CandleData!.BollingerBandsUpperBand!.Value;
        decimal mahi5Prev = (decimal)prev.CandleData!.Wma05High!.Value;
        decimal mahi5Prev2 = (decimal)prev2.CandleData!.Wma05High!.Value;

        bool maPoked = mahi5 >= upperB || mahi5Prev >= upperBPrev || mahi5Prev2 >= upperBPrev2;
        if (!maPoked)
            return false;

        bool bearishCandle = close < open || closePrev < openPrev;
        if (!bearishCandle)
            return false;

        // MQ5 line 811: second condition uses UpperBand[i] (current bar), not UpperBand[i-1]
        bool wickRejection =
              (high >= upperB && close < upperB)
           || (highPrev >= upperB && close < upperB)
           || (open <= upperB && closePrev >= upperBPrev);

        return wickRejection;
    }


    /// <summary>
    /// Momentum Sell / CSM — OmniView lines 909-913.
    ///   close[i] ≤ LowerBand[i] AND no Extreme-Buy at this candle (ext_buy[i]==EMPTY_VALUE).
    ///   The opposite-side Extreme check is wired up via <see cref="SignalBbmaOmniBase.OppositeExtremeChecker"/>
    ///   (set in IsSignal() from an ephemeral SignalBbmaOmniLong instance).
    /// </summary>
    private bool IsCsm(MyData data)
    {
        decimal close = data.Candle.Close;
        decimal lowerB = (decimal)data.CandleData!.BollingerBandsLowerBand!.Value;
        if (close > lowerB)
            return false;

        if (OppositeExtremeChecker != null && OppositeExtremeChecker(data))
            return false;

        return true;
    }

    /// <summary>
    /// Exposes IsCsm (mmt_sell) so SignalBbmaOmniLong can wire it up as the
    /// OppositeCsmChecker delegate for its own MHV Buy gate (OmniView.mq5 line 857:
    /// mmt_sell[i]==EMPTY_VALUE && mmt_sell[i-1]==EMPTY_VALUE).
    /// </summary>
    public bool IsCsmSellBar(MyData data) => IsCsm(data);


    /// <summary>
    /// TPW Sell — returns true for the bar where tpwsell transitioned 1→2 in the forward-pass
    /// state machine (MQ5 lines 836-841).
    ///
    /// Uses the pre-built TpwStateCache when available (matches MQ5 exactly).
    /// Falls back to the backward-scan approximation for HTF candles (CheckHtf).
    /// </summary>
    private bool IsTpw(MyData data)
    {
        if (TpwStateCache.TryGetValue(data.Candle.OpenTime, out OmniBarState state))
            return state.IsTpwSellFired;
        // Cache not built or different interval (HTF) — fall back to backward scan
        return IsTpwBackwardScan(data);
    }


    /// <summary>
    /// Backward-scan fallback for IsTpw — used when TpwStateCache is not available.
    /// </summary>
    private bool IsTpwBackwardScan(MyData data)
    {
        // Current bar must be a WMA(low) touch (the "tpwsell fires" condition)
        decimal low = data.Candle.Low;
        decimal malo5 = (decimal)data.CandleData!.Wma05Low!.Value;
        decimal malo10 = (decimal)data.CandleData!.Wma10Low!.Value;
        if (low > malo5 && low > malo10)
            return false;

        const int MaxLookback = 8;
        MyData? cursor = data;
        for (int i = 0; i < MaxLookback; i++)
        {
            if (!GetPrevCandle(cursor, out cursor) || cursor == null)
                return false;

            // Reset: if any bar had low < LowerBand, tpwsell was reset
            decimal lowerBCursor = (decimal)cursor.CandleData!.BollingerBandsLowerBand!.Value;
            if (cursor.Candle.Low < lowerBCursor)
                return false;

            // Found Extreme Sell first → current bar IS the first WMA(low) touch → TPW Sell
            if (IsExtreme(cursor))
                return true;

            // Found another WMA(low) touch before any Extreme → not a fresh TPW
            decimal lo = cursor.Candle.Low;
            decimal m5 = (decimal)cursor.CandleData!.Wma05Low!.Value;
            decimal m10 = (decimal)cursor.CandleData!.Wma10Low!.Value;
            if (lo <= m5 || lo <= m10)
                return false;
        }
        return false;
    }


    /// <summary>
    /// Returns true when a TPW Sell phase is active at <paramref name="from"/>.
    /// Used by IsMhvSell to gate the MHV fractal check.
    ///
    /// Uses the forward-pass cache (TpwSellCount &ge; 2) when available.
    /// Falls back to the backward-scan approximation for HTF candles.
    /// </summary>
    private bool IsTpwSellPhaseActive(MyData from)
    {
        if (TpwStateCache.TryGetValue(from.Candle.OpenTime, out OmniBarState state))
            return state.TpwSellCount >= 2;
        return IsTpwSellPhaseActiveBackwardScan(from);
    }


    /// <summary>
    /// Backward-scan fallback for IsTpwSellPhaseActive.
    /// </summary>
    private bool IsTpwSellPhaseActiveBackwardScan(MyData from)
    {
        const int MaxBars = 22;

        var history = new List<MyData>(MaxBars);
        MyData? cursor = from;
        for (int i = 0; i < MaxBars; i++)
        {
            if (!GetPrevCandle(cursor, out cursor) || cursor == null)
                break;
            history.Add(cursor);
        }

        // Find most recent WMA(low) touch (tpwsell=2 candidate) — index 0 = most recent
        int tpwIdx = -1;
        for (int i = 0; i < history.Count; i++)
        {
            MyData bar = history[i];
            decimal lo = bar.Candle.Low;
            decimal m5 = (decimal)bar.CandleData!.Wma05Low!.Value;
            decimal m10 = (decimal)bar.CandleData!.Wma10Low!.Value;
            if (lo <= m5 || lo <= m10)
            {
                tpwIdx = i;
                break;
            }
        }
        if (tpwIdx < 0)
            return false;

        // Check no reset (low < LowerBand) between index 0 and tpwIdx-1
        for (int i = 0; i < tpwIdx; i++)
        {
            MyData bar = history[i];
            if (bar.Candle.Low < (decimal)bar.CandleData!.BollingerBandsLowerBand!.Value)
                return false;
        }

        // Check Extreme Sell exists somewhere after tpwIdx (older bars)
        for (int i = tpwIdx + 1; i < history.Count; i++)
        {
            if (IsExtreme(history[i]))
                return true;
        }
        return false;
    }


    /// <summary>
    /// Builds the sell-side TPW state cache, oldest candle → newest (forward pass).
    /// Matches MQ5 tpwsell counter exactly (OmniView.mq5 lines 823-901):
    ///
    ///   Per bar i:
    ///   1. ext_buy[i-1]  fires  → tpwsell = 0  (cross-reset, only when isExtremeBuy provided)
    ///   2. ext_sell[i-1] fires  → tpwsell = 1  (armed)
    ///   3. tpwsell==1 AND low[i] &le; malo5[i] OR malo10[i]  → tpwsell = 2  (TPW fires at i)
    ///   4. (MQ5 sell reset — commented out in source — not applied here)
    ///
    /// After this call TpwStateCache[openTime].TpwSellCount / IsTpwSellFired are available.
    /// Call before any GetOmniState invocations.
    /// </summary>
    /// <param name="indicatorData">Indicator data for this symbol/interval.</param>
    /// <param name="isExtremeBuy">
    ///     Optional delegate to the Long classifier's IsExtremeBuyBar method.
    ///     When provided, a Buy Extreme on bar i-1 cross-resets tpwsell to 0 (matches MQ5).
    ///     Pass null to skip cross-reset (IsSignal use-case — no Long classifier available).
    /// </param>
    public void BuildTpwCache(CryptoSymbolInterval indicatorData, Func<MyData, bool>? isExtremeBuy = null)
    {
        TpwStateCache.Clear();

        int tpwSell = 0;
        bool prevExtremeSell = false;
        bool prevExtremeBuy = false;
        // (The MQ5 sell reset condition on low[i-1] < LowerBand[i-1] is commented out,
        //  so no prevLow / prevLowerBand tracking needed for tpwsell.)

        // Forward pass — oldest first (ascending open time)
        foreach (var time in indicatorData.Data.Keys.OrderBy(k => k))
        {
            if (!indicatorData.CandleList.TryGetValue(time, out CryptoCandle candle)
                || !indicatorData.Data.TryGetValue(time, out CryptoData? cd)
                || cd == null)
            {
                // Data gap — reset counter and prev-state tracking
                tpwSell = 0;
                prevExtremeSell = false;
                prevExtremeBuy = false;
                continue;
            }

            // Skip bars before WMA and BB indicators have warmed up
            if (cd.Wma05Low == null || cd.Wma10Low == null || cd.BollingerBandsLowerBand == null)
            {
                prevExtremeSell = false;
                prevExtremeBuy = false;
                continue;
            }

            MyData bar = new() { Candle = candle, CandleData = cd };

            // MQ5 line 823: ext_sell[i-1] → tpwsell=1, tpwbuy=0
            if (prevExtremeSell)
                tpwSell = 1;

            // MQ5 line 829: ext_buy[i-1] → tpwbuy=1, tpwsell=0  (cross-reset)
            if (prevExtremeBuy)
                tpwSell = 0;

            // MQ5 line 836: tpwsell==1 AND low[i] <= malo5[i] OR malo10[i] → tpwsell=2 (TPW fires)
            bool isTpwFiredHere = false;
            if (tpwSell == 1)
            {
                decimal low = candle.Low;
                decimal malo5 = (decimal)cd.Wma05Low.Value;
                decimal malo10 = (decimal)cd.Wma10Low.Value;
                if (low <= malo5 || low <= malo10)
                {
                    tpwSell = 2;
                    isTpwFiredHere = true;
                }
            }

            // MQ5 sell reset is commented out in the source — not applied here.

            TpwStateCache[time] = new OmniBarState
            {
                TpwSellCount = tpwSell,
                IsTpwSellFired = isTpwFiredHere,
            };

            // Store this bar's extremes for the next iteration's [i-1] checks
            prevExtremeSell = IsExtreme(bar);
            prevExtremeBuy = isExtremeBuy?.Invoke(bar) ?? false;
        }
    }


    /// <summary>
    /// Exposes IsExtreme so Bbma.cs (CryptoScanner project) can pass it as a cross-reset
    /// delegate to SignalBbmaOmniLong.BuildTpwCache.
    /// </summary>
    public bool IsExtremeSellBar(MyData data) => IsExtreme(data);


    /// <summary>
    /// MHV Sell — OmniView lines 878-896 (placed at cursor = bar[i-1] when next = bar[i] confirms fractal).
    ///
    /// Conditions (all must hold):
    ///   1. IsTpwSellPhaseActive(cursor) — tpwsell ≥ 2 in MQ5 terms.
    ///   2. Fractal Up at cursor: prev.High &lt; cursor.High AND next.High &lt; cursor.High  (strict right).
    ///   3. cursor.High &gt; cursor.Mid (high of the fractal bar is above BB-mid).
    ///   4. No CSM Buy (mmt_buy) at cursor or next — via <see cref="SignalBbmaOmniBase.OppositeCsmChecker"/>.
    /// </summary>
    public bool IsMhvSell(MyData cursor, MyData next)
    {
        // Gate: TPW Sell phase must be active at cursor
        if (!IsTpwSellPhaseActive(cursor))
            return false;

        // cursor.High > mid
        decimal midCursor = (decimal)cursor.CandleData!.Sma20!.Value;
        if (cursor.Candle.High <= midCursor)
            return false;

        // tpw_sell[i]==EMPTY_VALUE && tpw_sell[i-1]==EMPTY_VALUE (OmniView.mq5 line 878):
        // MHV cannot fire on the same bar TPW just fired, nor on the bar after.
        if (IsTpw(cursor) || IsTpw(next))
            return false;

        // No CSM (mmt_buy, the LONG classifier's momentum — close >= UpperBand) at cursor or
        // next. mmt_buy[i]==EMPTY_VALUE && mmt_buy[i-1]==EMPTY_VALUE (OmniView.mq5 line 878).
        // IsCsm on THIS (short) class is mmt_sell, the wrong side entirely — must go through
        // the opposite-side delegate, wired up in IsSignal() from an ephemeral Long instance.
        if (OppositeCsmChecker != null && (OppositeCsmChecker(cursor) || OppositeCsmChecker(next)))
            return false;

        // Fractal Up: need the bar before cursor (prev)
        if (!GetPrevCandle(cursor, out MyData? prev) || prev == null)
            return false;

        decimal highCursor = cursor.Candle.High;
        decimal highPrev = prev.Candle.High;
        decimal highNext = next.Candle.High;

        // CalculateDynamicFractals (OmniView.mq5 lines 1067-1105): left check fails only when
        // current > left (i.e. left <= current is fine — NON-STRICT). Right check fails when
        // current <= right (i.e. right must be strictly smaller — STRICT).
        // barsLeft=1: prev.High <= cursor.High (non-strict)
        // barsRight=1: next.High < cursor.High (strict)
        bool fractalUp = highPrev <= highCursor && highNext < highCursor;
        return fractalUp;
    }


    /// <summary>
    /// RejectedEMA50 Sell — OmniView lines 941-942.
    ///   high[i] &gt; EMA50[i] AND close[i] &lt; EMA50[i]
    ///   AND sinceDownTrend &lt; 4 (downtrend was active within the last 4 bars)
    ///   AND sinceBigBody &lt; 6 (a big-body candle in the last 6 bars)
    ///
    /// sinceDownTrend: BarsSinceTrend(isDown=true, limit=4)
    ///   → walks back until high[j] &lt; ema50[j] AND high[j-1] &lt; ema50[j-1] (two-bar downtrend).
    /// sinceBigBody: BarsSinceBigBody(limit=6)
    ///   → walks back until |close-open| &gt; 0.5 * ATR14.
    ///
    /// Returns false when ATR14 is unavailable (null-safe).
    /// </summary>
    private bool IsRejectedEma50(MyData data)
    {
        if (data.CandleData!.Ema50 == null)
            return false;

        decimal high = data.Candle.High;
        decimal close = data.Candle.Close;
        decimal ema50 = (decimal)data.CandleData!.Ema50.Value;

        if (high <= ema50 || close >= ema50)
            return false;

        int sinceDownTrend = BarsSinceTrend(data, isDown: true, limit: 4);
        if (sinceDownTrend >= 4)
            return false;

        int sinceBigBody = BarsSinceBigBody(data, limit: 6);
        if (sinceBigBody >= 6)
            return false;

        return true;
    }


    /// <summary>
    /// GAPBBtoEMA50 Sell — OmniView lines 959-965.
    ///   (EMA50[i-1..i-3] or EMA50[i]) &gt; UpperBand on any of those bars  (EMA50 outside band in last 4 bars)
    ///   AND close[i] &lt; open[i] AND close[i] &lt; UpperBand[i]             (bearish candle closed inside)
    ///   AND (high[i] ≥ UB[i] OR high[i-1] ≥ UB[i-1] OR high[i-2] ≥ UB[i-2]) (price touched the band in last 3 bars)
    /// </summary>
    private bool IsGapBbEma50(MyData data)
    {
        if (data.CandleData!.Ema50 == null)
            return false;

        decimal close = data.Candle.Close;
        decimal open = data.Candle.Open;
        decimal high = data.Candle.High;
        decimal upperB = (decimal)data.CandleData!.BollingerBandsUpperBand!.Value;

        // Bearish candle closed inside the band
        if (close >= open || close >= upperB)
            return false;

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

        decimal upperBPrev = (decimal)prev.CandleData!.BollingerBandsUpperBand!.Value;
        decimal upperBPrev2 = (decimal)prev2.CandleData!.BollingerBandsUpperBand!.Value;
        decimal upperBPrev3 = (decimal)prev3.CandleData!.BollingerBandsUpperBand!.Value;

        // EMA50 above UpperBand on any of i, i-1, i-2, i-3
        bool ema50AboveUb =
              (decimal)data.CandleData!.Ema50.Value > upperB
           || (decimal)prev.CandleData!.Ema50.Value > upperBPrev
           || (decimal)prev2.CandleData!.Ema50.Value > upperBPrev2
           || (decimal)prev3.CandleData!.Ema50.Value > upperBPrev3;
        if (!ema50AboveUb)
            return false;

        // Price touched the upper band in last 3 bars (i, i-1, i-2)
        bool highTouched =
              high >= upperB
           || prev.Candle.High >= upperBPrev
           || prev2.Candle.High >= upperBPrev2;
        return highTouched;
    }


    /// <summary>
    /// Reentry Sell — OmniView lines 920-923.
    ///   (high ≥ mahi5 OR high ≥ mahi10)
    /// AND (close ≤ mahi5 OR close ≤ mahi10)
    /// AND close ≤ mid (MiddleBuffer)
    /// </summary>
    private bool IsReentry(MyData data)
    {
        decimal close = data.Candle.Close;
        decimal high = data.Candle.High;
        decimal mid = (decimal)data.CandleData!.Sma20!.Value;
        decimal mahi5 = (decimal)data.CandleData!.Wma05High!.Value;
        decimal mahi10 = (decimal)data.CandleData!.Wma10High!.Value;

        bool touchedMa = high >= mahi5 || high >= mahi10;
        bool closedBack = close <= mahi5 || close <= mahi10;
        return touchedMa && closedBack && close <= mid;
    }


    /// <summary>
    /// HTF validation — mirror of <see cref="SignalBbmaOmniLong.CheckHtf"/>.
    /// MHV Sell requires knowledge of the next bar — we track nextCursor inside the loop.
    /// </summary>
    private bool CheckHtf(CryptoInterval interval, MyData current, out string htfSetup)
    {
        htfSetup = "";

        const int CsmLookback = 20;
        const int CsdLookback = 20;
        const int TpwLookback = 10;
        const int MhvLookback = 10;
        const int MinGap = 3;

        int csmIndex = -1;
        int csdIndex = -1;
        int tpwIndex = -1;
        int mhvIndex = -1;

        MyData? cursor = current;
        MyData? nextCursor = null;
        int max = Math.Max(CsmLookback, Math.Max(CsdLookback, Math.Max(TpwLookback, MhvLookback)));
        for (int i = 0; i < max; i++)
        {
            nextCursor = cursor;
            if (!GetPrevCandle(interval, cursor, out cursor) || cursor == null)
                break;

            // Independent buffer check — mirrors reading csak_sell[i]/csak2_sell[i]/csaa_sell[i]/
            // CrossEMA50mBB_sell[i]/mmt_sell[i]/tpw_sell[i] directly, not a single derived state.
            OmniBar bar = GetOmniBar(cursor);
            if (csmIndex < 0 && i < CsmLookback && bar.Csm) csmIndex = i;
            // CSD, CSAK2, CSAA, and Cross are all treated as "CSD-class" setup signals for HTF validation
            if (csdIndex < 0 && i < CsdLookback && bar.CsdClass) csdIndex = i;
            if (tpwIndex < 0 && i < TpwLookback && bar.Tpw) tpwIndex = i;

            if (mhvIndex < 0 && i < MhvLookback && nextCursor != null && IsMhvSell(cursor, nextCursor))
                mhvIndex = i;
        }

        // Priority 1 — MHV
        if (mhvIndex >= 0 && csmIndex > mhvIndex && csmIndex - mhvIndex >= MinGap)
        {
            htfSetup = "MHV";
            return true;
        }

        // Priority 2 — TPW
        if (tpwIndex >= 0 && csmIndex > tpwIndex && csmIndex - tpwIndex >= MinGap)
        {
            htfSetup = "TPW";
            return true;
        }

        // Priority 3 — CSM/Reentry
        if (csmIndex >= 0)
        {
            htfSetup = "CSM";
            return true;
        }

        // Priority 4 — CSD-class
        if (csdIndex >= 0)
        {
            htfSetup = "CSD";
            return true;
        }

        return false;
    }


    /// <summary>
    /// Invalidate the setup when a Long Extreme prints on the current candle.
    ///
    /// BUGFIX: see <see cref="SignalBbmaOmniLong.GiveUp"/> for the rationale — this used to
    /// check this (short) class's OWN Extreme (ext_sell, bearish exhaustion), which reinforces
    /// the short bias rather than invalidating it. The actual invalidation is the opposite-side
    /// Extreme (ext_buy), via an ephemeral Long instance.
    /// </summary>
    public override bool GiveUp(CryptoSignal signal)
    {
        var opposite = new SignalBbmaOmniLong
        {
            Symbol = Symbol,
            Interval = Interval,
            SymbolInterval = SymbolInterval,
            SignalSide = CryptoTradeSide.Long,
            SignalStrategy = SignalStrategy,
            CandleLast = CandleLast,
        };
        return opposite.IsExtremeBuyBar(CandleLast);
    }


    public override bool IsSignal()
    {
        ExtraText = "";
        string logPrefix = $"{Symbol.Name} {Interval.Name} bbma.omni {SignalSide} ";

        // Ephemeral opposite-side (Long) classifier, used purely to evaluate its Extreme
        // condition — needed to reproduce the MQ5 CSM gate (mmt_sell requires ext_buy[i]==EMPTY)
        // and the TPW cross-reset (ext_buy[i-1] resets tpwsell to 0). GetPrevCandle/GetOmniState
        // only read Symbol/Interval/SymbolInterval, so this instance is safe to share read-only.
        var opposite = new SignalBbmaOmniLong
        {
            Symbol = Symbol,
            Interval = Interval,
            SymbolInterval = SymbolInterval,
            SignalSide = CryptoTradeSide.Long,
            SignalStrategy = SignalStrategy,
            CandleLast = CandleLast,
        };
        OppositeExtremeChecker = opposite.IsExtremeBuyBar;
        OppositeCsmChecker = opposite.IsCsmBuyBar;

        // Build the forward-pass TPW cache before any GetOmniState calls, with the
        // Long-side Extreme as the cross-reset delegate (OmniView.mq5 line 829).
        BuildTpwCache(SymbolInterval, opposite.IsExtremeBuyBar);

        //if (!CandleLast.CheckBollingerBandsWidth(GlobalData.Settings.Signal.Stobb.BBMinPercentage, 100))
        //{
        //    ExtraText = $"bb.width too small {CandleLast.CandleData!.BollingerBandsPercentage:N2}";
        //    return false;
        //}

        // Checked directly on the buffer — see SignalBbmaOmniLong.IsSignal for the rationale
        // (GetOmniState would silently miss a true Reentry whenever this candle ALSO qualifies
        // for a higher-priority buffer, e.g. Csd).
        MyData? candleLtf = CandleLast;
        if (!GetOmniBar(candleLtf).Reentry)
        {
            ExtraText = $"LTF not in Reentry ({GetOmniState(candleLtf)})";
            return false;
        }

        // From here on we log every step — reaching this point is already meaningful
        //GlobalData.AddTextToLogTab($"{logPrefix} [{CandleLast.Candle.Date.ToLocalTime():yyyy-MM-dd HH:mm}] LTF=Reentry, starting evaluation");

        if (!GetIntervals(out CryptoIntervalPeriod mtf, out CryptoIntervalPeriod htf))
        {
            //GlobalData.AddTextToLogTab($"{logPrefix} GetIntervals failed");
            return false;
        }

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

            // Check MHV Sell first (requires next bar)
            if (candleLtfNext != null && IsMhvSell(candleLtf, candleLtfNext))
            {
                stateLtfBack = OmniState.Mhv;
                break;
            }

            // Independent buffer check (AnyTrigger) — not a single derived state.
            OmniBar barBack = GetOmniBar(candleLtf);
            if (barBack.AnyTrigger)
            {
                stateLtfBack = DeriveLabel(barBack);
                break;
            }
            stateLtfBack = OmniState.None;
        }

        if (stateLtfBack == OmniState.None || stateLtfBack == OmniState.Reentry)
        {
            ExtraText = $"LTF no preceding setup found (last: {stateLtfBack})";
            //GlobalData.AddTextToLogTab($"{logPrefix} LTF walkback: no setup found (last={stateLtfBack})");
            return false;
        }
        //GlobalData.AddTextToLogTab($"{logPrefix} LTF walkback: found {stateLtfBack}");

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

        var resultHtf = IndicatorEngine.CalculateIndicatorsForInterval(
            Symbol, Interval, CandleLast.Candle.OpenTime, htf);
        if (!resultHtf.success || resultHtf.candle == null || !IndicatorsOkay(resultHtf.candle))
        {
            ExtraText = $"no data for HTF";
            //GlobalData.AddTextToLogTab($"{logPrefix} HTF ({resultHtf.higherInterval.Interval.Name}): no data (success={resultHtf.success})");
            return false;
        }
        // HTF trend filter: EMA50 above mid-BB AND Wma05High above mid-BB → bearish bias
        double ema50Htf = resultHtf.candle.CandleData!.Ema50!.Value;
        double midBbHtf = resultHtf.candle.CandleData!.Sma20!.Value;
        double wma05HighHtf = resultHtf.candle.CandleData!.Wma05High!.Value;
        if (ema50Htf <= midBbHtf || wma05HighHtf <= midBbHtf)
        {
            ExtraText = $"HTF ema50 not above mid-BB — bullish bias";
            //GlobalData.AddTextToLogTab($"{logPrefix} HTF trend filter failed: ema50={ema50Htf:F4} wma05High={wma05HighHtf:F4} mid={midBbHtf:F4}");
            return false;
        }

        // HTF must currently be in Reentry as well — checked on the buffer directly (see
        // SignalBbmaOmniLong.IsSignal for the rationale).
        OmniBar htfBar = GetOmniBar(resultHtf.candle);
        if (!htfBar.Reentry)
        {
            ExtraText = $"HTF not in Reentry ({DeriveLabel(htfBar)})";
            //GlobalData.AddTextToLogTab($"{logPrefix} HTF not Reentry (={DeriveLabel(htfBar)})");
            return false;
        }
        OmniState stateHtf = DeriveLabel(htfBar);
        //GlobalData.AddTextToLogTab($"{logPrefix} HTF ({resultHtf.higherInterval.Interval.Name})={stateHtf}");

        if (!CheckHtf(resultHtf.higherInterval.Interval, resultHtf.candle, out string htfSetup))
        {
            ExtraText = $"HTF no CSM/CSD/TPW/MHV setup";
            //GlobalData.AddTextToLogTab($"{logPrefix} HTF CheckHtf: no setup found");
            return false;
        }
        //GlobalData.AddTextToLogTab($"{logPrefix} HTF CheckHtf: found {htfSetup}");

        // Code match — order: HTF + MTF + LTF (highest TF first).
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
