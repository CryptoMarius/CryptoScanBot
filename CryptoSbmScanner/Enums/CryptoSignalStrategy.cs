namespace CryptoSbmScanner.Enums;

// Basis strategie en dan een long en een short variant hierop
public enum CryptoSignalStrategy // CryptoStrategy
{
    Jump, // Alleen informatief?

    Sbm1,
    Sbm2,
    Sbm3,
    Sbm4, // is er niet meer
    Sbm5, // is er niet meer
    Stobb,

    // Experimental
    PriceCrossedSma20,
    PriceCrossedSma50,

    PriceCrossedEma20,
    PriceCrossedEma50,

    SlopeSma50,
    SlopeSma20,

    SlopeEma20,
    SlopeEma50,

    Flux,
    BullishEngulfing,
}