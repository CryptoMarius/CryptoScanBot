using CryptoScanBot.Context;
using CryptoScanBot.Enums;
using CryptoScanBot.Exchange;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Trader;

public class AssetTools
{
    public static async Task<(bool success, string reaction)> FetchAssetsAsync(CryptoTradeAccount tradeAccount, bool forceRefresh = false)
    {
        if (!PositionTools.ValidTradeAccount(tradeAccount))
            return (false, "Invalid trade account");


        if (tradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading)
        {
            if (GlobalData.TradingApi.Key == "" || GlobalData.TradingApi.Secret == "")
                return (false, "No API credentials available");
        }


        // Niet bij iedere keer de assets verversen (hammering) - difficult when not to refresh.. not to repeat the same action..
        if (forceRefresh || tradeAccount.LastRefreshAssets == null || tradeAccount.LastRefreshAssets?.AddMinutes(1) < DateTime.UtcNow)
        {
            // De assets verversen (optioneel adhv account)
            await ExchangeHelper.GetAssetsAsync(tradeAccount);
            tradeAccount.LastRefreshAssets = DateTime.UtcNow;
        }

        // okay
        return (true, "");
    }


    public static (decimal totalQuoteAsset, decimal freeQuoteAsset) GetAsset(CryptoTradeAccount tradeAccount, string assetName)
    {
        // Hoeveel muntjes hebben we op dit moment van deze munt?
        // (Opmerking: een gedeelte hiervan kan in orders zitten!)
        decimal freeQuoteAsset = 0;
        decimal totalQuoteAsset = 0;
        tradeAccount.AssetListSemaphore.Wait();
        try
        {
            if (tradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading)
            {
                if (tradeAccount.AssetList.TryGetValue(assetName, out CryptoAsset asset))
                {
                    freeQuoteAsset = asset.Free;
                    totalQuoteAsset = asset.Total;
                }
            }
            else
            {
                // Enought for backtest or papertrading?
                freeQuoteAsset = 1000000m;
                totalQuoteAsset = 1000000m;
            }
        }
        finally
        {
            tradeAccount.AssetListSemaphore.Release();
        }
        return (totalQuoteAsset, freeQuoteAsset);
    }


    public static (bool success, decimal entryQuoteAsset, decimal totalQuoteAsset, decimal freeQuoteAsset, string reaction) CheckAvailableAssets(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        // Get asset amounts
        var (totalQuoteAsset, freeQuoteAsset) = GetAsset(tradeAccount, symbol.Quote);
        if (totalQuoteAsset <= 0)
            return (false, 0, totalQuoteAsset, freeQuoteAsset, $"No assets available for {symbol.Quote}");

        // entry value (in quote)
        decimal entryQuoteAsset = TradeTools.GetEntryAmount(symbol, totalQuoteAsset, tradeAccount.TradeAccountType);
        if (entryQuoteAsset <= 0)
            return (false, entryQuoteAsset, totalQuoteAsset, freeQuoteAsset, "No amount/percentage given");

        // Check [min..max]
        if (symbol.QuoteValueMinimum > 0 && entryQuoteAsset < symbol.QuoteValueMinimum)
            return (false, entryQuoteAsset, totalQuoteAsset, freeQuoteAsset, $"Not enough cash available entryamount {entryQuoteAsset} < minimum instap van {symbol.QuoteValueMinimum}");
        if (symbol.QuoteValueMaximum > 0 && entryQuoteAsset > symbol.QuoteValueMaximum)
            return (false, entryQuoteAsset, totalQuoteAsset, freeQuoteAsset, $"Not enough cash available entryamount {entryQuoteAsset} > maximum instap van {symbol.QuoteValueMaximum}");

        // TODO Short/Long, bij futures/margin && short hoef je dit te bezitten (wel een onderpand?) - uitzoeken
        if (entryQuoteAsset > freeQuoteAsset)
            return (false, entryQuoteAsset, totalQuoteAsset, freeQuoteAsset, $"Not enough cash available entryamount {entryQuoteAsset} >= free assets {symbol.Quote}={freeQuoteAsset}");
        // Totaal overbodig
        if (entryQuoteAsset > totalQuoteAsset)
            return (false, entryQuoteAsset, totalQuoteAsset, freeQuoteAsset, $"Not enough cash available entryamount {entryQuoteAsset} >= total assets {symbol.Quote}={totalQuoteAsset}");

        // okay
        return (true, entryQuoteAsset, totalQuoteAsset, freeQuoteAsset, "");
    }

}
