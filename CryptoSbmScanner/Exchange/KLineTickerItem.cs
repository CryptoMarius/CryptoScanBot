using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange;

// todo: restant implementeren (inheritance)

public abstract class KLineTickerItem
{
    public int TickerCount = 0;
    public int ConnectionLostCount = 0;
    public string QuoteDataName;
    public List<string> symbols = new();

    public KLineTickerItem(CryptoQuoteData quoteData)
    {
        QuoteDataName = quoteData.Name;
    }

    public abstract Task StartAsync();
    public abstract Task StopAsync();
}