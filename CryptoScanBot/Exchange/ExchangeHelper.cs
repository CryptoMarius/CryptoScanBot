using System.Text.Encodings.Web;
using System.Text.Json;

using CryptoScanBot.Context;
using CryptoScanBot.Enums;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange;

public class ExchangeHelper
{
    public static CancellationTokenSource CancellationTokenSource { get; set; } = new();
    public static CancellationToken CancellationToken { get; set; } = CancellationTokenSource.Token;
    public static readonly JsonSerializerOptions JsonSerializerNotIndented = new()
    { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = false, IncludeFields = true };

public static Ticker PriceTicker { get; set; }
    public static Ticker KLineTicker { get; set; }
#if TRADEBOT
    public static Ticker UserTicker { get; set; }
#endif


    public static ExchangeBase GetExchangeInstance(int exchangeId)
    {
        // Yup, eventjes hardcoded voor nu, nog eens zien hoe dit verbeterd kan worden
        if (exchangeId == 1)
            return new BinanceSpot.Api();
        else if (exchangeId == 2)
            return new BybitSpot.Api();
        else if (exchangeId == 3)
            return new BybitFutures.Api();
        else if (exchangeId == 4)
            return new KucoinSpot.Api();
        else if (exchangeId == 5)
            return new KrakenSpot.Api();
        else if (exchangeId == 6)
            return new BinanceFutures.Api();
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

    public static async Task GetSymbolsAsync()
    {
        await GetApiInstance().GetSymbolsAsync();
    }

    public static async Task GetCandlesAsync()
    {
        await GetApiInstance().GetCandlesAsync();
    }

#if TRADEBOT
    public static async Task GetAssetsAsync(CryptoTradeAccount tradeAccount)
    {
        ScannerLog.Logger.Trace($"ExchangeHelper.GetAssetsForAccountAsync: Position {tradeAccount.Name}");
        if (tradeAccount == null || tradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
            return;
        await GetApiInstance().GetAssetsAsync(tradeAccount);
    }

	public static async Task<int> GetOrdersAsync(CryptoDatabase database, CryptoPosition position)
    {
        ScannerLog.Logger.Trace($"ExchangeHelper.GetOrdersForPositionAsync: Position {position.Symbol.Name}");
        if (position.TradeAccount == null || position.TradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
            return 0;
        return await GetApiInstance().GetOrdersAsync(database, position);
    }

    public static async Task<int> GetTradesAsync(CryptoDatabase database, CryptoPosition position)
    {
        ScannerLog.Logger.Trace($"ExchangeHelper.GetTradesForPositionAsync: Position {position.Symbol.Name}");
        if (position.TradeAccount.TradeAccountType != CryptoTradeAccountType.RealTrading)
            return 0;
        return await GetApiInstance().GetTradesAsync(database, position);
    }

#endif

}
