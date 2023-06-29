namespace CryptoSbmScanner.Enums;

// De ondersteunde types (alleen Binance heeft OCO)
public enum CryptoOrderType
{
    Market,             // Het "beste" bod van de markt
    Limit,              // Een standaard order
    StopLimit,          // Een stoplimit order
    Oco                 // OCO's alleen op Binance
}
