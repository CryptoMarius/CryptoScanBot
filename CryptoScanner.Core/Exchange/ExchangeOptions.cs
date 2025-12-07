namespace CryptoScanner.Core.Exchange;

// An experiment for DI..

//public interface IExchangeOptions
//{
//    // Official exchange name (registered in database)
//    public string ExchangeName { get; set; }

//    // The default selected QUOTE
//    public string? DefaultQuote { get; set; }

//    // Aantal symbols per subscription (een limiet van de exchange)
//    public int SymbolLimitPerSubscription { get; set; }

//    // Aantal subscriptions per client (een keuze in de techniek)
//    public int SubscriptionLimitPerClient { get; set; }

//    // Reduce the amount of symbols using the volume (if possible)
//    // - Specificly build for Kucoin because of the amount of symbols
//    // - Skip symbol if specified volume if to low (quotedata volume limit)
//    public bool LimitAmountOfSymbols { get; set; }

//    // Limit for fetching candles
//    public int CandleLimit { get; set; }

//    public void SetDefaultOptions(string exchangeName, string defaultQuote, int candleLimit, bool limitAmountOfSymbols,
//        int symbolLimitPerSubscription, int subscriptionLimitPerClient = 10);
//}

public class ExchangeOptions // : IExchangeOptions
{
    // Official exchange name (registered in database)
    public required string ExchangeName { get; set; } = "?";

    // The default selected QUOTE
    public string? DefaultQuote { get; set; } = null;

    // Aantal symbols per subscription (een limiet van de exchange)
    public int SymbolLimitPerSubscription { get; set; }

    // Aantal subscriptions per client (een keuze in de techniek)
    public int SubscriptionLimitPerClient { get; set; } = 10;

    // Reduce the amount of symbols using the volume (if possible)
    // - Specificly build for Kucoin because of the amount of symbols
    // - Skip symbol if specified volume if to low (quotedata volume limit)
    public bool LimitAmountOfSymbols { get; set; }

    // Limit for fetching candles
    public int CandleLimit { get; set; } = 1000;


    public void SetDefaultOptions(string exchangeName, string defaultQuote, int candleLimit, bool limitAmountOfSymbols, 
        int symbolLimitPerSubscription, int subscriptionLimitPerClient = 10)
    {
        ExchangeName = exchangeName;
        DefaultQuote = defaultQuote;
        CandleLimit = candleLimit;
        LimitAmountOfSymbols = true; // limitAmountOfSymbols; ALWAY's
        SymbolLimitPerSubscription = symbolLimitPerSubscription;
        SubscriptionLimitPerClient = subscriptionLimitPerClient;
    }
}
