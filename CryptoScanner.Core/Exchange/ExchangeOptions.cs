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
    /// ByBit, OKX, …). No local cache or timer is needed.
    FinalEvent,

    /// Exchange sends continuous partial updates for the currently open candle and never sends a
    /// definitive "closed" signal (HyperLiquid, Kraken Futures, Kucoin). A local cache and a
    /// minute-boundary timer are required to extract the completed candle.
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
    // Fallback for exchanges that do not state a boundary of their own. This is the value that used
    // to be hardcoded in ScannerSession, so leaving an exchange on this default changes nothing.
    // Since the sources of BloFin Spot and Bybit EU Futures were removed on 17-08-2026, every market
    // that is left states a boundary of its own, so nothing uses this value any more. Alpaca states a
    // boundary of zero: it picks its symbols itself and the volume it reports is the volume of one feed.
    // Whoever adds a market has to measure a boundary for it first, the way the comment above every
    // other SetDefaultOptions call describes.
    public const double DefaultMinimalVolume = 4_500_000;

    // Official exchange name (registered in database)
    public required string ExchangeName { get; set; } = "?";

    // The default selected QUOTE
    public string? DefaultQuote { get; set; } = null;

    // The 24 hour quote volume boundary that a NEW quote coin starts with, in the quote currency.
    // Every exchange has its own scale - a floor that is reasonable on Binance leaves nothing at all
    // on HyperLiquid - so the value belongs with the exchange, not in one global constant.
    //
    // The value each exchange states is 0.034% of everything that exchange trades in its default quote
    // over 24 hours, measured on 14-08-2026 and rounded to two digits. That fraction is calibrated on
    // Binance Futures, where 15 million is the boundary in daily use. Scaling it to the size of the
    // exchange is what makes the boundary comparable: it leaves roughly 100 to 140 symbols standing on
    // the large exchanges, where one flat number left 240 on Binance Futures and 1 on Bybit EU Spot.
    // Remeasuring is a manual job (see the volume comment above each SetDefaultOptions call); the
    // numbers age slowly because they follow the whole exchange, not an individual coin.
    //
    // It is only used to initialise DefaultQuote the first time it is seen (a new database or a quote
    // that is not in the settings yet); after that the value in the settings wins and the user is free
    // to change it.
    public double MinimalVolume { get; set; } = DefaultMinimalVolume;

    // Scanner name of the coin the pause trading rules watch - bitcoin against the default quote, the
    // coin the rest of the market follows. Also exchange specific: it is BTCUSDT on Binance, BTCUSD on
    // Kraken, XBTUSDC on Kucoin Futures and UBTCUSDC on HyperLiquid Spot, where the same rule with
    // "BTCUSDT" in it silently does nothing and only logs "symbol does not exist" every minute.
    // Used to fill in a pause rule that has no symbol of its own (see ScannerSession).
    public string PauseSymbol { get; set; } = "";

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
        KlineDelivery klineDelivery = KlineDelivery.FinalEvent,
        double minimalVolume = DefaultMinimalVolume,
        string? pauseSymbol = null)
    {
        ExchangeName = exchangeName;
        DefaultQuote = defaultQuote;
        CandleLimit = candleLimit;
        LimitAmountOfSymbols = true; // limitAmountOfSymbols; ALWAY's
        SymbolLimitPerSubscription = symbolLimitPerSubscription;
        SubscriptionsPerBundle = subscriptionsPerBundle;
        KlineDelivery = klineDelivery;
        MinimalVolume = minimalVolume;
        // Most exchanges simply call it BTC plus the quote, so only the exceptions have to say so
        PauseSymbol = pauseSymbol ?? "BTC" + defaultQuote;
    }
}
