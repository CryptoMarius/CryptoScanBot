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
    //Macd = 21, // experiment
#if DEBUG
    SmaDist = 22, // experiment
#endif

    NadarayaWatsonEnvelope = 25,
#if DEBUG
    NadarayaWatsonEnvelopeCross = 27,
    NadarayaWatsonEnvelopePull = 28,
#endif

#if DEBUG
    BbMa = 30, // still studying, its quite complicated that Oma Ally strategy
#endif

#if DEBUG
    Trend = 31,
#endif

#if DEBUG
    EmaCrossedSma20 = 32,
#endif
#if DEBUG
    Ema9KcBand = 33,
#endif
#if DEBUG
    Ema9KcCenter = 34,
#endif  
#if DEBUG
    TemaCrossedKcBand = 35,
#endif  
#if DEBUG
    TemaCrossedKcCenter = 36,
#endif  



    //BbRsiEngulfing = 50,
    //SignalSma50Sma20Price = 52,

    DominantLevel = 1000,
    DominantLevelNear = 1001,
    FairValueGap = 1003,
}