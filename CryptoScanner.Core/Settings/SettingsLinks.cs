using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;

using System.Globalization;

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

    /// <summary>
    /// The expiry in the tail of an instrument name, for the addresses that need it. Altrady puts it
    /// at the end of its own name for an Okx X-Perp: "APR-USD_UM_XPERP-310815" is APR_UM-XPERP-310815
    /// there, so the link cannot be built from base and quote alone.
    /// <para>
    /// Only a tail of digits counts. The last part of "BTC-USDT-SWAP" is SWAP and that is not an
    /// expiry, so an instrument without one answers with an empty string.
    /// </para>
    /// </summary>
    internal static string ExpiryOf(string exchangeName)
    {
        int dash = exchangeName.LastIndexOf('-');
        if (dash <= 0 || dash >= exchangeName.Length - 1)
            return "";

        string tail = exchangeName[(dash + 1)..];
        return tail.All(char.IsDigit) ? tail : "";
    }

    /// <summary>
    /// The futures month codes, January to December: the letter a futures chart puts behind the
    /// root of a contract. The same alphabet everywhere, so TradingView uses it for Okx as well.
    /// </summary>
    private const string FuturesMonthCodes = "FGHJKMNQUVXZ";

    /// <summary>
    /// An expiry (yyMMdd, the tail of an Okx instrument name: 310704) as a futures chart spells a
    /// contract: the month letter followed by the four-digit year, N2031.
    /// <para>
    /// TradingView needs this for an Okx X-Perp. Its symbol search lists BTCUSD.UM and ACTUSD.UM,
    /// but those are the FAMILY of a contract and open an empty chart ("This symbol doesn't
    /// exist"); the contract itself is ACTUSD.UMN2031, checked on the chart on 04-09-2026, after
    /// every X-Perp link had opened nothing since 28-08-2026. The continuous spelling with "1!"
    /// that other futures roots take does not exist for these either.
    /// </para>
    /// An expiry that is not six digits, or whose month is not 1 to 12, answers with an empty
    /// string rather than a guess.
    /// </summary>
    internal static string ExpiryCodeOf(string expiry)
    {
        if (expiry.Length != 6 || !expiry.All(char.IsDigit))
            return "";

        int month = int.Parse(expiry.Substring(2, 2), CultureInfo.InvariantCulture);
        if (month < 1 || month > 12)
            return "";

        return $"{FuturesMonthCodes[month - 1]}20{expiry[..2]}";
    }


    public (string Url, CryptoExternalUrlType Execute) GetExternalRef(Model.CryptoExchange exchange, CryptoTradingApp externalApp, bool telegram, CryptoSymbol symbol, CryptoInterval interval)
    {
        if (GetExternalRef(exchange, out CryptoExternalUrls? externalUrls0))
        {
            CryptoExternalUrls externalUrls = externalUrls0!;

            static CryptoExternalUrl? Pick(CryptoExternalUrls urls, CryptoTradingApp app) => app switch
            {
                CryptoTradingApp.Altrady => urls.Altrady,
                CryptoTradingApp.Hypertrader => urls.HyperTrader,
                CryptoTradingApp.TradingView => urls.TradingView,
                CryptoTradingApp.ExchangeUrl => urls.ExchangeUrl,
                _ => null
            };

            // A market that carries several products can state an address for one of them, because
            // the outside world does not name them the same way: TradingView calls an Okx swap
            // BTCUSDT.P and an X-Perp BTCUSD.UM. Anything the override leaves out falls back to the
            // address of the market itself.
            CryptoExternalUrl? externalUrl = null;
            // The product decides which addresses apply, unless the market reads a different one
            // out of the instrument itself (see CryptoExternalUrls.LinkProductOf).
            string linkProduct = externalUrls.LinkProductOf?.Invoke(symbol) ?? symbol.Product;
            if (linkProduct.Length > 0 && externalUrls.PerProduct.TryGetValue(linkProduct, out CryptoExternalUrls? perProduct))
                externalUrl = Pick(perProduct, externalApp);
            // A market an outside party deployed, without an entry of its own: the one template for
            // all of them (see CryptoExternalUrls.Deployed). Never for a product of ours, so a
            // perpetual keeps the address of the market itself.
            if (externalUrl == null && externalUrls.Deployed != null
                && symbol.Product.Length > 0 && !CryptoProduct.IsReserved(symbol.Product))
                externalUrl = Pick(externalUrls.Deployed, externalApp);
            externalUrl ??= Pick(externalUrls, externalApp);

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

            string expiry = ExpiryOf(symbol.ExchangeName);
            urlTemplate = urlTemplate.Replace("{expiry}", expiry);
            urlTemplate = urlTemplate.Replace("{EXPIRY}", expiry);

            // The same expiry the way a futures chart spells it: month letter plus year (N2031).
            string expiryCode = ExpiryCodeOf(expiry);
            urlTemplate = urlTemplate.Replace("{expirycode}", expiryCode.ToLower());
            urlTemplate = urlTemplate.Replace("{EXPIRYCODE}", expiryCode);

            // Which instrument this is - the part of the name behind the dot. PERP, SPOT, XPERP, or
            // the market an outside party deployed (HYNA). Empty for a barometer symbol.
            urlTemplate = urlTemplate.Replace("{product}", symbol.Product.ToLower());
            urlTemplate = urlTemplate.Replace("{PRODUCT}", symbol.Product.ToUpper());

            // The name the instrument has on the exchange itself, which is not always base + quote:
            // Kraken Perpetual calls BTCUSD "PF_XBTUSD" and Okx Perpetual calls it "BTC-USDT-SWAP". Those
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
