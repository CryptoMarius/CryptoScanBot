namespace CryptoScanner.Core.Enums;

public enum CryptoSignalStrategy
{
    Jump = 0,

    Sbm1 = 1,
    Sbm2 = 2,
    Sbm3 = 3,
    Stobb = 6,

    //Sma20Sma50 = 4,
    //SlopeSma20 = 8,

    IchimokuKumoBreakout = 9, // nazoeken, in de juiste positieve of negatieve trend doet ie het prima

    StoRsi = 10, // WGHM - STOSCH en RSI momentum indicator

    // Combined zone + momentum signals.
    // Trigger when a momentum (storsi/stobb) signal fires while price is at/near a precomputed
    // DLZ or FVG zone. Zones are owned by the dlz.near / fvg algorithms; these classes only read.
    StoRsiDlz = 12,
    StoRsiFvg = 13,
    StoRsiSmc = 16,
    StoRsiMulti = 11, // WGHM - STOSCH en RSI momentum indicator
    StoRsiMultiDlz = 20,
    StoRsiMultiFvg = 21,
    StoRsiMultiSmc = 18,

    StobbDlz = 14,
    StobbFvg = 15,
    StobbSmc = 17,
    StobbMulti = 7,
    StobbMultiDlz = 22,
    StobbMultiFvg = 23,
    StobbMultiSmc = 19,

    // Unified ".zone" variants — fire when the base storsi/stobb signal hits AND price was
    // rejected at ANY active zone type (DLZ or FVG or SMC). Consolidates the .dlz/.fvg/.smc
    // permutations into a single strategy name per base so the user's strategy list stays
    // short, while the per-zone variants remain available for debugging / A-B comparisons.
    StoRsiZone = 24,
    StoRsiMultiZone = 28,
    StobbZone = 29,
    StobbMultiZone = 30,

    Nwe = 25,


#if DEBUG
    NweNp = 26,
    NweBb = 27,
    Trend = 31,

    BbmaOmni = 43,

    WaveTrend = 50,
#endif

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