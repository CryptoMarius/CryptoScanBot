using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using Dapper;
using Dapper.Contrib.Extensions;

using Microsoft.Data.Sqlite;

namespace CryptoScanner.Core.Trader;

/// <summary>
/// Papertrading asset management
/// <para>
/// Two values, two very different mechanisms:
/// <list type="bullet">
/// <item><b>Total</b> is booked incrementally, and only when an order actually fills. A fill is a
/// one-off event guarded by <see cref="CryptoPositionStep.IsCalculated"/>, so it can be added up
/// safely.</item>
/// <item><b>Locked</b> is DERIVED - recomputed from the orders that are open right now (see
/// <see cref="RecalculateLocked"/>). It is never accumulated, because accumulating it is exactly
/// what broke the old implementation.</item>
/// </list>
/// </para>
/// </summary>
public class PaperAssets
{
    /// <summary>
    /// The start capital of one quote coin: the amount configured on the coin itself, and otherwise
    /// <paramref name="fallback"/> - the general setting, or the amount typed in the paper-assets
    /// screen or in an emulator run.
    /// <para>
    /// A single amount for every quote coin cannot work: 10.000 is a sensible amount of USDT and an
    /// absurd amount of BTC, while the capital line adds every coin up in USDT (see
    /// <see cref="AssetSnapshotTools.ReferenceCoin"/>). Zero on the coin means "not filled in", so a
    /// configuration that only trades USDT behaves exactly as it did before.
    /// </para>
    /// </summary>
    public static decimal ResolveStartCapital(CryptoQuoteData quoteData, decimal fallback)
    {
        if (quoteData.StartCapital > 0)
            return quoteData.StartCapital;
        return fallback;
    }


    /// <summary>
    /// Hand out the balances a paper account starts with - on an empty database and on every reset.
    /// <para>
    /// Two ways of saying it, and the list wins. A filled
    /// <see cref="Settings.SettingsTrading.PaperAssetDefaults"/> IS the starting point, coin by coin,
    /// and may hold coins that are never traded as a quote coin. Only when that list is empty does
    /// the older road apply: every traded quote coin gets its own start capital, or
    /// <paramref name="fallback"/> when it has none.
    /// </para>
    /// </summary>
    private static void SeedStartBalances(Model.CryptoExchange activeExchange, decimal fallback, bool logToTab)
    {
        List<CryptoPaperAssetDefault> defaults = GlobalData.Settings.Trading.PaperAssetDefaults;
        if (defaults.Count > 0)
        {
            foreach (CryptoPaperAssetDefault entry in defaults)
            {
                // An entry without a coin or without an amount is a half-filled row in the settings
                // screen, not an instruction to hand out nothing.
                if (string.IsNullOrWhiteSpace(entry.Name) || entry.Total <= 0)
                    continue;

                // In capitals, the way coins are administered everywhere else - "btc" typed in the
                // settings screen would otherwise land next to the real BTC balance.
                string name = entry.Name.Trim().ToUpperInvariant();
                CreateAsset(activeExchange, name, entry.Total);
                if (logToTab)
                    GlobalData.AddTextToLogTab($"Paper asset {name} started at {entry.Total.ToString0()}");
            }
            return;
        }

        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            if (quoteData.FetchCandles)
            {
                decimal amount = ResolveStartCapital(quoteData, fallback);
                CreateAsset(activeExchange, quoteData.Name, amount);
                if (logToTab)
                    GlobalData.AddTextToLogTab($"Paper asset {quoteData.Name} started at {amount.ToString0()}");
            }
        }
    }


    public static void LoadAssets(Model.CryptoExchange activeExchange)
    {
        ScannerLog.Logger.Trace($"PaperAssets.LoadAssets: account {activeExchange.Name}");

        activeExchange.Data.AssetListSemaphore.Wait();
        try
        {
            using CryptoDatabase database = new();
            database.Open();

            activeExchange.Data.AssetList.Clear();

            foreach (CryptoAsset asset in database.Connection.GetAll<CryptoAsset>())
            {
                activeExchange.Data.AssetList.TryAdd(asset.Name, asset);
            }

            // Hand out the start capital only when there is nothing at all - a fresh database. Seeding
            // per missing quote coin instead would top the balance up again on every restart, which
            // silently hands free money to a paper session that had traded itself down to zero.
            // Starting over on purpose goes through ResetAssets (Tools -> Paper assets, or the
            // emulator at the start of a run).
            if (activeExchange.Data.AssetList.IsEmpty)
            {
                SeedStartBalances(activeExchange, GlobalData.Settings.Trading.PaperAssetStartCapital, logToTab: true);
            }

            RecalculateLocked(activeExchange);
        }
        finally
        {
            activeExchange.Data.AssetListSemaphore.Release();
        }
    }

    public static CryptoAsset CreateAsset(Model.CryptoExchange activeExchange, string name, decimal defaultTotal)
    {
        if (!activeExchange.Data.AssetList.TryGetValue(name, out CryptoAsset? asset))
        {
            asset = new()
            {
                Name = name,
                Locked = 0,
                Free = defaultTotal,
                Total = defaultTotal,
            };
            activeExchange.Data.AssetList.TryAdd(asset.Name, asset);

            using CryptoDatabase database = new();
            database.Open();

            if (asset.Id == 0)
                database.Connection.Insert(asset);
            else
                database.Connection.Update(asset);

            // Money that appears without a trade behind it, so the capital line can tell the start
            // capital apart from something that was earned.
            AssetAdjustmentTools.Record(activeExchange, asset.Name, 0, defaultTotal,
                CryptoAssetAdjustmentReason.StartCapital);
        }
        return asset;
    }

    internal static CryptoAsset FindOrCreateAsset(Model.CryptoExchange activeExchange, string name)
    {
        if (!activeExchange.Data.AssetList.TryGetValue(name, out CryptoAsset? assetBase))
        {
            assetBase = new()
            {
                Name = name,
                Free = 0,
                Total = 0,
                Locked = 0,
            };
            activeExchange.Data.AssetList.TryAdd(assetBase.Name, assetBase);
        }

        return assetBase;
    }

    /// <summary>
    /// Wipe every paper balance and hand out the starting balances again - the default asset list
    /// when there is one, and otherwise the traded quote coins at <paramref name="startCapital"/>
    /// (see <see cref="SeedStartBalances"/>). The emulator calls this at the start of a run so every
    /// run starts with the same money; the UI offers it as a "start over".
    /// </summary>
    public static void ResetAssets(Model.CryptoExchange activeExchange, decimal startCapital)
    {
        activeExchange.Data.AssetListSemaphore.Wait();
        try
        {
            // Scoped so the connection is closed again before CreateAsset opens its own one below
            using (CryptoDatabase database = new())
            {
                database.Open();
                database.Connection.Execute("delete from Asset");
            }

            // Everything that is thrown away here leaves the account, so it belongs in the ledger -
            // read before the list is cleared, because after that the amounts are gone.
            foreach (CryptoAsset removed in activeExchange.Data.AssetList.Values)
                AssetAdjustmentTools.Record(activeExchange, removed.Name, removed.Total, 0,
                    CryptoAssetAdjustmentReason.Reset);

            activeExchange.Data.AssetList.Clear();

            // Silent: the emulator resets at the start of every run, and a line per coin per run
            // buries the log. The caller says what it handed out.
            SeedStartBalances(activeExchange, startCapital, logToTab: false);
        }
        finally
        {
            activeExchange.Data.AssetListSemaphore.Release();
        }
    }


    /// <summary>
    /// Set one asset to an exact amount - the manual correction from the UI.
    /// </summary>
    public static void SetAsset(Model.CryptoExchange activeExchange, string name, decimal total)
    {
        activeExchange.Data.AssetListSemaphore.Wait();
        try
        {
            CryptoAsset asset = FindOrCreateAsset(activeExchange, name);
            decimal previousTotal = asset.Total;
            asset.Total = total;

            RecalculateLocked(activeExchange);

            using CryptoDatabase database = new();
            database.Open();
            using var transaction = database.BeginTransaction();
            UpdateAsset(activeExchange, database, transaction, asset);
            transaction.Commit();

            // Money in or out without a trade behind it. Correcting a balance to zero is how the
            // paper-assets screen deletes a coin, and UpdateAsset above then drops the row - which is
            // exactly why this is recorded in a table of its own and not next to the balance.
            AssetAdjustmentTools.Record(activeExchange, name, previousTotal, total,
                CryptoAssetAdjustmentReason.ManualCorrection);
        }
        finally
        {
            activeExchange.Data.AssetListSemaphore.Release();
        }
    }


    /// <summary>
    /// Bring the reservations up to date, taking the lock itself. For callers outside the trader that
    /// just want to look at the balances (the paper-assets window); inside the trader
    /// <see cref="RecalculateLocked"/> is called while the lock is already held.
    /// </summary>
    public static void RefreshLocked(Model.CryptoExchange activeExchange)
    {
        activeExchange.Data.AssetListSemaphore.Wait();
        try
        {
            RecalculateLocked(activeExchange);
        }
        finally
        {
            activeExchange.Data.AssetListSemaphore.Release();
        }
    }


    /// <summary>
    /// Recompute the locked amount of every asset from the order steps that are open right now, and
    /// derive Free from it.
    /// <para>
    /// Locked is a DERIVED value, never a running total. It used to be tracked with +/- deltas: an
    /// order locked its value at the order price and released it again at the fill price. Those two
    /// are rarely equal - a market entry is placed at Symbol.LastPrice but filled at the candle
    /// close - so every trade left a little bit locked forever. The free balance kept shrinking until
    /// no new position could be opened, which is what made asset management look broken. Recomputing
    /// removes that failure mode by construction: whatever happened before, the locked amount always
    /// matches the orders that are actually on the book.
    /// </para>
    /// <para>The caller must hold the AssetListSemaphore.</para>
    /// </summary>
    internal static void RecalculateLocked(Model.CryptoExchange activeExchange)
    {
        // Real trading and Altrady get the balance INCLUDING the locked part from the exchange
        // itself, so recomputing here would overwrite the truth with our own guess.
        if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.RealTrading || GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.Altrady)
            return;

        // Build the new amounts first and only then apply them. The emulator walks symbols in
        // parallel and every thread mutates the part/step lists of its own position; those are plain
        // SortedLists, so enumerating another symbol's position can collide with that symbol's
        // thread. Computing into a separate dictionary keeps such a collision from leaving half-reset
        // amounts behind - we keep the previous ones and the next order event redoes the work.
        Dictionary<string, decimal>? lockedPerAsset = CalculateLockedAmounts(activeExchange);
        if (lockedPerAsset == null)
            return;

        foreach (string assetName in lockedPerAsset.Keys)
            FindOrCreateAsset(activeExchange, assetName);

        foreach (CryptoAsset asset in activeExchange.Data.AssetList.Values)
        {
            asset.Locked = lockedPerAsset.TryGetValue(asset.Name, out decimal locked) ? locked : 0;
            asset.Free = asset.Total - asset.Locked;
            // An asset cannot have more reserved than it holds - as long as asset management is what
            // hands out the money. With it switched off the trader spends money it does not have on
            // purpose, and flooring the result here would hide exactly the number the run is meant
            // to show.
            if (asset.Free < 0 && GlobalData.Settings.Trading.UseAssetManagement)
                asset.Free = 0;
        }
    }


    /// <summary>
    /// The amount every asset has tied up in open orders. Returns null when the position
    /// administration kept changing underneath us (see <see cref="RecalculateLocked"/>).
    /// </summary>
    private static Dictionary<string, decimal>? CalculateLockedAmounts(Model.CryptoExchange activeExchange)
    {
        for (int attempt = 0; attempt < 3; attempt++)
        {
            Dictionary<string, decimal> locked = [];
            try
            {
                foreach (CryptoPosition position in activeExchange.Data.PositionList.Values)
                {
                    CryptoSymbol symbol = position.Symbol;
                    foreach (CryptoPositionPart part in position.PartList.Values)
                    {
                        foreach (CryptoPositionStep step in part.StepList.Values)
                        {
                            // Only an order that is still on the book reserves anything
                            if (step.Status != CryptoOrderStatus.New && step.Status != CryptoOrderStatus.PartiallyFilled)
                                continue;

                            // Whatever is not filled yet stays reserved
                            decimal openQuantity = step.Quantity - step.QuantityFilled;
                            if (openQuantity <= 0)
                                continue;

                            if (position.Side == CryptoTradeSide.Long && step.Side == CryptoOrderSide.Sell)
                            {
                                // Long take profit or stop loss: the base coins themselves are on the book
                                AddLocked(locked, symbol.Base, openQuantity);
                            }
                            else
                            {
                                // Everything else reserves quote: a long entry pays quote, and a short is
                                // tracked entirely in quote (entry collateral as well as buyback cost).
                                // A stop order carries its price in StopPrice, not in Price.
                                decimal price = step.Price > 0 ? step.Price : step.StopPrice ?? 0;
                                AddLocked(locked, symbol.Quote, openQuantity * price);
                            }
                        }
                    }
                }
                return locked;
            }
            catch (InvalidOperationException)
            {
                // "Collection was modified" - another symbol's thread changed its position while we
                // were walking it. Retry; whatever we computed so far is discarded.
            }
        }

        ScannerLog.Logger.Trace("PaperAssets.CalculateLockedAmounts: positions kept changing, keeping the previous amounts");
        return null;
    }


    private static void AddLocked(Dictionary<string, decimal> locked, string assetName, decimal value)
    {
        locked[assetName] = locked.TryGetValue(assetName, out decimal current) ? current + value : value;
    }

    public static void UpdateAsset(Model.CryptoExchange activeExchange, CryptoDatabase database, SqliteTransaction transaction, CryptoAsset asset)
    {
        // Quote
        // The floors only apply while asset management guards the entries; without that guard a
        // balance is allowed to go negative, see RecalculateLocked.
        bool useAssetManagement = GlobalData.Settings.Trading.UseAssetManagement;
        if (asset.Total < 0 && useAssetManagement)
            asset.Total = 0; // fix
        if (asset.Locked < 0)
            asset.Locked = 0; // fix
        asset.Free = asset.Total - asset.Locked;
        if (asset.Free < 0 && useAssetManagement)
            asset.Free = 0; // fix

        // Only drop an asset once it is really gone: nothing left AND nothing reserved for an open
        // order. Deleting it while orders were still on the book threw their reservation away.
        if (asset.Total == 0 && asset.Locked == 0)
        {
            activeExchange.Data.AssetList.TryRemove(asset.Name, out _);
            if (asset.Id > 0)
                database.Connection.Delete(asset, transaction);
        }
        else
        {
            if (asset.Id == 0)
                database.Connection.Insert(asset, transaction);
            else
                database.Connection.Update(asset, transaction);
        }
    }

    /// <summary>
    /// Book the commission of a filled order. A commission is always a cost: it lowers the asset it
    /// was charged in, whatever the trade side or the order side was.
    /// <para>
    /// This used to be done by calling <see cref="Change"/> with negative quantities. Change applies
    /// a different sign per side, so that trick subtracted the fee on one side and ADDED it on the
    /// other (futures long entry, short take profit) - an error of twice the fee - and it counted the
    /// fee as a reservation on top of that.
    /// </para>
    /// </summary>
    public static void BookCommission(Model.CryptoExchange activeExchange, CryptoSymbol symbol,
        decimal commissionBase, decimal commissionQuote, string debugText)
    {
        // No asset management for these available (although, would be very nice for Altraady)
        if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.RealTrading || GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.Altrady)
            return;
        if (commissionBase == 0 && commissionQuote == 0)
            return;

        activeExchange.Data.AssetListSemaphore.Wait();
        try
        {
            CryptoAsset assetBase = FindOrCreateAsset(activeExchange, symbol.Base); // Base asset (BTC)
            CryptoAsset assetQuote = FindOrCreateAsset(activeExchange, symbol.Quote); // Quote asset (USDT)

            assetBase.Total -= commissionBase;
            assetQuote.Total -= commissionQuote;

            RecalculateLocked(activeExchange);

            using CryptoDatabase database = new();
            database.Open();
            using var transaction = database.BeginTransaction();
            UpdateAsset(activeExchange, database, transaction, assetBase);
            UpdateAsset(activeExchange, database, transaction, assetQuote);
            transaction.Commit();

            if (GlobalData.Settings.General.DebugAssetManagement)
                GlobalData.AddTextToLogTab($"Debug asset commission {symbol.Name} {assetBase.Name}=-{commissionBase} {assetQuote.Name}=-{commissionQuote} {debugText}");
        }
        finally
        {
            activeExchange.Data.AssetListSemaphore.Release();
        }
    }


    /// <summary>
    /// Undo what a position did to the balances, as if it had never been opened. For deleting a
    /// position from the database: the row disappears, so the money it moved has to disappear with it.
    ///
    /// <para>
    /// Only the FILLED part needs undoing. An order that is still on the book never moved anything, it
    /// only reserved - and the reservation is derived from the orders that are open right now (see
    /// <see cref="RecalculateLocked"/>), so it releases itself the moment the position leaves the
    /// position list.
    /// </para>
    /// <para>
    /// What a position moved is exactly what it administers, whichever way the fills went:
    /// <list type="bullet">
    /// <item><b>Long</b> - base changed by <c>Quantity</c> (already net of the base commission) and
    /// quote by <c>Returned - Invested - Commission</c>.</item>
    /// <item><b>Short</b> - a short never touches base except for a commission charged in it, and
    /// quote changed by <c>Invested - Returned - Commission</c>.</item>
    /// </list>
    /// Both are the profit formula of that side, which is no coincidence: what a position leaves
    /// behind in cash IS its result. Note that they are built from the raw fields and NOT from
    /// <see cref="CryptoPosition.Profit"/>, because that one is corrected for RemainingDust and would
    /// hand back a different amount than was ever booked.
    /// </para>
    /// <para>
    /// Where it matters most is a short. Its proceeds land on the balance at the entry while the coins
    /// it owes are only ever derived from the open position, so deleting one without this would leave
    /// the proceeds behind and let the debt evaporate - the full size of the position, as free money.
    /// </para>
    /// </summary>
    public static void ReversePosition(Model.CryptoExchange activeExchange, CryptoPosition position)
    {
        // Same reason as Change and BookCommission: with real trading or Altrady the balances are
        // kept at the exchange and none of this bookkeeping applies.
        if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.RealTrading || GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.Altrady)
            return;

        decimal baseChange, quoteChange;
        if (position.Side == CryptoTradeSide.Long)
        {
            baseChange = position.Quantity;
            quoteChange = position.Returned - position.Invested - position.Commission;
        }
        else
        {
            baseChange = -position.CommissionBase;
            quoteChange = position.Invested - position.Returned - position.Commission;
        }

        if (baseChange == 0 && quoteChange == 0)
            return; // nothing was ever filled

        CryptoSymbol symbol = position.Symbol;
        decimal previousBase, previousQuote;

        activeExchange.Data.AssetListSemaphore.Wait();
        try
        {
            CryptoAsset assetBase = FindOrCreateAsset(activeExchange, symbol.Base);
            CryptoAsset assetQuote = FindOrCreateAsset(activeExchange, symbol.Quote);

            previousBase = assetBase.Total;
            previousQuote = assetQuote.Total;

            assetBase.Total -= baseChange;
            assetQuote.Total -= quoteChange;

            RecalculateLocked(activeExchange);

            using CryptoDatabase database = new();
            database.Open();
            using var transaction = database.BeginTransaction();
            UpdateAsset(activeExchange, database, transaction, assetBase);
            UpdateAsset(activeExchange, database, transaction, assetQuote);
            transaction.Commit();

            if (GlobalData.Settings.General.DebugAssetManagement)
                GlobalData.AddTextToLogTab($"Debug asset reversal {symbol.Name} {position.Side} position={position.Id} " +
                    $"{assetBase.Name}={-baseChange} {assetQuote.Name}={-quoteChange}");
        }
        finally
        {
            activeExchange.Data.AssetListSemaphore.Release();
        }

        // Outside the lock, because the ledger opens its own connection. Money that moves without a
        // trade behind it, so the capital line must not read it as a result - a deleted losing
        // position gives its loss back, and that is not profit.
        AssetAdjustmentTools.Record(activeExchange, symbol.Base, previousBase, previousBase - baseChange,
            CryptoAssetAdjustmentReason.PositionDeleted);
        AssetAdjustmentTools.Record(activeExchange, symbol.Quote, previousQuote, previousQuote - quoteChange,
            CryptoAssetAdjustmentReason.PositionDeleted);
    }


    public static void Change(Model.CryptoExchange activeExchange, CryptoSymbol symbol, CryptoTradeSide tradeSide, CryptoOrderSide side,
        CryptoOrderStatus status, decimal quantity, decimal quoteQuantity, string debugText)
    {
        // No asset management for these available (although, would be very nice for Altraady)
        if (GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.RealTrading || GlobalData.Settings.Trading.TradeVia == CryptoTradeVia.Altrady)
            return;

        activeExchange.Data.AssetListSemaphore.Wait();
        try
        {
            CryptoAsset assetBase = FindOrCreateAsset(activeExchange, symbol.Base); // Base asset (BTC)
            CryptoAsset assetQuote = FindOrCreateAsset(activeExchange, symbol.Quote); // Quote asset (USDT)
            if (GlobalData.Settings.General.DebugAssetManagement)
                GlobalData.AddTextToLogTab($"Debug asset before {symbol.Name} {tradeSide} {side} {assetBase.Name} total={assetBase.Total} locked={assetBase.Locked}  {assetQuote.Name} total={assetQuote.Total} locked={assetQuote.Locked} {debugText}");


            // Manipulate assets (example BTCUSDT)

            // Only a fill moves money. Placing an order and cancelling it again changes nothing but
            // the reservation, and that is derived from the open steps in RecalculateLocked below.
            // When an order is filled the total USDT will be higher and BTC will be lower


            if (status.IsFilled())
            {
                if (tradeSide == CryptoTradeSide.Long)
                {
                    // going long it will increase the quote (no changes needed)

                    if (side == CryptoOrderSide.Buy) // entry
                    {
                        assetBase.Total += quantity;
                        assetQuote.Total -= quoteQuantity;
                    }

                    if (side == CryptoOrderSide.Sell) // tp or sl
                    {
                        assetBase.Total -= quantity;
                        assetQuote.Total += quoteQuantity;
                    }
                }
                else
                {
                    // Short: tracked entirely via quote (USDT) — base is not modified.
                    // This matches both spot-margin and USDT-margined futures behaviour.
                    // Entry (Sell): receive sale proceeds in quote.
                    // TP/SL (Buy): pay buyback cost from quote.

                    if (side == CryptoOrderSide.Sell) // entry: sell base, receive quote proceeds
                        assetQuote.Total += quoteQuantity;     // quote increases (sale proceeds received)

                    if (side == CryptoOrderSide.Buy) // tp or sl: buy back base, spend quote
                        assetQuote.Total -= quoteQuantity;     // quote decreases (buyback cost paid)
                }
            }

            // The reservation of every open order, this one included
            RecalculateLocked(activeExchange);


            using CryptoDatabase database = new();
            database.Open();
            using var transaction = database.BeginTransaction();
            UpdateAsset(activeExchange, database, transaction, assetBase);
            UpdateAsset(activeExchange, database, transaction, assetQuote);

            // Base
            //if (assetBase.Total < 0)
            //    assetBase.Total = 0; // fix
            //if (assetBase.Locked < 0)
            //    assetBase.Locked = 0; // fix

            //assetBase.Free = assetBase.Total - Math.Abs(assetBase.Locked);

            //if (assetBase.Total == 0)
            //{
            //    tradeAccount.Data.AssetList.Remove(assetBase.Name);
            //    if (assetBase.Id > 0)
            //        database.Connection.Delete(assetBase, transaction);
            //}
            //else
            //{
            //    if (assetBase.Id == 0)
            //        database.Connection.Insert(assetBase, transaction);
            //    else
            //        database.Connection.Update(assetBase, transaction);
            //}

            //// Quote
            //if (assetQuote.Total < 0)
            //    assetQuote.Total = 0; // fix
            //if (assetQuote.Locked < 0)
            //    assetQuote.Locked = 0; // fix

            //assetQuote.Free = assetQuote.Total - Math.Abs(assetQuote.Locked);

            //if (assetQuote.Total == 0)
            //{
            //    tradeAccount.Data.AssetList.Remove(assetQuote.Name);
            //    if (assetQuote.Id > 0)
            //        database.Connection.Delete(assetQuote, transaction);
            //}
            //else
            //{
            //    if (assetQuote.Id == 0)
            //        database.Connection.Insert(assetQuote, transaction);
            //    else
            //        database.Connection.Update(assetQuote, transaction);
            //}
            transaction.Commit();

            if (GlobalData.Settings.General.DebugAssetManagement)
                GlobalData.AddTextToLogTab($"Debug asset after {symbol.Name} {tradeSide} {side} {assetBase.Name} total={assetBase.Total} locked={assetBase.Locked}  {assetQuote.Name} total={assetQuote.Total} locked={assetQuote.Locked} {debugText}");
        }
        finally
        {
            activeExchange.Data.AssetListSemaphore.Release();
        }
    }


}
