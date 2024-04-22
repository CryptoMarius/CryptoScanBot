using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Trader;

public struct AssetInfo
{
    // Info base asset
    public decimal BaseFree = 0;
    public decimal BaseTotal = 0;

    // Info quote asset
    public decimal QuoteFree = 0;
    public decimal QuoteTotal = 0;

    public AssetInfo()
    {
    }
}

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

            // TODO Kucoin - check additional password conditions
            // TODO: Make check in the space of the exchange
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


    public static AssetInfo GetAsset(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        // Hoeveel muntjes hebben we op dit moment van deze munt?
        // (Opmerking: een gedeelte hiervan kan in orders zitten!)
        AssetInfo info = new();

        tradeAccount.AssetListSemaphore.Wait();
        try
        {
            if (tradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading)
            {
                if (tradeAccount.AssetList.TryGetValue(symbol.Base, out CryptoAsset asset))
                {
                    info.BaseFree = asset.Free;
                    info.BaseTotal = asset.Total;
                }
            }
            else
            {
                // Enought for backtest or papertrading?
                info.BaseFree = 1000000m;
                info.BaseTotal = 1000000m;
            }

            if (tradeAccount.TradeAccountType == CryptoTradeAccountType.RealTrading)
            {
                if (tradeAccount.AssetList.TryGetValue(symbol.Quote, out CryptoAsset asset))
                {
                    info.QuoteFree = asset.Free;
                    info.QuoteTotal = asset.Total;
                }
            }
            else
            {
                // Enought for backtest or papertrading?
                info.QuoteFree = 1000000m;
                info.QuoteTotal = 1000000m;
            }

        }
        finally
        {
            tradeAccount.AssetListSemaphore.Release();
        }
        return info;
    }


    public static (bool success, decimal entryQuoteAsset, AssetInfo info, string reaction) CheckAvailableAssets(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        // Get asset amounts
        var info = GetAsset(tradeAccount, symbol);
        if (info.QuoteTotal <= 0)
            return (false, 0, info, $"No assets available for {symbol.Quote}");


        // The entry value (in quote)
        decimal entryQuoteAsset = TradeTools.GetEntryAmount(symbol, info.QuoteTotal, tradeAccount.TradeAccountType);
        if (entryQuoteAsset <= 0)
            return (false, entryQuoteAsset, info, "No amount/percentage given");


        // Check [min..max]
        if (symbol.QuoteValueMinimum > 0 && entryQuoteAsset < symbol.QuoteValueMinimum)
            return (false, entryQuoteAsset, info, $"Not enough cash available entryamount {entryQuoteAsset} < minimum instap van {symbol.QuoteValueMinimum}");
        if (symbol.QuoteValueMaximum > 0 && entryQuoteAsset > symbol.QuoteValueMaximum)
            return (false, entryQuoteAsset, info, $"Not enough cash available entryamount {entryQuoteAsset} > maximum instap van {symbol.QuoteValueMaximum}");


        // TODO Short/Long, bij futures/margin && short hoef je dit te bezitten (wel een onderpand?) - uitzoeken
        if (entryQuoteAsset > info.QuoteFree)
            return (false, entryQuoteAsset, info, $"Not enough cash available entryamount {entryQuoteAsset} >= free assets {symbol.Quote}={info.QuoteFree}");
        // Totaal overbodig
        if (entryQuoteAsset > info.QuoteTotal)
            return (false, entryQuoteAsset, info, $"Not enough cash available entryamount {entryQuoteAsset} >= total assets {symbol.Quote}={info.QuoteTotal}");


        // okay
        return (true, entryQuoteAsset, info, "");
    }

}
