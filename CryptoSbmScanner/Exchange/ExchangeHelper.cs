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
        else if (exchangeId == 5)
            return new Kraken.Api();
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

    public static async Task FetchSymbolsAsync()
    {
        await GetApiInstance().FetchSymbolsAsync();
    }

    public static async Task FetchCandlesAsync()
    {
        await GetApiInstance().FetchCandlesAsync();
    }

#if TRADEBOT
    public static async Task FetchAssetsAsync(CryptoTradeAccount tradeAccount)
    {
        if (tradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
            return; // Task.CompletedTask;
        await GetApiInstance().FetchAssetsAsync(tradeAccount);
    }

    public static async Task FetchTradesAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        if (tradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
            return; // Task.CompletedTask;
        await GetApiInstance().FetchTradesForSymbolAsync(tradeAccount, symbol);
    }


    public static async Task<(bool succes, TradeParams tradeParams)> Cancel(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoPositionStep step)
    {
        return await GetApiInstance().Cancel(tradeAccount, symbol, step);
    }

#endif

}
