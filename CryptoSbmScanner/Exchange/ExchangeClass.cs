using CryptoSbmScanner.Exchange.Binance;
using CryptoSbmScanner.Exchange.Bybit;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Exchange;

/*
 * 
https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-servertime
https://api-testnet.bybit.com/v2/public/time
{"ret_code":0,"ret_msg":"OK","result":{},"ext_code":"","ext_info":"","time_now":"1688116858.760925"}

https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-announcement
https://api-testnet.bybit.com/v2/public/announcement
{"ret_code":0,"ret_msg":"OK","result":[],"ext_code":"","ext_info":"","time_now":"1688116961.886013"}
(dat lijkt nogal op die eerste..)


https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-querykline
https://api-testnet.bybit.com/v2/public/kline/list
{"retCode":10001,"retMsg":"The requested symbol is invalid.","result":{},"retExtInfo":{},"time":1688117090806}
https://api-testnet.bybit.com/v2/public/kline/list?symbol=BTCUSDT&interval=1


https://bybit-exchange.github.io/docs-legacy/futuresV2/inverse/#t-querysymbol
https://api-testnet.bybit.com/spot/v3/public/symbols
(denk om de versie verschillen)

 */


/// <summary>
/// Eerste poging om een tweede Exchange te ondersteunen
/// (kan vast beter dan dit, maar voorlopig even dit)
/// inheritance (op voorwaarde dat alles hetzelfde uitpakt)
/// </summary>
public static class ExchangeClass
{
    public static void SetExchangeDefaults()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            BinanceApi.SetExchangeDefaults();
        else
            BybitApi.SetExchangeDefaults();
    }
    

    public static async Task FetchSymbols()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            await Task.Run(async () => { await BinanceFetchSymbols.ExecuteAsync(); });
        else
            await Task.Run(async () => { await BybitFetchSymbols.ExecuteAsync(); });
    }


    public static async Task FetchCandles()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            await Task.Run(async () => { await BinanceFetchCandles.ExecuteAsync(); });
        else
            await Task.Run(async () => { await BybitFetchCandles.ExecuteAsync(); });

    }


    public static void StartPriceTickerStream()
    {
        // De (uitgebreide) price ticker voor laatste prijs, bied prijs, vraag prijs, volume enzovoort
        if (GlobalData.Settings.General.ExchangeId == 1)
            BinanceApi.StartPriceTicker();
        else
            BybitApi.StartPriceTicker();
    }
    public static async Task StopPriceTickerStream()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            await BinanceApi.StopPriceTicker();
        else
            await BybitApi.StopPriceTicker();
    }
    public static void ResetPriceTickerStream()
    {
        // De (uitgebreide) price ticker voor laatste prijs, bied prijs, vraag prijs, volume enzovoort
        if (GlobalData.Settings.General.ExchangeId == 1)
            BinanceApi.ResetPriceTickerStream();
        else
            BybitApi.ResetPriceTickerStream();
    }
    public static int CountPriceTickerStream()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            return BinanceApi.CountPriceTickerStream();
        else
            return BybitApi.CountPriceTickerStream();
    }


    public static async Task Start1mCandleStream()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            await BinanceApi.Start1mCandleStream();
        else
            await BybitApi.Start1mCandleStream();
    }
    public static async Task Stop1mCandleStream()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            await BinanceApi.Stop1mCandleStream();
        else
            await BybitApi.Stop1mCandleStream();
    }
    public static void Reset1mCandleStream()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            BinanceApi.Reset1mCandleStream();
        else
            BybitApi.Reset1mCandleStream();
    }
    public static int Count1mCandleStream()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            return BinanceApi.Count1mCandleStream();
        else
            return BybitApi.Count1mCandleStream();
    }



    public static void StartUserDataStream()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            BinanceApi.StartUserDataStream();
        else
            BybitApi.StartUserDataStream();
    }
    public static async Task StopUserDataStream()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            await BinanceApi.StopUserDataStream();
        else
            await BybitApi.StopUserDataStream();
    }
    public static void ResetUserDataStream()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            BinanceApi.ResetUserDataStream();
        else
            BybitApi.ResetUserDataStream();
    }


    public static async Task FetchAssets(CryptoTradeAccount tradeAccount)
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            await BinanceApi.FetchAssets(tradeAccount);
        else
            await BybitApi.FetchAssets(tradeAccount);
    }

    public static async Task FetchTradesForSymbol(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            await Task.Run(async () => { await BinanceFetchTrades.FetchTradesForSymbol(tradeAccount, symbol); });
        else
            await Task.Run(async () => { await BybitFetchTrades.FetchTradesForSymbol(tradeAccount, symbol); });
    }
}
