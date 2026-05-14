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

    NadarayaWatsonEnvelope = 25,
    NadarayaWatsonEnvelopeNp = 26,


#if DEBUG
    NweBb = 27,
    Trend = 31,

    TrendBosChoch = 32,

    Box = 33,

    BbmaOmni = 43,
    Bbma = 44,

    StochDir = 46,
#endif

    DominantLevel = 1000,
    DominantLevelNear = 1001,
    FairValueGap = 1003,
}