using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Settings;


public class CryptoExternalUrlList : SortedList<string, CryptoExternalUrls>
{

    /// <summary>
    /// Defaults for the url's.
    /// The list itself is built by CryptoScanner.Exchanges (ExchangeProvider.InitializeUrls),
    /// because every entry comes from an Api class over there. See ExchangeRegistry.
    /// </summary>
    public void InitializeUrls()
    {
        ExchangeRegistry.InitializeUrls(this);
    }

    public static string GetTradingAppName(CryptoTradingApp tradingApp, string exchangeName)
    {
        string text = tradingApp switch
        {
            CryptoTradingApp.Altrady => $"Altrady {exchangeName}",
            CryptoTradingApp.Hypertrader => $"Hypertrader {exchangeName}",
            CryptoTradingApp.TradingView => $"TradingView {exchangeName}",
            CryptoTradingApp.ExchangeUrl => $"Exchange {exchangeName}",
            _ => "",
        };
        return text;
    }

    //altrady://market/BINA_ETH_LOKA:2
    //http://www.ccscanner.nl/hypertrader/?e=binance&a=lto&b=usdt&i=60
    ///hypertrader://binance/BETA-BTC/5
    ///https://app.altrady.com/d/BINA_BTC_BETA:1
    ///https://app.altrady.com/d/BINA_BTC_USDT:2
    ///https://app.muunship.com/chart/BN-BETABTC?l=5&resolution=1
    ///https://www.tradingview.com/chart/?symbol=BINANCE:BETABTC&interval=1

    public (string Url, CryptoExternalUrlType Execute) GetExternalRef(CryptoTradingApp externalApp, bool telegram, CryptoSymbol symbol, CryptoInterval interval)
    {
        if (!GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ActivateExchangeName, out Model.CryptoExchange? exchange))
            exchange = GlobalData.ActiveExchange;
        return GetExternalRef(exchange!, externalApp, telegram, symbol, interval);
    }


    public bool GetExternalRef(Model.CryptoExchange exchange, out CryptoExternalUrls? externalUrls)
    {
        return TryGetValue(exchange.Name, out externalUrls);
    }

    public (string Url, CryptoExternalUrlType Execute) GetExternalRef(Model.CryptoExchange exchange, CryptoTradingApp externalApp, bool telegram, CryptoSymbol symbol, CryptoInterval interval)
    {
        if (GetExternalRef(exchange, out CryptoExternalUrls? externalUrls0))
        {
            CryptoExternalUrls externalUrls = externalUrls0!;

            CryptoExternalUrl? externalUrl = externalApp switch
            {
                CryptoTradingApp.Altrady => externalUrls.Altrady,
                CryptoTradingApp.Hypertrader => externalUrls.HyperTrader,
                CryptoTradingApp.TradingView => externalUrls.TradingView,
                CryptoTradingApp.ExchangeUrl => externalUrls.ExchangeUrl,
                _ => null
            };

            if (externalUrl == null)
                return ("", CryptoExternalUrlType.Internal);


            string urlTemplate = externalUrl.Url;
            if (telegram && externalUrl.Telegram != null && externalUrl.Telegram != "")
                urlTemplate = externalUrl.Telegram;

            urlTemplate = urlTemplate.Replace("{name}", symbol.Name.ToLower());
            urlTemplate = urlTemplate.Replace("{base}", symbol.Base.ToLower());
            urlTemplate = urlTemplate.Replace("{quote}", symbol.Quote.ToLower());

            urlTemplate = urlTemplate.Replace("{NAME}", symbol.Name.ToUpper());
            urlTemplate = urlTemplate.Replace("{BASE}", symbol.Base.ToUpper());
            urlTemplate = urlTemplate.Replace("{QUOTE}", symbol.Quote.ToUpper());

            // The name the instrument has on the exchange itself, which is not always base + quote:
            // Kraken Futures calls BTCUSD "PF_XBTUSD" and Okx Futures calls it "BTC-USDT-SWAP". Those
            // trade pages cannot be addressed with {BASE} and {QUOTE} alone.
            urlTemplate = urlTemplate.Replace("{exchangename}", symbol.ExchangeName.ToLower());
            urlTemplate = urlTemplate.Replace("{EXCHANGENAME}", symbol.ExchangeName.ToUpper());

            // Interval: amount of minutes
            string intervalCode = ((int)interval.Duration).ToString();
            urlTemplate = urlTemplate.Replace("{interval}", intervalCode.ToLower());
            urlTemplate = urlTemplate.Replace("{INTERVAL}", intervalCode.ToUpper());

            // Interval: name 1h, 2h etc..
            urlTemplate = urlTemplate.Replace("{intervalname}", interval.Name.ToLower());
            urlTemplate = urlTemplate.Replace("{INTERVALNAME}", interval.Name.ToUpper());
            return (urlTemplate, externalUrl.Execute);
        }

        return ("", CryptoExternalUrlType.Internal);
    }

}
