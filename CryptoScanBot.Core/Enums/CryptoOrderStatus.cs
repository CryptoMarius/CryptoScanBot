namespace CryptoScanBot.Core.Enums;

// De status van een order (c.q. step)
public enum CryptoOrderStatus
{
    New,
    PartiallyFilled,
    PartiallyAndClosed,
    Filled,
    // Alles hierna is een verklaring waarom de order geannuleerd is
    Canceled,
    Expired,
    Timeout,
    BarameterToLow,
    TradingRules,
    PositionClosed,
    ChangedSettings,
    ChangedBreakEven,
    JoJoSell,
    TrailingChange,
    ManuallyByUser
}

public static class OrderHelper
{
    public static string ToText(this CryptoOrderStatus status)
    {
        // The text "PartiallyAndClosed" is actually filled with an additional an internal purpose
        if (status == CryptoOrderStatus.PartiallyAndClosed)
            return "Filled";
        else
            return status.ToString();
    }
}

