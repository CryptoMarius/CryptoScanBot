using System.Text.Encodings.Web;
using System.Text.Json;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange;

public static class Helper
{
    public static ExchangeBase GetApiInstance(this Model.CryptoExchange exchange)
    {
        switch (exchange.ExchangeType)
        {
            case CryptoExchangeType.Binance:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Binance.Spot.Api();
                else
                    return new Binance.Futures.Api();
            case CryptoExchangeType.Bybit:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new BybitApi.Spot.Api();
                else
                    return new BybitApi.Futures.Api();
            case CryptoExchangeType.Kraken:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Kraken.Spot.Api();
                else
                    throw new Exception("Niet ondersteunde exchange");
            case CryptoExchangeType.Kucoin:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Kucoin.Spot.Api();
                else
                    throw new Exception("Niet ondersteunde exchange");
            case CryptoExchangeType.Mexc:
                if (exchange.TradingType == CryptoTradingType.Spot)
                    return new Mexc.Spot.Api();
                else
                    throw new Exception("Niet ondersteunde exchange");
            default:
                throw new Exception("Niet ondersteunde exchange");
        }
    }
}


public class ExchangeHelper
{
    public static CancellationTokenSource CancellationTokenSource { get; set; } = new();
    public static CancellationToken CancellationToken { get; set; } = CancellationTokenSource.Token;
    public static readonly JsonSerializerOptions JsonSerializerNotIndented = new()
    { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = false };

    public static Ticker? PriceTicker { get; set; }
    public static Ticker? KLineTicker { get; set; }
#if TRADEBOT
    public static Ticker? UserTicker { get; set; }
#endif

 
    public static ExchangeBase GetApiInstance()
    {
        return GlobalData.Settings.General.Exchange!.GetApiInstance();
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
        await GetApiInstance().GetCandlesForAllSymbolsAsync();
    }

#if TRADEBOT
    public static async Task GetAssetsAsync(CryptoAccount tradeAccount)
    {
        ScannerLog.Logger.Trace($"ExchangeHelper.GetAssetsAsync: Position {tradeAccount.AccountType}");
        if (tradeAccount == null || tradeAccount.AccountType != CryptoAccountType.RealTrading)
            return;
        await GetApiInstance().GetAssetsAsync(tradeAccount);
    }

	public static async Task<int> GetOrdersAsync(CryptoDatabase database, CryptoPosition position)
    {
        ScannerLog.Logger.Trace($"ExchangeHelper.GetOrdersAsync: Position {position.Symbol.Name}");
        if (position.Account == null || position.Account.AccountType != CryptoAccountType.RealTrading)
            return 0;
        return await GetApiInstance().GetOrdersAsync(database, position);
    }

    public static async Task<int> GetTradesAsync(CryptoDatabase database, CryptoPosition position)
    {
        ScannerLog.Logger.Trace($"ExchangeHelper.GetTradesAsync: Position {position.Symbol.Name}");
        if (position.Account.AccountType != CryptoAccountType.RealTrading)
            return 0;
        return await GetApiInstance().GetTradesAsync(database, position);
    }

#endif

}
