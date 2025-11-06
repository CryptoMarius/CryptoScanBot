using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Settings;


public class CryptoExternalUrlList : SortedList<string, CryptoExternalUrls>
{

    /// <summary>
    /// Defaults for the url's
    /// </summary>
    public void InitializeUrls()
    {
        // This can/should be some kind of service..

        // Altrady: Codes on webpage
        // https://support.altrady.com/en/article/valid-values-for-exchange-and-symbol-1xrzfap/
        // TradingView: Codes are in the symbol description (kind of hidden)

        Remove("Binance");
        this.TryAdd("Binance Spot", Exchange.Binance.Spot.Api.GetExchangeLinks());
        this.TryAdd("Binance Futures", Exchange.Binance.Futures.Api.GetExchangeLinks());

        this.TryAdd("BloFin Futures", Exchange.BloFin.Futures.Api.GetExchangeLinks());

        Remove("Bybit");
        this.TryAdd("Bybit Spot", Exchange.BybitApi.Spot.Api.GetExchangeLinks());
        this.TryAdd("Bybit Futures", Exchange.BybitApi.Futures.Api.GetExchangeLinks());

        Remove("Bybit EU");
        this.TryAdd("Bybit EU Spot", Exchange.BybitEu.Spot.Api.GetExchangeLinks());
        this.TryAdd("Bybit EU Futures", Exchange.BybitEu.Futures.Api.GetExchangeLinks());

        Remove("BitMart");
        this.TryAdd("BitMart Spot", Exchange.BitMart.Spot.Api.GetExchangeLinks());
        this.TryAdd("BitMart Futures", Exchange.BitMart.Futures.Api.GetExchangeLinks());

        Remove("BloFin");
        //this.TryAdd("BloFin Spot", Exchange.BloFin.Spot.Api.GetExchangeLinks());
        this.TryAdd("BloFin Futures", Exchange.BloFin.Futures.Api.GetExchangeLinks());

        Remove("Coinbase");
        this.TryAdd("Coinbase Spot", Exchange.Coinbase.Spot.Api.GetExchangeLinks());
        //this.TryAdd("Bybit EU Futures", Exchange.BybitEu.Futures.Api.GetExchangeLinks());

        Remove("HyperLiquid");
        this.TryAdd("HyperLiquid Spot", Exchange.HyperLiquid.Spot.Api.GetExchangeLinks());
        this.TryAdd("HyperLiquid Futures", Exchange.HyperLiquid.Futures.Api.GetExchangeLinks());

        Remove("Kucoin");
        this.TryAdd("Kucoin Spot", Exchange.Kucoin.Spot.Api.GetExchangeLinks());
        this.TryAdd("Kucoin Futures", Exchange.Kucoin.Futures.Api.GetExchangeLinks());

        Remove("Kraken");
        this.TryAdd("Kraken Spot", Exchange.Kraken.Spot.Api.GetExchangeLinks());
        this.TryAdd("Kraken Futures", Exchange.Kraken.Futures.Api.GetExchangeLinks());

        Remove("Mexc");
        this.TryAdd("Mexc Spot", Exchange.Mexc.Spot.Api.GetExchangeLinks());

        Remove("Okx");
        this.TryAdd("Okx Spot", Exchange.Okx.Spot.Api.GetExchangeLinks());
        this.TryAdd("Okx Futures", Exchange.Okx.Futures.Api.GetExchangeLinks());

        Remove("Coinbase");
        this.TryAdd("Coinbase Spot", Exchange.Coinbase.Spot.Api.GetExchangeLinks());
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

            // Interval: amount of minutes
            string intervalCode = ((int)(interval.Duration / 60)).ToString();
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
