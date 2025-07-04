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
    StoRsi2 = 12, // = STORSI, repeated storsi
    StoRsi3 = 13, // = STORSI, 2, but different

    Stoch = 20,

    BbMa = 30, // still studying, complicated Oma Ally strategy

    Trend = 31,

    BbRsiEngulfing = 50,
    SignalSma50Sma20Price = 52,

    DominantLevel = 1000,
    DominantLevelNear = 1001,
    FairValueGap = 1003,
}