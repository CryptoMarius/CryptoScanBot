namespace CryptoScanner.Core.Exchange;

/// <summary>
/// Describes how an exchange delivers closed 1-minute candles over its WebSocket feed.
/// Used as documentation and for diagnostics; the actual implementation choice is expressed
/// by inheriting from <see cref="SubscriptionKLineCachedTicker"/> (Timer) or
/// <see cref="Subscription"/> directly (FinalEvent).
/// </summary>
public enum KlineDelivery
{
    /// Exchange sends a single definitive "final" event once the candle closes (Binance, BloFin,
    /// ByBit, OKX, Kucoin, …). No local cache or timer is needed.
    FinalEvent,

    /// Exchange sends continuous partial updates for the currently open candle and never sends a
    /// definitive "closed" signal (HyperLiquid, Kraken Futures). A local cache and a minute-boundary
    /// timer are required to extract the completed candle.
    TimerFlush,
}

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
//    public int SubscriptionsPerBundle { get; set; }

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
    // Every bundle owns one socket client, so this also decides how many socket clients are created.
    public int SubscriptionsPerBundle { get; set; } = 10;

    // Reduce the amount of symbols using the volume (if possible)
    // - Specificly build for Kucoin because of the amount of symbols
    // - Skip symbol if specified volume if to low (quotedata volume limit)
    public bool LimitAmountOfSymbols { get; set; }

    // Limit for fetching candles
    public int CandleLimit { get; set; } = 1000;

    // How the exchange delivers completed klines over its WebSocket feed.
    public KlineDelivery KlineDelivery { get; set; } = KlineDelivery.FinalEvent;


    public void SetDefaultOptions(string exchangeName, string defaultQuote, int candleLimit, bool limitAmountOfSymbols,
        int symbolLimitPerSubscription, int subscriptionsPerBundle = 10,
        KlineDelivery klineDelivery = KlineDelivery.FinalEvent)
    {
        ExchangeName = exchangeName;
        DefaultQuote = defaultQuote;
        CandleLimit = candleLimit;
        LimitAmountOfSymbols = true; // limitAmountOfSymbols; ALWAY's
        SymbolLimitPerSubscription = symbolLimitPerSubscription;
        SubscriptionsPerBundle = subscriptionsPerBundle;
        KlineDelivery = klineDelivery;
    }
}
