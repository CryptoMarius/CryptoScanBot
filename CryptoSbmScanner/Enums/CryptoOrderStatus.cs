namespace CryptoSbmScanner.Enums;

// De status van een order (c.q. step)
public enum CryptoOrderStatus
{
    New,
    PartiallyFilled,
    Filled,
    Canceled,
    Expired
}
