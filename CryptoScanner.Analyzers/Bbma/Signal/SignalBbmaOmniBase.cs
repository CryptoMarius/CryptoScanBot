using CryptoScanner.Core.Enums;
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
    protected internal Func<MyData, bool>? OppositeExtremeChecker;

    /// <summary>
    /// Optional check for the OPPOSITE-side momentum (CSM), used to reproduce the MQ5 MHV gate
    /// (MHV Buy requires mmt_sell[i]==EMPTY && mmt_sell[i-1]==EMPTY — OmniView.mq5 line 857;
    /// MHV Sell requires mmt_buy[i]==EMPTY && mmt_buy[i-1]==EMPTY — line 878). Wired up in
    /// IsSignal() via an ephemeral instance of the other side's classifier (IsCsmBuyBar /
    /// IsCsmSellBar). Without this, IsMhvBuy/IsMhvSell would have to check their OWN class's
    /// momentum, which is the wrong side entirely (mmt_buy is irrelevant to gating MHV Buy).
    /// </summary>
    protected internal Func<MyData, bool>? OppositeCsmChecker;


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
    //   R = Reentry, 2 = Csak2, A = Csaa, X = Cross, D = Csd, M = Csm, - = None
    //
    // The NEW path has no fixed whitelist — it fires whenever HTF code = 'R' AND
    // the LTF lookback code is neither '-' nor 'R'. So in addition to the four
    // old codes you also get setups starting with T / J / G / 2 / A / X / D / M
    // at the LTF position. The "[htfSetup]" text in ExtraText shows the HTF
    // precondition name (CSD / CSM / TPW / MHV).
    //
    // Until 2026-09-05 Csd and Csm both mapped to '-', which made the code-match
    // REJECT every Reentry whose most recent trigger was a CSD or a CSM candle.
    // Those are exactly the two setups the BBMA rules call the strongest ("Reentry
    // after CSD" and "Reentry after CSM", see Bbma.md), so they now carry their own
    // letters D and M and pass the code-match like every other trigger.
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
    public static string OmniStateCode(OmniState state) => state switch
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
        OmniState.Csd => "D",
        OmniState.Csm => "M",
        _ => "-"   // None: no trigger found
    };

    /// <summary>
    /// The 3-TF code-match rule (HTF + MTF + LTF, highest timeframe first): the HTF must be in
    /// Reentry ('R') and the LTF lookback must have found a real trigger — not '-' (nothing found)
    /// and not 'R' (another Reentry is not a setup by itself). The MTF letter is informational.
    /// </summary>
    public static bool IsCodeMatch(string code)
    {
        if (code.Length != 3)
            return false;
        return code[0] == 'R' && code[2] != '-' && code[2] != 'R';
    }


    // -----------------------------------------------------------------------
    // HTF trend zone (OmniView "Green Zone" / "Red Zone", OmniView.mq5 lines 703-715)
    // -----------------------------------------------------------------------

    /// <summary>
    /// OmniView Green Zone (line 711): EMA50 at or below the BB-mid AND all four WMA's (5/10 on
    /// high and low) at or above the BB-mid. That is the BBMA picture of an uptrend: the EMA50
    /// under the mid-band and the MA5/10 reentry zone above it.
    /// <para>
    /// Until 2026-09-05 the long filter demanded Wma05Low BELOW the mid-band, the opposite of this
    /// zone, so the only HTF candle that passed was a deep correction closing back above the mid.
    /// </para>
    /// </summary>
    public static bool IsHtfTrendBullish(MyData data)
    {
        CryptoData cd = data.CandleData!;
        if (cd.Ema50 == null || cd.Sma20 == null || cd.Wma05Low == null || cd.Wma10Low == null
            || cd.Wma05High == null || cd.Wma10High == null)
            return false;

        double mid = cd.Sma20.Value;
        return cd.Ema50.Value <= mid
            && cd.Wma05Low.Value >= mid && cd.Wma10Low.Value >= mid
            && cd.Wma05High.Value >= mid && cd.Wma10High.Value >= mid;
    }

    /// <summary>
    /// OmniView Red Zone (line 704): EMA50 at or above the BB-mid AND all four WMA's at or below
    /// the BB-mid — the mirror of <see cref="IsHtfTrendBullish"/>.
    /// </summary>
    public static bool IsHtfTrendBearish(MyData data)
    {
        CryptoData cd = data.CandleData!;
        if (cd.Ema50 == null || cd.Sma20 == null || cd.Wma05Low == null || cd.Wma10Low == null
            || cd.Wma05High == null || cd.Wma10High == null)
            return false;

        double mid = cd.Sma20.Value;
        return cd.Ema50.Value >= mid
            && cd.Wma05Low.Value <= mid && cd.Wma10Low.Value <= mid
            && cd.Wma05High.Value <= mid && cd.Wma10High.Value <= mid;
    }


    // -----------------------------------------------------------------------
    // Classifying candles of another interval (MTF / HTF)
    // -----------------------------------------------------------------------

    /// <summary>
    /// The side-specific per-bar classification (GetOmniBar in the Long/Short subclass), reachable
    /// from the base so <see cref="CheckHtf"/> can be written once.
    /// </summary>
    public virtual OmniBar ClassifyBar(MyData data) => throw new NotImplementedException();

    /// <summary>The side-specific display label of a bar (DeriveLabel over <see cref="ClassifyBar"/>).</summary>
    public OmniState ClassifyState(MyData data) => DeriveLabel(ClassifyBar(data));

    /// <summary>The side-specific MHV check (IsMhvBuy / IsMhvSell in the subclass).</summary>
    public virtual bool IsMhv(MyData cursor, MyData next) => throw new NotImplementedException();

    /// <summary>
    /// A classifier of the same side for another interval of the same symbol, with the
    /// opposite-side checkers wired up for THAT interval. Every multi-bar condition (the two-bar
    /// CSD, the Extreme anti-repeat guard, GapBbEma50, the EMA50 rejection, the TPW backward scan)
    /// reads its previous candles through the single-argument GetPrevCandle, which follows the
    /// instance's own Interval. Until 2026-09-05 the HTF and MTF candles were classified by the
    /// LTF instance, so "the previous candle" of a 1d candle was the 1h candle before its open —
    /// the wrong series entirely. MTF and HTF candles must go through an instance made here.
    /// </summary>
    public virtual SignalBbmaOmniBase CreateForInterval(CryptoInterval interval) => throw new NotImplementedException();


    // -----------------------------------------------------------------------
    // The HTF setup: a reentry is a setup only after a CSD or a CSM on the same side
    // -----------------------------------------------------------------------

    /// <summary>
    /// Is the HTF reentry at <paramref name="current"/> preceded by a setup? The rules give two
    /// reentry setups, "Reentry after CSD" and "Reentry after CSM", so the most recent CSD (or
    /// CSAK2, its early form) or CSM on this side within <see cref="BbmaSettings.HtfSetupLookback"/>
    /// candles is the setup. It is void when the market has since said the other way: an
    /// opposite-side CSM (a close beyond the far band) between the setup and now always
    /// invalidates it, an opposite-side Extreme (exhaustion at the far band, the start of the next
    /// cycle in the rules) does so when <see cref="BbmaSettings.HtfSetupExtremeInvalidates"/> is on.
    /// <para>
    /// Must run on the classifier of the HTF (see <see cref="CreateForInterval"/>): the previous
    /// candles are read through the instance's own Interval. A lookback of zero switches the
    /// check off. <paramref name="htfSetup"/> carries the setup name ("CSM", "CSD", prefixed with
    /// "TPW>" or "MHV>" when that opened the cycle before it) or the reason of the rejection.
    /// </para>
    /// <para>
    /// Until 2026-09-05 this check fell through to "any CSM or CSD-class candle in the last twenty",
    /// which every trending HTF satisfies, and its two priority rules wanted the CSM OLDER than
    /// the MHV or TPW — the reverse of the cycle. It never blocked anything.
    /// </para>
    /// </summary>
    public bool CheckHtf(MyData current, out string htfSetup)
    {
        BbmaSettings settings = BbmaPlugin.Settings;
        int lookback = settings.HtfSetupLookback;
        if (lookback <= 0)
        {
            htfSetup = "any";
            return true;
        }

        MyData? cursor = current;
        MyData? next;
        int setupIndex = -1;
        htfSetup = "";
        for (int i = 0; i < lookback; i++)
        {
            next = cursor;
            if (!GetPrevCandle(cursor, out cursor) || cursor == null)
                break;

            // Newer than the setup: anything against us here voids it.
            if (OppositeCsmChecker != null && OppositeCsmChecker(cursor))
            {
                htfSetup = $"opposite CSM {i + 1} candle(s) back";
                return false;
            }
            if (settings.HtfSetupExtremeInvalidates && OppositeExtremeChecker != null && OppositeExtremeChecker(cursor))
            {
                htfSetup = $"opposite Extreme {i + 1} candle(s) back";
                return false;
            }

            OmniBar bar = ClassifyBar(cursor);
            if (bar.Csm)
            {
                htfSetup = "CSM";
                setupIndex = i;
                break;
            }
            if (bar.Csd || bar.Csak2)
            {
                htfSetup = "CSD";
                setupIndex = i;
                break;
            }
        }

        if (setupIndex < 0)
        {
            htfSetup = $"no CSD/CSM within {lookback} candle(s)";
            return false;
        }

        // Label only: the TPW or MHV that opened the cycle before the setup (Extreme → TPW → MHV →
        // CSD → Reentry → CSM → Reentry), within the same lookback.
        for (int i = setupIndex + 1; i < lookback; i++)
        {
            next = cursor;
            if (!GetPrevCandle(cursor, out cursor) || cursor == null)
                break;
            if (next != null && IsMhv(cursor, next))
            {
                htfSetup = "MHV>" + htfSetup;
                break;
            }
            if (ClassifyBar(cursor).Tpw)
            {
                htfSetup = "TPW>" + htfSetup;
                break;
            }
        }
        return true;
    }


    // -----------------------------------------------------------------------
    // Reentry: the OmniView "AllBBMA" variant, or the strict reading of the rules
    // -----------------------------------------------------------------------

    /// <summary>
    /// Reentry Buy. The loose form is OmniView lines 925-929 ("AllBBMA version"): the low touched
    /// MA5 or MA10 (low), the close is back above MA5 or MA10, and the close is at or above the
    /// mid-band. The strict form is the rule as documented (and the "TradingView version" OmniView
    /// keeps commented out on lines 916-917): the candle must not close beyond the MA5/10 zone, so
    /// the close is back above BOTH MA5 and MA10, and the zone itself sits at or above the mid-band
    /// (MA5 or MA10 at or above the mid) — a pullback in an uptrend, not a bounce under the mid.
    /// </summary>
    public static bool IsReentryBuy(MyData data, bool strict)
    {
        decimal close = data.Candle.Close;
        decimal low = data.Candle.Low;
        decimal mid = (decimal)data.CandleData!.Sma20!.Value;
        decimal malo5 = (decimal)data.CandleData!.Wma05Low!.Value;
        decimal malo10 = (decimal)data.CandleData!.Wma10Low!.Value;

        bool touchedMa = low <= malo5 || low <= malo10;
        if (!touchedMa || close < mid)
            return false;

        if (!strict)
            return close >= malo5 || close >= malo10;

        bool closedAboveZone = close >= malo5 && close >= malo10;
        bool zoneAboveMid = malo5 >= mid || malo10 >= mid;
        return closedAboveZone && zoneAboveMid;
    }

    /// <summary>Reentry Sell — the mirror of <see cref="IsReentryBuy"/> on the MA5/10 (high) zone.</summary>
    public static bool IsReentrySell(MyData data, bool strict)
    {
        decimal close = data.Candle.Close;
        decimal high = data.Candle.High;
        decimal mid = (decimal)data.CandleData!.Sma20!.Value;
        decimal mahi5 = (decimal)data.CandleData!.Wma05High!.Value;
        decimal mahi10 = (decimal)data.CandleData!.Wma10High!.Value;

        bool touchedMa = high >= mahi5 || high >= mahi10;
        if (!touchedMa || close > mid)
            return false;

        if (!strict)
            return close <= mahi5 || close <= mahi10;

        bool closedBelowZone = close <= mahi5 && close <= mahi10;
        bool zoneBelowMid = mahi5 <= mid || mahi10 <= mid;
        return closedBelowZone && zoneBelowMid;
    }

    /// <summary>
    /// The rules want the pullback to take its time: "a reentry occurs for a minimum of three
    /// candles". Read as: the trigger (CSD, CSM, Extreme, ...) the LTF walkback found has to be at
    /// least <paramref name="minimum"/> candles behind the reentry candle. Zero switches it off.
    /// </summary>
    public static bool TriggerTooRecent(int candlesSinceTrigger, int minimum)
        => minimum > 0 && candlesSinceTrigger < minimum;


    // -----------------------------------------------------------------------
    // The strategy's own exit: take profit at the outer band, stop beyond the reentry candle
    // -----------------------------------------------------------------------

    /// <summary>
    /// The stop-loss distance the signal hands to the trader (OverrideSlPercentage), set by
    /// IsSignal in the Long/Short subclass when <see cref="BbmaSettings.StopBeyondReentryCandle"/>
    /// is on: the distance from the close to the far side of the reentry candle plus the margin.
    /// </summary>
    protected decimal? SlPercentage;

    public override decimal? OverrideSlPercentage => SlPercentage;

    /// <summary>
    /// Stop distance as a percentage of the close: for a long the distance from the close down to
    /// the low of the reentry candle, for a short from the close up to its high, plus
    /// <paramref name="marginPercentage"/> of extra room. The BBMA rules put the stop beyond the
    /// swing the reentry came from; the reentry candle's own extreme is the tightest version of
    /// that. Returns null when the distance is not positive (a candle that closed on its extreme
    /// without margin) or the close is not usable.
    /// </summary>
    public static decimal? StopPercentageBeyondCandle(MyData candle, CryptoTradeSide side, decimal marginPercentage)
    {
        decimal close = candle.Candle.Close;
        if (close <= 0)
            return null;

        decimal distance = side == CryptoTradeSide.Long
            ? close - candle.Candle.Low
            : candle.Candle.High - close;

        decimal percentage = 100m * distance / close + marginPercentage;
        if (percentage <= 0)
            return null;
        return percentage;
    }

    /// <summary>
    /// The strategy decides when to leave when <see cref="BbmaSettings.TakeProfitAtOuterBand"/>
    /// is on. The trader's stop loss and take profit keep working next to it (set the global take
    /// profit wide to measure the pure band exit).
    /// </summary>
    public override bool HasExitSignal => BbmaPlugin.Settings.TakeProfitAtOuterBand;

    /// <summary>
    /// The band the take profit aims at: the outer band of the position's own interval, or — with
    /// <see cref="BbmaSettings.TakeProfitOnHtfBand"/> — the band of the HTF of the fixed 3-TF
    /// triplet (the rules give the take profit on the outer band of the higher timeframe, e.g. the
    /// D1 band for an H1 entry). The HTF band comes from the last CLOSED HTF candle at the time of
    /// CandleLast. Returns false when there is no band to compare against.
    /// </summary>
    private bool TryGetExitBand(out decimal upper, out decimal lower, out string source)
    {
        upper = 0;
        lower = 0;
        source = Interval.Name;

        CryptoData? cd = CandleLast.CandleData;
        if (BbmaPlugin.Settings.TakeProfitOnHtfBand)
        {
            if (!GetIntervals(out _, out CryptoIntervalPeriod htf))
                return false;
            var result = IndicatorEngine.CalculateIndicatorsForInterval(Symbol, Interval, CandleLast.Candle.OpenTime, htf);
            if (!result.success || result.candle == null)
            {
                ExtraText = $"no HTF data for the take profit band";
                return false;
            }
            cd = result.candle.CandleData;
            source = result.higherInterval.Interval.Name;
        }

        if (cd == null || cd.BollingerBandsUpperBand == null || cd.BollingerBandsLowerBand == null)
            return false;

        upper = (decimal)cd.BollingerBandsUpperBand.Value;
        lower = (decimal)cd.BollingerBandsLowerBand.Value;
        return true;
    }

    /// <summary>
    /// Take profit at the outer Bollinger band, the target the BBMA rules give a reentry trade: a
    /// long leaves once a closed candle has reached the upper band, a short once one has reached
    /// the lower band. Evaluated on the candle that just closed on the position's interval, so the
    /// band is the band of that moment, not the one at signal time. Which band — the position's
    /// own interval or the HTF — is decided by <see cref="TryGetExitBand"/>.
    /// </summary>
    public override bool IsExitSignal()
    {
        ExtraText = "";
        if (!BbmaPlugin.Settings.TakeProfitAtOuterBand)
            return false;

        if (!TryGetExitBand(out decimal upper, out decimal lower, out string source))
            return false;

        if (SignalSide == CryptoTradeSide.Long)
        {
            if (CandleLast.Candle.High < upper)
            {
                ExtraText = $"upper band {upper} ({source}) not reached (high {CandleLast.Candle.High})";
                return false;
            }
            ExtraText = $"reached the upper band {upper} ({source}) (high {CandleLast.Candle.High})";
            return true;
        }
        else
        {
            if (CandleLast.Candle.Low > lower)
            {
                ExtraText = $"lower band {lower} ({source}) not reached (low {CandleLast.Candle.Low})";
                return false;
            }
            ExtraText = $"reached the lower band {lower} ({source}) (low {CandleLast.Candle.Low})";
            return true;
        }
    }

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
