using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Trader;

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
    public static (bool success, string reaction) FetchAssets(Model.CryptoExchange? activeExchange, bool forceRefresh = false)
    {
        //if (activeExchange == null)
        //    return (false, "Invalid trade account");

        //if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.RealTrading)
        //{
        //    if (GlobalData.TradingApi.Key == "" || GlobalData.TradingApi.Secret == "")
        //        return (false, "No Exchange API credentials available");
        //    // TODO Kucoin - check additional password conditions
        //    // TODO: Make check in the space of the exchange
        //}

        //if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.Altrady)
        //{
        //    if (GlobalData.AltradyApi.Key == "" || GlobalData.AltradyApi.Secret == "")
        //        return (false, "No Altrady API credentials available");
        //    // TODO Kucoin - check additional password conditions
        //    // TODO: Make check in the space of the exchange
        //}

        //// Refresh assets?
        //// Niet bij iedere keer de assets verversen (hammering) - difficult when not to refresh.. not to repeat the same action..
        //if (forceRefresh || GlobalData.ActiveExchange!.Data.LastRefreshAssets == null || GlobalData.ActiveExchange.Data.LastRefreshAssets?.AddMinutes(1) < GlobalData.GetCurrentDateTime())
        //{
        //    if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.RealTrading || GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.Altrady)
        //    {
        //        //var api = GlobalData.ActiveExchange!.GetApiInstance();
        //        //await api.Asset.GetAssets(activeExchange); // from exchange
        //    }
        //    else
        //        PaperAssets.LoadAssets(activeExchange); // from db
        //    GlobalData.ActiveExchange!.Data.LastRefreshAssets = GlobalData.GetCurrentDateTime();
        //}

        if (activeExchange == null)
            return (false, "No active exchange");

        // Paper trading keeps its balances in memory and in the database and they are updated on
        // every order event, so there is nothing to refresh. Reading the balances from a real
        // exchange is not wired up: the per-exchange Asset.cs files are excluded from the build in
        // CryptoScanner.Exchanges.csproj, and only Binance and Bybit have one at all.
        if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.RealTrading)
            return (false, "Reading balances from the exchange is not implemented");

        activeExchange.Data.LastRefreshAssets = GlobalData.Clock.UtcNow;

        // okay
        return (true, "");
    }


    public static AssetInfo GetAsset(Model.CryptoExchange activeExchange, CryptoSymbol symbol)
    {
        // Hoeveel muntjes hebben we op dit moment van deze munt?
        // (Opmerking: een gedeelte hiervan kan in orders zitten!)
        AssetInfo info = new();

        activeExchange.Data.AssetListSemaphore.Wait();
        try
        {
            // Paper trading and the emulator used to get a hardcoded 1,000,000 here, which meant the
            // balances PaperAssets so carefully maintained were never actually read by the trader.
            // They are read now, so a paper run really can run out of money.
            // Locked is derived from the orders that are open right now, so refresh it before reading.
            PaperAssets.RecalculateLocked(activeExchange);

            if (activeExchange.Data.AssetList.TryGetValue(symbol.Base, out CryptoAsset? assetBase))
            {
                info.BaseFree = assetBase.Free;
                info.BaseTotal = assetBase.Total;
            }

            if (activeExchange.Data.AssetList.TryGetValue(symbol.Quote, out CryptoAsset? assetQuote))
            {
                info.QuoteFree = assetQuote.Free;
                info.QuoteTotal = assetQuote.Total;
            }

        }
        finally
        {
            activeExchange.Data.AssetListSemaphore.Release();
        }
        return info;
    }


    public static (bool success, decimal entryQuoteAsset, AssetInfo info, string reaction) CheckAvailableAssets(Model.CryptoExchange activeExchange, CryptoSymbol symbol)
    {
        // GetSymbolData asset amounts
        var info = GetAsset(activeExchange, symbol);
        if (info.QuoteTotal <= 0)
            return (false, 0, info, $"No assets available for {symbol.Quote}");


        // The entry value (in quote)
        decimal entryQuoteAsset = TradeTools.GetEntryAmount(symbol, info.QuoteTotal);
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
