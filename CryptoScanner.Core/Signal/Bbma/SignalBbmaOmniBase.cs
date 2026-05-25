#if DEBUG
namespace CryptoScanner.Core.Signal.Bbma;

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
///   - MHV stateless approx (MLV): wick rejection with WMA still inside band.
///
/// Not ported from OmniView:
///   - TPW    : requires persistent state between bars (tpwbuy/tpwsell flags).
///   - MHV-as-fractal : the fractal pivot is plotted at i-1 once i is known;
///                      we approximate MLV statelessly.
///   - RejectedEMA50  : needs ATR + BarsSince helpers (complex, deferred).
///   - GAPBBtoEMA50   : needs EMA50 + 3-bar lookback (deferred).
/// </summary>
public class SignalBbmaOmniBase : SignalBbmaBase
{
    /// <summary>
    /// BBMA Omni state — separate from <see cref="BbmaState"/> on purpose so the Omni port
    /// can evolve independently from the Pine-aligned SignalBbma classes.
    /// </summary>
    public enum OmniState
    {
        None,
        Extreme,  // ext_buy / ext_sell      : WMA poke outside BB + wick rejection
        Csd,      // csak_buy / csak_sell     : single- or two-bar BB-mid cross + beyond WMA5/10
        Csak2,    // csak2_buy / csak2_sell   : continuation — both open & close beyond mid/WMA, not at outer band
        Csm,      // mmt_buy / mmt_sell       : close beyond outer BB (gated: no Extreme on same bar)
        Csaa,     // csaa_buy / csaa_sell     : WMA zone above/below mid, candle pulls back through WMA zone
        Cross,    // CrossEMA50mBB buy/sell   : BB-mid or EMA50 cross confirmed by the other level
        Mlv,      // MHV stateless approx     : wick rejection at outer BB with WMA still inside band
        Reentry,  // ret_buy / ret_sell        : pullback to WMA zone, close correct side of mid
    }

    /// <summary>
    /// Maps an OmniState to a single-letter code used in the multi-TF code-match string.
    /// The code-match accepts any 3-char code "R??" where position 0 = 'R' (HTF Reentry)
    /// and position 2 (LTF lookback) is not '-' (a meaningful preceding event was found).
    /// </summary>
    internal static string OmniStateCode(OmniState state) => state switch
    {
        OmniState.Extreme  => "E",
        OmniState.Mlv      => "M",
        OmniState.Reentry  => "R",
        OmniState.Csak2    => "2",
        OmniState.Csaa     => "A",
        OmniState.Cross    => "X",
        _                  => "-"   // Csd, Csm → "-": they are HTF setup states, not code-match components
    };
}
#endif
