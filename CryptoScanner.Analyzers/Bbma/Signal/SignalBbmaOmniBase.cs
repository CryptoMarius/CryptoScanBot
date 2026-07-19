using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Analyzers.Bbma.Signal;

/// <summary>
/// Base class for the BBMA Omni strategy. State classification is a direct port of the
/// "BBMA Oma Ally OmniView.mq5" formulas — kept as close to the MQL5 source as possible
/// so we can cross-reference. The multi-timeframe setup (GetIntervals, TfStateCode for
/// the code-match string) is inherited from SignalBbmaBase.
///
/// Ported from OmniView:
///   - CSAK (CSD)          : single-bar and two-bar BB-mid cross + beyond WMA5/10.
///   - CSAK2 (Csak2)       : continuation — both open &amp; close beyond mid/WMA, not at outer band.
///   - Extreme             : WMA poke outside band + wick rejection.
///   - Momentum (CSM)      : close beyond outer BB, gated by no Extreme on same bar.
///   - CSAA                : WMA zone above/below mid, candle pulls back through WMA zone.
///   - CrossEMA50mBB (Cross): BB-mid or EMA50 crossover confirmed by the other level.
///   - Reentry (AllBBMA version): pullback to WMA zone, close correct side of mid.
///   - TPW                 : first WMA-zone touch after an Extreme (state-machine approximated
///                           via backward scan; no persistent counter needed).
///   - MHV                 : fractal pivot confirmed at i-1 once bar i is known; requires a
///                           (cursor, next) two-parameter call — see IsMhvBuy / IsMhvSell.
///   - RejectedEMA50       : EMA50 wick rejection filtered by ATR body size + trend context.
///   - GAPBBtoEMA50        : EMA50 outside BB in last 4 bars, price returns inside.
///
/// TPW state machine — forward-pass cache (see BuildTpwCache in Long/Short subclasses):
///   A Dictionary&lt;CandleTime, OmniBarState&gt; is built oldest→newest before any GetOmniState
///   calls, matching the MQ5 tpwbuy/tpwsell integer counters exactly.
///   IsTpw / IsTpwBuyPhaseActive / IsTpwSellPhaseActive use this cache first; they fall back
///   to the backward-scan approximation only for HTF candles (CheckHtf) whose open times are
///   not in the LTF cache.
/// </summary>
public class SignalBbmaOmniBase : SignalBbmaBase
{
    // -----------------------------------------------------------------------
    // Forward-pass TPW state machine cache
    // -----------------------------------------------------------------------

    /// <summary>
    /// Per-bar state produced by BuildTpwCache, matching MQ5 tpwbuy/tpwsell integer counters.
    /// Long subclass populates TpwBuyCount / IsTpwBuyFired.
    /// Short subclass populates TpwSellCount / IsTpwSellFired.
    /// </summary>
    public struct OmniBarState
    {
        /// <summary>
        /// Buy-side counter after processing this bar:
        ///   0 = inactive, 1 = armed (Extreme Buy on prev bar),
        ///   2 = TPW fired (WMA-high zone touched while armed).
        /// Resets to 0 when high[i-1] &gt; UpperBand[i-1] while &ge;2 (MQ5 line 898).
        /// Also resets to 0 when Sell Extreme fires on prev bar (cross-reset).
        /// </summary>
        public int TpwBuyCount;

        /// <summary>True iff tpwbuy transitioned 1→2 on THIS bar (TPW Buy signal drawn here).</summary>
        public bool IsTpwBuyFired;

        /// <summary>
        /// Sell-side counter after processing this bar:
        ///   0 = inactive, 1 = armed, 2 = TPW fired.
        /// (The MQ5 sell reset on low[i-1] &lt; LowerBand[i-1] is commented-out in the source.)
        /// </summary>
        public int TpwSellCount;

        /// <summary>True iff tpwsell transitioned 1→2 on THIS bar (TPW Sell signal drawn here).</summary>
        public bool IsTpwSellFired;
    }

    /// <summary>
    /// Per-bar TPW state cache keyed by candle open time.
    /// Built by BuildTpwCache in each Long/Short subclass before any GetOmniState calls.
    /// </summary>
    protected Dictionary<CandleTime, OmniBarState> TpwStateCache { get; } = [];

    /// <summary>
    /// Optional check for an Extreme on the OPPOSITE side, used to reproduce the MQ5 CSM gate
    /// (mmt_buy requires ext_sell[i]==EMPTY, mmt_sell requires ext_buy[i]==EMPTY — OmniView.mq5
    /// lines 904/909). Wired up in IsSignal() via an ephemeral instance of the other side's
    /// classifier (IsExtremeBuyBar / IsExtremeSellBar). Left null, Csm falls back to the
    /// un-gated check (own-side Extreme only, via GetOmniState priority order).
    /// </summary>
    protected Func<MyData, bool>? OppositeExtremeChecker;

    /// <summary>
    /// Optional check for the OPPOSITE-side momentum (CSM), used to reproduce the MQ5 MHV gate
    /// (MHV Buy requires mmt_sell[i]==EMPTY && mmt_sell[i-1]==EMPTY — OmniView.mq5 line 857;
    /// MHV Sell requires mmt_buy[i]==EMPTY && mmt_buy[i-1]==EMPTY — line 878). Wired up in
    /// IsSignal() via an ephemeral instance of the other side's classifier (IsCsmBuyBar /
    /// IsCsmSellBar). Without this, IsMhvBuy/IsMhvSell would have to check their OWN class's
    /// momentum, which is the wrong side entirely (mmt_buy is irrelevant to gating MHV Buy).
    /// </summary>
    protected Func<MyData, bool>? OppositeCsmChecker;


    // -----------------------------------------------------------------------
    // OmniState enum and helpers
    // -----------------------------------------------------------------------

    // ===========================================================================
    // BBMA Omni — code translation (OLD BBMA vs NEW Omni) and chart symbol legend
    // ===========================================================================
    //
    // CODE TRANSLATION
    // ----------------
    // The OLD SignalBbMaLong/Short whitelist (RRE, REM, REE, RMEE) used different
    // single-letter codes than this Omni implementation. The most relevant difference
    // is that MLV/MHV is now 'H' instead of 'M'. MagicExtreme was already merged into
    // Extreme in the old code (RMEE was matched as "RME" there).
    //
    //   OLD code   OLD meaning (HTF / MTF / LTF)                  NEW Omni code
    //   --------   --------------------------------------------   -------------
    //   RRE        Reentry / Reentry / Extreme                    RRE  (same)
    //   REM        Reentry / Extreme / MHV                        REH
    //   REE        Reentry / Extreme / Extreme                    REE  (same)
    //   RMEE       Reentry / MHV / Extreme (after merge: RME)     RHE
    //
    // OLD letter codes (see SignalBbmaBase.TfStateCode):
    //   E = Extreme, EE = MagicExtreme (merged into Extreme), M = MLV/MHV, R = Reentry
    //
    // NEW Omni letter codes (see OmniStateCode below):
    //   E = Extreme, T = Tpw, H = Mhv, J = RejectedEma50, G = GapBbEma50,
    //   R = Reentry, 2 = Csak2, A = Csaa, X = Cross, - = Csd / Csm
    //
    // The NEW path has no fixed whitelist — it fires whenever HTF code = 'R' AND
    // the LTF lookback code is neither '-' nor 'R'. So in addition to the four
    // old codes you also get setups starting with T / J / G / 2 / A / X at the
    // LTF position. The "[htfSetup]" text in ExtraText shows the HTF precondition
    // name (CSD / CSM / TPW / MHV), which is how CSD becomes visible in the
    // notification text even though it never appears as an LTF code-letter.
    //
    //
    // CHART WINDOW SYMBOL LEGEND  (drawn by CryptoScanner.ViewModels.Chart.Bbma)
    // -------------------------------------------------------------------------
    // Convention: COLOR encodes direction (LimeGreen = buy, Red = sell),
    //             SHAPE encodes which state it is.
    //
    //   IMPORTANT states (large markers, just outside the candle body):
    //     Extreme    → Triangle    (urgency / exhaustion)
    //     Tpw        → Circle      (first WMA-touch after Extreme)
    //     Mhv        → Diamond     (fractal pivot inside TPW phase)
    //     Reentry    → Square      (the actual entry box)
    //
    //   INTERMEDIATE states (small semi-transparent gray dots, 1-bar offset):
    //     Csd, Csak2, Csaa, Csm, Cross, GapBbEma50, RejectedEma50
    //
    //   Buy markers sit BELOW the candle, sell markers sit ABOVE — so the
    //   color + position together always tell direction unambiguously.
    // ===========================================================================

    /// <summary>
    /// BBMA Omni state — separate from <see cref="BbmaState"/> on purpose so the Omni port
    /// can evolve independently from the Pine-aligned SignalBbma classes.
    /// See the doc-block above this enum for the OLD-vs-NEW code translation table and the
    /// chart window symbol legend.
    /// </summary>
    public enum OmniState
    {
        None,
        Extreme,       // ext_buy / ext_sell        : WMA poke outside BB + wick rejection
        Csd,           // csak_buy / csak_sell       : single- or two-bar BB-mid cross + beyond WMA5/10
        Csak2,         // csak2_buy / csak2_sell     : continuation — both open & close beyond mid/WMA, not at outer band
        Csm,           // mmt_buy / mmt_sell         : close beyond outer BB (gated: no Extreme on same bar)
        Csaa,          // csaa_buy / csaa_sell       : WMA zone above/below mid, candle pulls back through WMA zone
        Cross,         // CrossEMA50mBB buy/sell     : BB-mid or EMA50 cross confirmed by the other level
        Tpw,           // tpw_buy / tpw_sell         : first WMA-zone touch after an Extreme
        Mhv,           // MHV_buy / MHV_sell         : fractal pivot in TPW phase (low[i-1] < mid, confirmed by i)
        RejectedEma50, // rejectedEMA50_buy/sell     : EMA50 wick rejection with ATR body filter + trend context
        GapBbEma50,    // GAPBBtoEMA50_buy/sell      : EMA50 outside BB in last 4 bars, price returns inside
        Reentry,       // ret_buy / ret_sell          : pullback to WMA zone, close correct side of mid
    }

    /// <summary>
    /// Independent per-bar signal buffers — the direct C# equivalent of the MQL5 indicator
    /// buffers (csak_buy[i], csak2_buy[i], ext_buy[i], mmt_buy[i], csaa_buy[i],
    /// CrossEMA50mBB_buy[i], tpw_buy[i], rejectedEMA50_buy[i], GAPBBtoEMA50_buy[i], ret_buy[i]
    /// — or their _sell counterparts). In OmniView.mq5 these are independent arrays: more than
    /// one can be non-EMPTY on the same bar (only Csak2 has an explicit source-level gate
    /// against Csd — "csak_buy[i]==EMPTY_VALUE" — see GetOmniBar). Mhv is intentionally NOT a
    /// field here: it needs the NEXT bar to confirm the fractal, so it stays a two-argument
    /// call (IsMhvBuy/IsMhvSell(cursor, next)), same as in the MQL5 source (placed at i-1 once
    /// bar i confirms it).
    /// </summary>
    public struct OmniBar
    {
        public bool Extreme;
        public bool Csm;
        public bool Csd;
        public bool Csak2;
        public bool Csaa;
        public bool Cross;
        public bool Tpw;
        public bool RejectedEma50;
        public bool GapBbEma50;
        public bool Reentry;

        /// <summary>True when ANY trigger-class buffer fired on this bar (everything except Reentry).</summary>
        public bool AnyTrigger => Extreme || Csm || Csd || Csak2 || Csaa || Cross || Tpw || RejectedEma50 || GapBbEma50;

        /// <summary>True when any of the "CSD-class" buffers fired — Csd, Csak2, Csaa, Cross are
        /// all grouped together for HTF-setup validation purposes (see CheckHtf).</summary>
        public bool CsdClass => Csd || Csak2 || Csaa || Cross;
    }

    /// <summary>
    /// Maps an OmniState to a single-letter code used in the multi-TF code-match string.
    /// The code-match accepts any 3-char code "R??" where position 0 = 'R' (HTF Reentry)
    /// and position 2 (LTF lookback) is not '-' (a meaningful preceding event was found).
    /// </summary>
    internal static string OmniStateCode(OmniState state) => state switch
    {
        OmniState.Extreme => "E",
        OmniState.Tpw => "T",
        OmniState.Mhv => "H",
        OmniState.RejectedEma50 => "J",
        OmniState.GapBbEma50 => "G",
        OmniState.Reentry => "R",
        OmniState.Csak2 => "2",
        OmniState.Csaa => "A",
        OmniState.Cross => "X",
        _ => "-"   // Csd, Csm → "-": HTF setup states, not code-match components
    };

    /// <summary>
    /// Derives a single display/code-match label from an <see cref="OmniBar"/> — used ONLY for
    /// the "[htfSetup]" ExtraText and the 3-TF code-match string (e.g. "RRE"). This priority
    /// order is an INVENTED hierarchy with no MQL5 equivalent (the source buffers are
    /// independent and have no precedence between them, except Csak2-vs-Csd). All actual
    /// pass/fail GATING must read the OmniBar fields directly (see CheckHtf, IsSignal),
    /// never this derived label — multiple buffers can be true on the same bar and a gate that
    /// only checks the single highest-priority label would silently miss the others.
    /// </summary>
    internal static OmniState DeriveLabel(OmniBar bar)
    {
        if (bar.Extreme) return OmniState.Extreme;
        if (bar.Csm) return OmniState.Csm;
        if (bar.Csd) return OmniState.Csd;
        if (bar.Csak2) return OmniState.Csak2;
        if (bar.GapBbEma50) return OmniState.GapBbEma50;
        if (bar.Cross) return OmniState.Cross;
        if (bar.Csaa) return OmniState.Csaa;
        if (bar.Tpw) return OmniState.Tpw;
        if (bar.RejectedEma50) return OmniState.RejectedEma50;
        if (bar.Reentry) return OmniState.Reentry;
        return OmniState.None;
    }


    // -----------------------------------------------------------------------
    // Shared helper methods (used by both Long and Short subclasses)
    // -----------------------------------------------------------------------

    /// <summary>
    /// OmniView BarsSinceBigBody (lines 1009-1020).
    /// Walks backward from the bar BEFORE <paramref name="from"/>, counting candles until a
    /// "big body" is found (|close-open| &gt; 0.5 * ATR14). Returns the zero-based distance
    /// (0 = the immediately preceding bar is already a big body). Returns 9999 when ATR14 is
    /// unavailable or the limit is reached without finding one.
    /// </summary>
    protected int BarsSinceBigBody(MyData from, int limit)
    {
        int count = 0;
        MyData? cursor = from;
        while (count < limit)
        {
            if (!GetPrevCandle(cursor, out cursor) || cursor == null)
                return 9999;

            double? atr = cursor.CandleData!.Atr14;
            if (atr == null || atr.Value == 0)
                return 9999;

            double body = Math.Abs((double)(cursor.Candle.Close - cursor.Candle.Open));
            if (body > 0.5 * atr.Value)
                return count;

            count++;
        }
        return 9999;
    }


    /// <summary>
    /// OmniView BarsSinceTrend (lines 1026-1040).
    /// Walks backward from the bar BEFORE <paramref name="from"/>, counting candles until the
    /// two-bar trend condition is satisfied. When <paramref name="isDown"/> is true the condition
    /// is high[j] &lt; ema50[j] AND high[j-1] &lt; ema50[j-1] (downtrend); otherwise it is
    /// low[j] &gt; ema50[j] AND low[j-1] &gt; ema50[j-1] (uptrend).
    /// Returns 9999 when the limit is reached or EMA50 data is unavailable.
    /// </summary>
    protected int BarsSinceTrend(MyData from, bool isDown, int limit)
    {
        int count = 0;
        MyData? j = from;
        while (count < limit)
        {
            if (!GetPrevCandle(j, out j) || j == null)
                return 9999;
            if (j.CandleData!.Ema50 == null)
                return 9999;

            // We need j-1 for the two-bar condition
            if (!GetPrevCandle(j, out MyData? jPrev) || jPrev == null)
                return 9999;
            if (jPrev.CandleData!.Ema50 == null)
                return 9999;

            double ema50J = j.CandleData!.Ema50.Value;
            double ema50JPrev = jPrev.CandleData!.Ema50.Value;

            bool cond = isDown
                ? ((double)j.Candle.High < ema50J && (double)jPrev.Candle.High < ema50JPrev)
                : ((double)j.Candle.Low > ema50J && (double)jPrev.Candle.Low > ema50JPrev);

            if (cond)
                return count;

            count++;
        }
        return 9999;
    }
}
