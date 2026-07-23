namespace CryptoScanner.Core.Enums;

// TODO: Remove this enumeration
// Address by name instead of by number
public enum CryptoSignalStrategy
{
    Jump = 0,

    Sbm1 = 1,
    Sbm2 = 2,
    Sbm3 = 3,
    Stobb = 6,
    StobbMulti = 7,
    StoRsi = 10, // WGHM - STOSCH en RSI momentum indicator
    StoRsiMulti = 11, // WGHM - STOSCH en RSI momentum indicator
    Nwe = 25,
    NweNp = 26,
    NweBb = 27,

    // VWAP Bands macro-band hit — long on the lower band, short on the upper band
    // (the same events the chart prints as percentage labels).
    Vbs = 28,
    // AtrRb Bands macro-band hit — long on the lower band, short on the upper band
    // (the same events the chart prints as percentage labels).
    AtrRb = 29,
    // DBR (Donchian Breakout Reversion) macro-band break — long on the lower band,
    // short on the upper band (the same events the chart prints as percentage labels).
    Dbr = 30,

    Trend = 31,

    Bbma = 42,
    BbmaOmni = 43,

    StochDir = 52,
    BbRsiEngulfing = 53,
    IchimokuKumoBreakout = 54,

    // CHoCH (Change of Character) — fires when the ZigZag-derived structure makes a Change
    // of Character on the primary or secondary trend. The .pullback variants additionally
    // wait for an opposite zigzag pivot + breakthrough before allowing the trader to step in.
    ChochPrimary = 60,
    ChochPrimaryPullback = 61,
    ChochSecondary = 62,
    ChochSecondaryPullback = 63,


    DominantLevel = 1000,
    DominantLevelNear = 1001,
    FairValueGap = 1003,

    // SMC supply/demand order block: price returns to a fresh/strong base zone. Zone-based
    // (>= DominantLevel) so it follows the same prepare/execute path as DLZ/FVG.
    // OrderBlock fires on a touch INTO the zone.
    OrderBlock = 1004,
    // Fires on the confirmed rejection: price tested the zone and closed back outside the
    // proximal edge (the actual bounce). This is the entry-grade SMC signal.
    OrderBlockRejection = 1006,
}