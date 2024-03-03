namespace CryptoScanBot.Enums;

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
