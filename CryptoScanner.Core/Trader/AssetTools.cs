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


    /// <summary>
    /// What every DCA level behind an entry of <paramref name="entryQuoteAsset"/> costs together, in
    /// quote. The trader puts all remaining levels on the book the moment the entry fills (see
    /// PositionMonitor.CheckAddDcaFixedPercentage), so opening a position really commits this much on
    /// top of the entry itself.
    /// <para>
    /// The factor is a percentage of the entry amount: 100 = the same amount again, 200 = twice the
    /// amount. It is NOT a multiplier, however much "2" looks like one.
    /// </para>
    /// </summary>
    public static decimal GetDcaReservation(decimal entryQuoteAsset)
    {
        decimal reserved = 0;
        foreach (var dcaEntry in GlobalData.Settings.Trading.DcaList)
            reserved += entryQuoteAsset * dcaEntry.Factor / 100m;
        return reserved;
    }


    /// <summary>
    /// Whether the free balance covers the entry AND every DCA level behind it, weighed on the
    /// entry value that is really going to be ordered.
    /// <para>
    /// CheckAvailableAssets asks the same question one step earlier, on the amount that was MEANT to
    /// be staked. Between the two the quantity is put onto the symbol's size grid, and that moves
    /// the number: down when it is rounded off, and up when the grid step is coarser than the stake
    /// (one tick of XAUT0 on HyperLiquid is worth 44.62 against an entry amount of 15). Since every
    /// DCA level is a percentage OF that entry value, a shifted entry shifts the whole commitment
    /// with it - which is the number that has to fit.
    /// </para>
    /// </summary>
    public static bool CheckAssetsCoverEntryAndDca(Model.CryptoExchange activeExchange, CryptoSymbol symbol,
        decimal entryValue, out string reason)
    {
        reason = "";

        // Without asset management nothing is refused for lack of money (the balance is allowed to
        // run negative), same reading as CheckAvailableAssets uses.
        if (!GlobalData.Settings.Trading.UseAssetManagement)
            return true;

        var info = GetAsset(activeExchange, symbol);

        // Reserved for every configured level, including ones a signal SL would skip - erring
        // towards refusing an entry we could have taken rather than taking one we cannot defend.
        decimal dcaReservation = GetDcaReservation(entryValue);
        decimal required = entryValue + dcaReservation;
        if (required > info.QuoteFree)
        {
            string what = dcaReservation > 0
                ? $"entryamount {entryValue} + dca's {dcaReservation}"
                : $"entryamount {entryValue}";
            reason = $"{what} > free assets {symbol.Quote}={info.QuoteFree}";
            return false;
        }
        return true;
    }


    /// <summary>
    /// Whether there is room for an entry, and how big that entry may be.
    /// </summary>
    /// <param name="reserveForDca">
    /// True when a NEW position is being opened: the DCA levels behind the entry have to fit as well,
    /// because they all go onto the book as soon as the entry fills. False when something is added to
    /// a position that is already open - those orders are already in the locked amount.
    /// </param>
    public static (bool success, decimal entryQuoteAsset, AssetInfo info, string reaction) CheckAvailableAssets(
        Model.CryptoExchange activeExchange, CryptoSymbol symbol, bool reserveForDca = false)
    {
        // GetSymbolData asset amounts
        var info = GetAsset(activeExchange, symbol);

        bool useAssetManagement = GlobalData.Settings.Trading.UseAssetManagement;
        if (useAssetManagement && info.QuoteTotal <= 0)
            return (false, 0, info, $"No assets available for {symbol.Quote}");


        // The entry value (in quote). With asset management on it is sized against what is FREE - the
        // rest is reserved by orders that are already on the book, so spending it would be spending
        // the same money twice.
        decimal entryQuoteAsset;
        if (useAssetManagement)
            entryQuoteAsset = TradeTools.GetEntryAmount(symbol, info.QuoteFree);
        else
        {
            // With it off there is no balance to take a percentage OF - the balance is allowed to run
            // negative and a percentage of that shrinks every entry to nothing. So the plain entry
            // amount is the entry, and a quote coin configured with only a percentage cannot trade in
            // this mode. Saying so beats silently entering with a number nobody chose.
            entryQuoteAsset = symbol.QuoteData!.EntryAmount;
            if (entryQuoteAsset <= 0)
                return (false, entryQuoteAsset, info, $"No entry amount given for {symbol.Quote} (a percentage needs asset management)");
        }

        if (entryQuoteAsset <= 0)
            return (false, entryQuoteAsset, info, "No amount/percentage given");


        // A percentage of a shrinking balance produces ever smaller entries. The entry AMOUNT is the
        // floor under that: below it an entry is not worth taking any more. Only when both are filled
        // in - a percentage without an amount has no floor, which is how it always worked.
        if (symbol.QuoteData!.EntryPercentage > 0 && symbol.QuoteData.EntryAmount > 0 && entryQuoteAsset < symbol.QuoteData.EntryAmount)
            return (false, entryQuoteAsset, info, $"Not enough cash available entryamount {entryQuoteAsset} < minimum entry amount {symbol.QuoteData.EntryAmount}");


        // Check [min..max]
        if (symbol.QuoteValueMinimum > 0 && entryQuoteAsset < symbol.QuoteValueMinimum)
            return (false, entryQuoteAsset, info, $"Not enough cash available entryamount {entryQuoteAsset} < minimum instap van {symbol.QuoteValueMinimum}");
        if (symbol.QuoteValueMaximum > 0 && entryQuoteAsset > symbol.QuoteValueMaximum)
            return (false, entryQuoteAsset, info, $"Not enough cash available entryamount {entryQuoteAsset} > maximum instap van {symbol.QuoteValueMaximum}");


        if (useAssetManagement)
        {
            // Opening a position commits the entry AND every DCA level behind it, so both have to fit.
            // Without this a position could be opened that cannot be defended: the entry fills, and
            // the DCA orders that were supposed to catch the drop are refused for lack of money.
            decimal requiredQuoteAsset = entryQuoteAsset;
            if (reserveForDca)
                requiredQuoteAsset += GetDcaReservation(entryQuoteAsset);

            // TODO Short/Long, bij futures/margin && short hoef je dit te bezitten (wel een onderpand?) - uitzoeken
            if (requiredQuoteAsset > info.QuoteFree)
            {
                string required = requiredQuoteAsset > entryQuoteAsset
                    ? $"entryamount {entryQuoteAsset} + dca's {requiredQuoteAsset - entryQuoteAsset}"
                    : $"entryamount {entryQuoteAsset}";
                return (false, entryQuoteAsset, info, $"Not enough cash available {required} >= free assets {symbol.Quote}={info.QuoteFree}");
            }
            // Totaal overbodig
            if (requiredQuoteAsset > info.QuoteTotal)
                return (false, entryQuoteAsset, info, $"Not enough cash available entryamount {entryQuoteAsset} >= total assets {symbol.Quote}={info.QuoteTotal}");
        }


        // okay
        return (true, entryQuoteAsset, info, "");
    }

}
