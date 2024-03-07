using CryptoScanBot.Context;
using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;
using CryptoScanBot.Settings;

namespace CryptoScanBot.Exchange;

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
    public static async Task GetAssetsForAccountAsync(CryptoTradeAccount tradeAccount)
    {
        if (tradeAccount == null || tradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
            return;
        await GetApiInstance().GetAssetsForAccountAsync(tradeAccount);
    }

	public static async Task GetOrdersForPositionAsync(CryptoDatabase database, CryptoPosition position)
    {
        if (position.TradeAccount == null || position.TradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
            return;
        await GetApiInstance().GetOrdersForPositionAsync(database, position);
    }

    public static async Task GetTradesForPositionAsync(CryptoDatabase database, CryptoPosition position)
    {
        if (position.TradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
            return;
        await GetApiInstance().GetTradesForSymbolAsync(database, position);
    }

    public static async Task<(bool succes, TradeParams tradeParams)> Cancel(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoPositionStep step)
    {
        return await GetApiInstance().Cancel(tradeAccount, symbol, step);
    }

#endif

}
