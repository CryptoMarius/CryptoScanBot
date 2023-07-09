using CryptoSbmScanner.Exchange.Binance;
using CryptoSbmScanner.Exchange.Bybit;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;


// Hier valt nog wel het een en ander te optimaliseren
// Voorstel introductie inheritance
// (heeft nog even tijd)

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


    public static async Task FetchCandlesAsync()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            await Task.Run(async () => { await BinanceFetchCandles.ExecuteAsync(); });
        else
            await Task.Run(async () => { await BybitFetchCandles.ExecuteAsync(); });

    }


    public static async Task StartPriceTickerAsync()
    {
        // De (uitgebreide) price ticker voor laatste prijs, bied prijs, vraag prijs, volume enzovoort
        if (GlobalData.Settings.General.ExchangeId == 1)
            await BinanceApi.StartPriceTickerAsync();
        else
            await BybitApi.StartPriceTickerAsync();
    }
    public static async Task StopPriceTicker()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            await BinanceApi.StopPriceTickerAsync();
        else
            await BybitApi.StopPriceTickerAsync();
    }
    public static void ResetPriceTicker()
    {
        // De (uitgebreide) price ticker voor laatste prijs, bied prijs, vraag prijs, volume enzovoort
        if (GlobalData.Settings.General.ExchangeId == 1)
            BinanceApi.ResetPriceTicker();
        else
            BybitApi.ResetPriceTicker();
    }
    public static int CountPriceTicker()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            return BinanceApi.CountPriceTicker();
        else
            return BybitApi.CountPriceTicker();
    }


    public static async Task Start1mCandleAsync()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            await BinanceApi.Start1mCandleAsync();
        else
            await BybitApi.Start1mCandleAsync();
    }
    public static async Task Stop1mCandleAsync()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            await BinanceApi.Stop1mCandleAsync();
        else
            await BybitApi.Stop1mCandleAsync();
    }
    public static void Reset1mCandle()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            BinanceApi.Reset1mCandle();
        else
            BybitApi.Reset1mCandle();
    }
    public static int Count1mCandle()
    {
        if (GlobalData.Settings.General.ExchangeId == 1)
            return BinanceApi.Count1mCandle();
        else
            return BybitApi.Count1mCandle();
    }


#if TRADEBOT
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
#endif
}
