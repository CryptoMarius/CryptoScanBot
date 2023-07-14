using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange;

public abstract class ExchangeBase
{
    public ExchangeBase()
    {
    }

    public abstract void ExchangeDefaults();
    public abstract Task FetchSymbolsAsync();
    public abstract Task FetchCandlesAsync();
#if TRADEBOT
    public abstract Task FetchAssetsAsync(CryptoTradeAccount tradeAccount);
    public abstract Task FetchTradesAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol);
#endif

    // De specifieke code voor de exchange teruggeven
    public abstract string GetAltradyCode();
    public abstract string GetHyperTraderCode();
    public abstract string GetTradingViewCode();
}
