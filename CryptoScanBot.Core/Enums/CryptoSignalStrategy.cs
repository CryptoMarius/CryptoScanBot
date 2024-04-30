namespace CryptoScanBot.Core.Enums;

// Basis strategie en dan een long en een short variant hierop
public enum CryptoSignalStrategy // CryptoStrategy
{
    Jump = 0, // Alleen informatief?

    Sbm1 = 1,
    Sbm2 = 2,
    Sbm3 = 3,
    Sbm4 = 4, // is er niet meer, blijft bestaan vanwege de enumeratie
    Sbm5 = 5, // is er niet meer, blijft bestaan vanwege de enumeratie
    Stobb = 6,

    // Experimental
    Flux = 7, // is er niet meer, blijft bestaan vanwege de enumeratie
    BullishEngulfing = 8, // zwak
    IchimokuKumoBreakout = 9, // nazoeken, in de juiste positieve of negatieve trend doet ie het prima

    StoRsi, // WGHM - STOSCH en RSI momentum indicator
#if EXTRASTRATEGIESSLOPEEMA
    SlopeEma50 = 10,
    SlopeEma20 = 11,
#endif
#if EXTRASTRATEGIESSLOPESMA
    SlopeSma20 = 12,
    SlopeSma50 = 13,
#endif
#if EXTRASTRATEGIESSLOPEKELTNER
    SlopeKeltner = 14,
#endif
#if EXTRASTRATEGIESPSARRSI
    PSarRsi = 15,
#endif
#if EXTRASTRATEGIESDPO
    Dpo = 16,
#endif
#if EXTRASTRATEGIESFISHER
    Fisher = 17,
#endif

#if EXTRASTRATEGIES
    MacdLt = 18, // Vervallen, opgepikt uit de groep van Marco (een ziens of dit inderdaad werkt)
    MacdTest = 19, // Vervallen, Nieuw idee maar dat bevalt  niet


    // Alleen relevant in een sterke up- of downtrend (vervallen?
    PriceCrossedSma20 = 20,
    PriceCrossedSma50 = 21,
    PriceCrossedEma20 = 22,
    PriceCrossedEma50 = 23,
    EmaCross926 = 24,
#endif
}