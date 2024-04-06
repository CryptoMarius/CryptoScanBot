namespace CryptoScanBot.Exchange;

public class ExchangeOptions
{
    // Official exchange name (registered in database)
    public string ExchangeName { get; set; }

    public Type ApiType { get; set; }

    // Aantal symbols per subscription (een limiet van de exchange)
    public int SubscriptionLimitSymbols { get; set; }

    // Aantal subscriptions per client (een keuze in de techniek)
    public int SubscriptionLimitClient { get; set; } = 10;

    // Reduceer het aantal suymbols adhv het volume (indien mogelijk)
    public bool LimitAmountOfSymbols {get; set; }

    // De 3 verschillende tickers
    //public Type KLineTickerItemType { get; set; }
    //public Type PriceTickerItemType { get; set; }
    //public Type UserTickerItemType { get; set; }
}
