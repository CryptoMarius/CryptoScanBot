namespace CryptoScanBot.Core.Exchange;

public class ExchangeOptions
{
    // Official exchange name (registered in database)
    public required string ExchangeName { get; set; } = "";

    // Aantal symbols per subscription (een limiet van de exchange)
    public int SymbolLimitPerSubscription { get; set; }

    // Aantal subscriptions per client (een keuze in de techniek)
    public int SubscriptionLimitPerClient { get; set; } = 10;

    // Reduceer het aantal symbols adhv het volume (indien mogelijk)
    // - Specificly build for Kucoin because of the amount of symbols
    // - Skip symbol if specified volume if to low (quotedata volume limit)
    public bool LimitAmountOfSymbols { get; set; }

    // Limit for fetching candles
    public int CandleLimit { get; set; } = 1000;
}
