namespace CryptoScanBot.Core.Enums;

public enum CryptoSignalStrategy
{
    Jump = 0,

    Sbm1 = 1,
    Sbm2 = 2,
    Sbm3 = 3,
    Stobb = 6,
    StobbMulti = 7,

    IchimokuKumoBreakout = 9, // nazoeken, in de juiste positieve of negatieve trend doet ie het prima

    StoRsi = 10, // WGHM - STOSCH en RSI momentum indicator
    StoRsiMulti = 11, // WGHM - STOSCH en RSI momentum indicator

    Stoch = 20,
    Macd = 21, // experiment

    NadarayaWatsonEnvelope = 25,
    NadarayaWatsonEnvelopeCross = 27,

    BbMa = 30, // still studying, its quite complicated that Oma Ally strategy

    Trend = 31,

    BbRsiEngulfing = 50,
    SignalSma50Sma20Price = 52,

    DominantLevel = 1000,
    DominantLevelNear = 1001,
    FairValueGap = 1003,
}