using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

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
    ///https://app.muunship.com/chart/BN-BETABTC?l=5&resolution=1
    ///https://www.tradingview.com/chart/?symbol=BINANCE:BETABTC&interval=1

    public static string GetAltradyRef(CryptoSymbol symbol, CryptoInterval interval)
    {
        string[] AltradyInterval = new[] { "1", "2", "3", "5", "10", "15", "30", "60", "120", "240", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1" };

        ExchangeBase exchangeBase;
        if (GlobalData.Settings.General.ActivateExchange > 0)
            exchangeBase = GetExchangeInstance(GlobalData.Settings.General.ActivateExchange);
        else
            exchangeBase = GetApiInstance();

        string code = exchangeBase.GetAltradyCode();
        string href = string.Format("https://app.altrady.com/d/{0}_{1}_{2}:{3}", 
            code, symbol.Quote, symbol.Base, AltradyInterval[(int)interval.IntervalPeriod]);
        return href;
    }

    public static string GetHyperTraderRef(CryptoSymbol symbol, CryptoInterval interval, bool htmlVersion)
    {
        string[] HypertraderInterval = new[] { "1", "2", "3", "5", "10", "15", "30", "60", "120", "240", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1" };

        ExchangeBase exchangeBase;
        if (GlobalData.Settings.General.ActivateExchange > 0)
            exchangeBase = GetExchangeInstance(GlobalData.Settings.General.ActivateExchange);
        else
            exchangeBase = GetApiInstance();

        string href;
        string code = exchangeBase.GetHyperTraderCode();
        if (htmlVersion)
            href = string.Format("http://www.ccscanner.nl/hypertrader/?e={0}&a={1}&b={2}&i={3}",
                code, symbol.Base, symbol.Quote, HypertraderInterval[(int)interval.IntervalPeriod]);
        else
            href = string.Format("hypertrader://{0}/{1}-{2}/{3}",
                code, symbol.Base, symbol.Quote, HypertraderInterval[(int)interval.IntervalPeriod]);
        return href.ToLower();
    }


    public static string GetTradingViewRef(CryptoSymbol symbol, CryptoInterval interval)
    {
        string[] TradingViewInterval = new[] { "1", "2", "3", "5", "10", "15", "30", "60", "120", "240", "1", "1", "1", "1", "1", "1", "1", "1", "1", "1" };

        ExchangeBase exchangeBase;
        if (GlobalData.Settings.General.ActivateExchange > 0)
            exchangeBase = GetExchangeInstance(GlobalData.Settings.General.ActivateExchange);
        else
            exchangeBase = GetApiInstance();

        string code = exchangeBase.GetTradingViewCode();
        string href = string.Format("https://www.tradingview.com/chart/?symbol={0}:{1}&interval={2}",
            code, symbol.Name, TradingViewInterval[(int)interval.IntervalPeriod]);
        return href;
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
