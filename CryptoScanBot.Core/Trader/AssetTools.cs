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
    public static async Task<(bool success, string reaction)> FetchAssetsAsync(CryptoAccount tradeAccount, bool forceRefresh = false)
    {
        if (tradeAccount == null)
            return (false, "Invalid trade account");

        if (tradeAccount.AccountType == CryptoAccountType.RealTrading)
        {
            if (GlobalData.TradingApi.Key == "" || GlobalData.TradingApi.Secret == "")
                return (false, "No Exchange API credentials available");
            // TODO Kucoin - check additional password conditions
            // TODO: Make check in the space of the exchange
        }

        if (tradeAccount.AccountType == CryptoAccountType.Altrady)
        {
            if (GlobalData.AltradyApi.Key == "" || GlobalData.AltradyApi.Secret == "")
                return (false, "No Altrady API credentials available");
            // TODO Kucoin - check additional password conditions
            // TODO: Make check in the space of the exchange
        }

        // Refresh assets?
        // Niet bij iedere keer de assets verversen (hammering) - difficult when not to refresh.. not to repeat the same action..
        if (forceRefresh || tradeAccount.Data.LastRefreshAssets == null || tradeAccount.Data.LastRefreshAssets?.AddMinutes(1) < GlobalData.GetCurrentDateTime(tradeAccount))
        {
            if (tradeAccount.AccountType == CryptoAccountType.RealTrading || tradeAccount.AccountType == CryptoAccountType.Altrady)
            {
                var api = GlobalData.Settings.General.Exchange!.GetApiInstance();
                await api.Asset.GetAssetsAsync(tradeAccount); // from exchange
            }
            else
                PaperAssets.LoadAssets(tradeAccount); // from db
            tradeAccount.Data.LastRefreshAssets = GlobalData.GetCurrentDateTime(tradeAccount);
        }


        // okay
        return (true, "");
    }


    public static AssetInfo GetAsset(CryptoAccount tradeAccount, CryptoSymbol symbol)
    {
        // Hoeveel muntjes hebben we op dit moment van deze munt?
        // (Opmerking: een gedeelte hiervan kan in orders zitten!)
        AssetInfo info = new();

        tradeAccount.Data.AssetListSemaphore.Wait();
        try
        {
            if (tradeAccount.AccountType == CryptoAccountType.RealTrading)
            {
                if (tradeAccount.Data.AssetList.TryGetValue(symbol.Base, out CryptoAsset? asset))
                {
                    info.BaseFree = asset.Free;
                    info.BaseTotal = asset.Total;
                }
            }
            else
            {
                // Enough for backtest or papertrading?
                info.BaseFree = 1000000m;
                info.BaseTotal = 1000000m;
            }

            if (tradeAccount.AccountType == CryptoAccountType.RealTrading)
            {
                if (tradeAccount.Data.AssetList.TryGetValue(symbol.Quote, out CryptoAsset? asset))
                {
                    info.QuoteFree = asset.Free;
                    info.QuoteTotal = asset.Total;
                }
            }
            else
            {
                // Enough for backtest or papertrading?
                info.QuoteFree = 1000000m;
                info.QuoteTotal = 1000000m;
            }

        }
        finally
        {
            tradeAccount.Data.AssetListSemaphore.Release();
        }
        return info;
    }


    public static (bool success, decimal entryQuoteAsset, AssetInfo info, string reaction) CheckAvailableAssets(CryptoAccount tradeAccount, CryptoSymbol symbol)
    {
        // Get asset amounts
        var info = GetAsset(tradeAccount, symbol);
        if (info.QuoteTotal <= 0)
            return (false, 0, info, $"No assets available for {symbol.Quote}");


        // The entry value (in quote)
        decimal entryQuoteAsset = TradeTools.GetEntryAmount(symbol, info.QuoteTotal, tradeAccount.AccountType);
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
