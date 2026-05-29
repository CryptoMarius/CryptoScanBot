namespace CryptoScanner.Core.Enums;

public enum CryptoZoneKind
{
    DominantLevel = 1, // DLZ Dominant Liquidity Zone
    FairValueGap = 2, // FVG Fair Value Gap Zone
    OrderBlock = 3,   // SMC Order Block — last opposite-direction candle before a structure break
}