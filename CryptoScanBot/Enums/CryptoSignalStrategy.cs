namespace CryptoScanBot.Enums;

// Basis strategie en dan een long en een short variant hierop
public enum CryptoSignalStrategy // CryptoStrategy
{
    Jump, // Alleen informatief?

    Sbm1,
    Sbm2,
    Sbm3,
    Sbm4, // is er niet meer, blijft bestaan vanwege de enumeratie
    Sbm5, // is er niet meer, blijft bestaan vanwege de enumeratie
    Stobb,

    // Experimental
    Flux, // is er niet meer, blijft bestaan vanwege de enumeratie
    BullishEngulfing, // zwak
    IchimokuKumoBreakout, // nazoeken, in de juiste positieve of negatieve trend doet ie het prima
    
    Wghm, // Vervallen, uit de Telegram groepen, ziet er leuk uit (maar of het echt wat is moet ik uitzoeken)
#if EXTRASTRATEGIES
    MacdLt, // Vervallen, opgepikt uit de groep van Marco (een ziens of dit inderdaad werkt)
    MacdTest, // Vervallen, Nieuw idee maar dat bevalt  niet

    SlopeEma50,
    SlopeSma50,
    SlopeEma20,
    SlopeSma20,

    //// Vervallen?
    PriceCrossedSma20,
    PriceCrossedSma50,
    PriceCrossedEma20,
    PriceCrossedEma50,
    EmaCross926,
#endif
}