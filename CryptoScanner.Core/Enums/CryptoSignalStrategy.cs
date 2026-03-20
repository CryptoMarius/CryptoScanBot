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

#if DEBUG
    Stoch = 20,
#endif


    NadarayaWatsonEnvelope = 25,
#if DEBUG
    NadarayaWatsonEnvelopePull = 28,
#endif

#if DEBUG
    BbMa = 30, // still studying, its complicated
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
    BbmaReentryOld = 42,
#endif
#if DEBUG
    BbmaReentryNew = 43,
#endif

#if DEBUG
    // BB wick + SMA20 slope + SMA50 cross reversal signal
    BbWickSma = 44,
#endif

#if DEBUG
    // BBMA Magic Extreme: WMA5(Low/High) AND WMA10(Low/High) both outside the Bollinger Band
    BbmaMagicExtreme = 45,
#endif

    DominantLevel = 1000,
    DominantLevelNear = 1001,
    FairValueGap = 1003,
}