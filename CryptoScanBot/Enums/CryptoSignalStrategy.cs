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
    
    StoRsi, // WGHM - STOSCH en RSI momentum indicator
#if EXTRASTRATEGIESSLOPEEMA
    SlopeEma50,
    SlopeEma20,
#endif
#if EXTRASTRATEGIESSLOPESMA
    SlopeSma20,
    SlopeSma50,
#endif
#if EXTRASTRATEGIES
    MacdLt, // Vervallen, opgepikt uit de groep van Marco (een ziens of dit inderdaad werkt)
    MacdTest, // Vervallen, Nieuw idee maar dat bevalt  niet


    //// Vervallen?
    PriceCrossedSma20,
    PriceCrossedSma50,
    PriceCrossedEma20,
    PriceCrossedEma50,
    EmaCross926,
#endif
}