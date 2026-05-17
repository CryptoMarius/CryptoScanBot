#if DEBUG
namespace CryptoScanner.Core.Signal.Bbma;

/// <summary>
/// Base class for the BBMA Omni strategy. State classification (Extreme, CSD, CSM, MLV,
/// Reentry) is a direct port of the "BBMA Oma Ally OmniView.mq5" formulas — kept as close
/// to the MQL5 source as possible so we can cross-reference. The multi-timeframe setup
/// (GetIntervals, TfStateCode for the code-match string) is inherited from SignalBbmaBase.
///
/// What is intentionally NOT ported from the OmniView source:
///   - CSAK2  : "still trending" continuation marker, not a fresh trigger event.
///   - TPW    : requires persistent state between bars (tpwbuy/tpwsell flags).
///   - MHV-as-fractal : the fractal pivot in OmniView is plotted at i-1 once i is known;
///                      we approximate MLV statelessly (wick rejection + WMA inside band).
///   - All chart-only signals (CSAA, CrossEMA50mBB, RejectedEMA50, GAPBBtoEMA50, etc.).
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
        Extreme,  // ext_buy / ext_sell : MA poke + wick rejection at outer BB
        Csd,      // csak_buy / csak_sell : bullish/bearish cross of BB middle + beyond WMA5/10
        Csm,      // mmt_buy / mmt_sell : close beyond outer BB (no Extreme)
        Mlv,      // MHV stateless approximation: wick rejection with WMA still inside band
        Reentry,  // ret_buy / ret_sell : pullback to WMA zone, close on correct side of mid
    }

    /// <summary>
    /// Maps an OmniState to the same single-letter code used by the existing BBMA system,
    /// so the multi-TF code-match strings remain "RRE" / "REM" / "REE" / "RME".
    /// CSD and CSM are local trigger events on HTF — they do not appear in the code-match
    /// (which only encodes Reentry / Extreme / MLV) so they map to "-".
    /// </summary>
    internal static string OmniStateCode(OmniState state) => state switch
    {
        OmniState.Extreme => "E",
        OmniState.Mlv => "M",
        OmniState.Reentry => "R",
        _ => "-"
    };
}
#endif
