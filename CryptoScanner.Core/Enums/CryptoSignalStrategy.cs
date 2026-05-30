namespace CryptoScanner.Core.Enums;

public enum CryptoSignalStrategy
{
    Jump = 0,

    Sbm1 = 1,
    Sbm2 = 2,
    Sbm3 = 3,
    Stobb = 6,
    StobbMulti = 7,

    //Sma20Sma50 = 4,
    //SlopeSma20 = 8,

    IchimokuKumoBreakout = 9, // nazoeken, in de juiste positieve of negatieve trend doet ie het prima

    StoRsi = 10, // WGHM - STOSCH en RSI momentum indicator
    StoRsiMulti = 11, // WGHM - STOSCH en RSI momentum indicator

    // Combined zone + momentum signals.
    // Trigger when a momentum (storsi/stobb) signal fires while price is at/near a precomputed
    // DLZ or FVG zone. Zones are owned by the dlz.near / fvg algorithms; these classes only read.
    StoRsiDlz = 12,
    StoRsiFvg = 13,
    StobbDlz = 14,
    StobbFvg = 15,

    Nwe = 25,


#if DEBUG
    NweNp = 26,
    NweBb = 27,
    Trend = 31,

    BbmaOmni = 43,
    //Bbma = 44,

    StochDir = 46,

    WaveTrend = 50,
#endif

    DominantLevel = 1000,
    DominantLevelNear = 1001,
    FairValueGap = 1003,

    // SMC supply/demand order block: price returns to a fresh/strong base zone. Zone-based
    // (>= DominantLevel) so it follows the same prepare/execute path as DLZ/FVG.
    // OrderBlock fires on a touch INTO the zone; OrderBlockNear fires earlier, while price is
    // still approaching the proximal edge. Mirrors the DominantLevel / DominantLevelNear pair.
    OrderBlock = 1004,
    OrderBlockNear = 1005,
}