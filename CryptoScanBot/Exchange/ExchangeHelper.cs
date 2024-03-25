using CryptoScanBot.Context;
using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;
using CryptoScanBot.Settings;

namespace CryptoScanBot.Exchange;

public class ExchangeHelper
{
    public static CancellationTokenSource CancellationTokenSource { get; set; } = new();
    public static CancellationToken CancellationToken { get; set; } = CancellationTokenSource.Token;

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
            throw new Exception("Exchange not supported");
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
        ScannerLog.Logger.Trace($"ExchangeHelper.GetAssetsForAccountAsync: Position {tradeAccount.Name}");
        if (tradeAccount == null || tradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
            return;
        await GetApiInstance().GetAssetsForAccountAsync(tradeAccount);
    }

	public static async Task<int> GetOrdersForPositionAsync(CryptoDatabase database, CryptoPosition position)
    {
        ScannerLog.Logger.Trace($"ExchangeHelper.GetOrdersForPositionAsync: Position {position.Symbol.Name}");
        if (position.TradeAccount == null || position.TradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
            return 0;
        return await GetApiInstance().GetOrdersForPositionAsync(database, position);
    }

    public static async Task<int> GetTradesForPositionAsync(CryptoDatabase database, CryptoPosition position)
    {
        ScannerLog.Logger.Trace($"ExchangeHelper.GetTradesForPositionAsync: Position {position.Symbol.Name}");
        if (position.TradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
            return 0;
        return await GetApiInstance().GetTradesForPositionAsync(database, position);
    }

#endif

}
