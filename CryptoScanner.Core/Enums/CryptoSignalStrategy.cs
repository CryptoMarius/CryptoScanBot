namespace CryptoScanner.Core.Enums;

public enum CryptoSignalStrategy
{
    Jump = 0,

    Sbm1 = 1,
    Sbm2 = 2,
    Sbm3 = 3,
    Stobb = 6,
    StobbMulti = 7,

    Sma20Sma50 = 4,
    //SlopeSma20 = 8,

    IchimokuKumoBreakout = 9, // nazoeken, in de juiste positieve of negatieve trend doet ie het prima

    StoRsi = 10, // WGHM - STOSCH en RSI momentum indicator
    StoRsiMulti = 11, // WGHM - STOSCH en RSI momentum indicator

    NadarayaWatsonEnvelope = 25,
#if DEBUG
    NadarayaWatsonEnvelopePull = 28,
#endif

#if DEBUG
    BbMaGrok = 30, // still studying, its complicated // Confirmations from higher timeframe(s)
#endif

#if DEBUG
    Trend = 31,
#endif

#if DEBUG
    RollingFft = 37,
#endif
#if DEBUG
    RsiDivergence = 38,
#endif

#if DEBUG
    BbmaReentryOld = 42, // No confirmations from higher timeframe(s)
#endif
#if DEBUG
    BbmaReentryNew1 = 43, // Confirmations from higher timeframe(s)
    BbmaReentryNew2 = 44, // Confirmations from higher timeframe(s)
#endif

#if DEBUG
    StochDir = 46,
#endif

    DominantLevel = 1000,
    DominantLevelNear = 1001,
    FairValueGap = 1003,
}