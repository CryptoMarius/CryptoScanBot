namespace CryptoScanBot.Core.Exchange;

public class ExchangeOptions
{
    // Official exchange name (registered in database)
    public string ExchangeName { get; set; }

    //public Type ApiType { get; set; }

    // Aantal symbols per subscription (een limiet van de exchange)
    public int SubscriptionLimitSymbols { get; set; }

    // Aantal subscriptions per client (een keuze in de techniek)
    public int SubscriptionLimitClient { get; set; } = 10;

    // Reduceer het aantal symbols adhv het volume (indien mogelijk)
    // - Specifiek voor Kucoin vanwege het aantal low volume coins
    public bool LimitAmountOfSymbols { get; set; }
}
