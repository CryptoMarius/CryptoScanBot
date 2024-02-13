namespace CryptoSbmScanner.Enums;

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

#if EXTRASTRATEGIES
    // Experimental
    Flux, // is er niet meer, blijft bestaan vanwege de enumeratie
    BullishEngulfing, // zwak
    IchimokuKumoBreakout, // nazoeken, in de juiste positieve of negatieve trend doet ie het prima
    
    Wghbm, // Vervallen, uit de Telegram groepen, ziet er leuk uit (maar of het echt wat is moet ik uitzoeken)
    MacdLt, // Vervallen, opgepikt uit de groep van Marco (een ziens of dit inderdaad werkt)
    MacdTest, // Vervallen, Nieuw idee maar dat bevalt  niet

    SlopeEma50, // test
    SlopeSma50, // test
    SlopeEma20, // test
    SlopeSma20, // test

    //// Vervallen
    PriceCrossedSma20, //Vervallen
    PriceCrossedSma50, //Vervallen
    PriceCrossedEma20, //Vervallen
    PriceCrossedEma50, //Vervallen
#endif
}