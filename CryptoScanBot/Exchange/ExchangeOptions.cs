namespace CryptoScanBot.Exchange;

public class ExchangeOptions
{
    // Official exchange name (registered in database)
    public string ExchangeName { get; set; }

    //public Type ApiType { get; set; }

    // Exchange subscription limit per client (sessie)
    public int SubscriptionLimit { get; set; }

    // Reduceer het aantal suymbols adhv het volume (indien mogelijk)
    public bool LimitAmountOfSymbols {get; set; }

    // De 3 verschillende tickers
    public Type KLineTickerItemType { get; set; }
    public Type PriceTickerItemType { get; set; }
    public Type UserTickerItemType { get; set; }
}
