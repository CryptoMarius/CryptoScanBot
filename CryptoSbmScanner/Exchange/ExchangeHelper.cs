using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Settings;

namespace CryptoSbmScanner.Exchange;

public class ExchangeHelper
{

    public static PriceTickerBase PriceTicker { get; set; }
    public static KLineTickerBase KLineTicker { get; set; }
#if TRADEBOT
    public static UserDataBase UserData { get; set; }
#endif


    public static ExchangeBase GetExchangeInstance(int exchangeId)
    {
        // Yup, eventjes hardcoded voor nu, nog eens zien hoe dit verbeterd kan worden
        if (exchangeId == 1)
            return new Binance.Api();
        else if (exchangeId == 2)
            return new BybitSpot.Api();
        else if (exchangeId == 3)
            return new BybitFutures.Api();
        else if (exchangeId == 4)
            return new Kucoin.Api();
        else
            throw new Exception("Niet ondersteunde exchange");
    }

    private static ExchangeBase GetApiInstance()
    {
        return GetExchangeInstance(GlobalData.Settings.General.ExchangeId);
    }

    public static void ExchangeDefaults()
    {
        GetApiInstance().ExchangeDefaults();
    }

    public static async Task FetchSymbols()
    {
        await GetApiInstance().FetchSymbolsAsync();
    }

    public static async Task FetchCandlesAsync()
    {
        await GetApiInstance().FetchCandlesAsync();
    }

    //altrady://market/BINA_BUSD_LOKA:2
    //http://www.ccscanner.nl/hypertrader/?e=binance&a=lto&b=usdt&i=60
    ///hypertrader://binance/BETA-BTC/5
    ///https://app.altrady.com/d/BINA_BTC_BETA:1
    ///https://app.altrady.com/d/BINA_BTC_USDT:2
    ///https://app.muunship.com/chart/BN-BETABTC?l=5&resolution=1
    ///https://www.tradingview.com/chart/?symbol=BINANCE:BETABTC&interval=1

    public static (string Url, bool Execute) GetExternalRef(CryptoExternalUrlApp externalApp, bool telegram, CryptoSymbol symbol, CryptoInterval interval)
    {
        Model.CryptoExchange exchange = symbol.Exchange;
        if (GlobalData.Settings.General.ActivateExchange > 0)
        {
            if (!GlobalData.ExchangeListId.TryGetValue(GlobalData.Settings.General.ActivateExchange, out exchange))
                return ("", false);
        }

        if (GlobalData.Settings.ExternalUrls.TryGetValue(exchange.Name, out CryptoExternalUrls externalUrls))
        {

            CryptoExternalUrl externalUrl = externalApp switch
            {
                CryptoExternalUrlApp.Altrady => externalUrls.Altrady,
                CryptoExternalUrlApp.Hypertrader => externalUrls.HyperTrader,
                CryptoExternalUrlApp.TradingView => externalUrls.TradingView,
                _ => null
            };


            string intervalCode = ((int)(interval.Duration / 60)).ToString();
            bool executeApp = externalUrl.Execute;
            string urlTemplate = externalUrl.Url;
            if (telegram && externalUrl.Telegram != "")
                urlTemplate = externalUrl.Telegram;
            urlTemplate = urlTemplate.Replace("{name}", symbol.Name);
            urlTemplate = urlTemplate.Replace("{base}", symbol.Base);
            urlTemplate = urlTemplate.Replace("{quote}", symbol.Quote);
            urlTemplate = urlTemplate.Replace("{interval}", intervalCode);
            return (urlTemplate, executeApp);
        }

        return ("", false);
    }

#if TRADEBOT
    public static async Task FetchAssetsAsync(CryptoTradeAccount tradeAccount)
    {
        await GetApiInstance().FetchAssetsAsync(tradeAccount);
    }

    public static async Task FetchTradesAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        await GetApiInstance().FetchTradesAsync(tradeAccount, symbol);
    }
#endif

}
