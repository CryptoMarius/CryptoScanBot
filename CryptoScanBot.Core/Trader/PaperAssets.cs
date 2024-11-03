using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using Dapper.Contrib.Extensions;

using Microsoft.Data.Sqlite;

namespace CryptoScanBot.Core.Trader;

/// <summary>
/// Papertrading asset management 
/// </summary>
public class PaperAssets
{
    public static void LoadAssets(CryptoAccount tradeAccount)
    {
        ScannerLog.Logger.Trace($"PaperAssets.LoadAssets: account {tradeAccount.AccountType}");

        tradeAccount.Data.AssetListSemaphore.Wait();
        try
        {
            using CryptoDatabase database = new();
            database.Open();

            tradeAccount.Data.AssetList.Clear();

            foreach (CryptoAsset asset in database.Connection.GetAll<CryptoAsset>())
            {
                tradeAccount.Data.AssetList.Add(asset.Name, asset);
            }

            CreateAsset(tradeAccount, "BTC", 10);
            CreateAsset(tradeAccount, "USDT", 10000);
        }
        finally
        {
            tradeAccount.Data.AssetListSemaphore.Release();
        }
    }

    public static CryptoAsset CreateAsset(CryptoAccount tradeAccount, string name, decimal defaultTotal)
    {
        if (!tradeAccount.Data.AssetList.TryGetValue(name, out CryptoAsset? asset))
        {
            asset = new()
            {
                Name = name,
                Locked = 0,
                Free = defaultTotal,
                Total = defaultTotal,
                TradeAccountId = tradeAccount.Id,
            };
            tradeAccount.Data.AssetList.Add(asset.Name, asset);

            using CryptoDatabase database = new();
            database.Open();

            if (asset.Id == 0)
                database.Connection.Insert(asset);
            else
                database.Connection.Update(asset);
        }
        return asset;
    }

    internal static CryptoAsset FindOrCreateAsset(CryptoAccount tradeAccount, string name)
    {
        if (!tradeAccount.Data.AssetList.TryGetValue(name, out CryptoAsset? assetBase))
        {
            assetBase = new()
            {
                Name = name,
                Free = 0,
                Total = 0,
                Locked = 0,
                TradeAccountId = tradeAccount.Id,
            };
            tradeAccount.Data.AssetList.Add(assetBase.Name, assetBase);
        }

        return assetBase;
    }

    internal static void AddLocked(CryptoAsset asset, CryptoOrderStatus status, decimal value)
    {
        // When placing an order the locked USDT will be higher
        // When cancelling/filling an order the locked USDT will be lower
        if (status == CryptoOrderStatus.New)
            asset.Locked += value;
        else
            asset.Locked -= value;
    }

    public static void UpdateAsset(CryptoAccount tradeAccount, CryptoDatabase database, SqliteTransaction transaction, CryptoAsset asset)
    {
        // Quote
        if (asset.Total < 0)
            asset.Total = 0; // fix
        if (asset.Locked < 0)
            asset.Locked = 0; // fix
        asset.Free = asset.Total - asset.Locked;

        if (asset.Total == 0)
        {
            tradeAccount.Data.AssetList.Remove(asset.Name);
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

    public static void Change(CryptoAccount tradeAccount, CryptoSymbol symbol, CryptoTradeSide tradeSide, CryptoOrderSide side, 
        CryptoOrderStatus status, decimal quantity, decimal quoteQuantity, string debugText)
    {
        // No asset management for these available (although, would be very nice for Altraady)
        if (tradeAccount.AccountType == CryptoAccountType.RealTrading || tradeAccount.AccountType == CryptoAccountType.Altrady)
            return;

        tradeAccount.Data.AssetListSemaphore.Wait();
        try
        {
            CryptoAsset assetBase = FindOrCreateAsset(tradeAccount, symbol.Base); // Base asset (BTC)
            CryptoAsset assetQuote = FindOrCreateAsset(tradeAccount, symbol.Quote); // Quote asset (USDT)
            if (GlobalData.Settings.General.DebugAssetManagement)
                GlobalData.AddTextToLogTab($"Debug asset before {symbol.Name} {tradeSide} {side} {assetBase.Name} total={assetBase.Total} locked={assetBase.Locked}  {assetQuote.Name} total={assetQuote.Total} locked={assetQuote.Locked} {debugText}");


            // Manipulate assets (example BTCUSDT)

            // When placing an order the locked USDT will be higher
            // When cancelling/filling an order the locked USDT will be lower
            // When an order is filled the total USDT will be higher and BTC will be lower


            if (tradeSide == CryptoTradeSide.Long)
            {
                // going long it will increase the quote (no changes needed)
                
                if (side == CryptoOrderSide.Buy) // entry
                {
                    AddLocked(assetQuote, status, quoteQuantity);
                    if (status.IsFilled())
                    {
                        assetBase.Total += quantity;
                        assetQuote.Total -= quoteQuantity;
                    }
                    //if (status == CryptoOrderStatus.New) asset.Locked += quoteQuantity; else asset.Locked -= quoteQuantity;
                    //if (status.IsFilled()) { assetBase.Total += quantity; asset.Total -= quoteQuantity; //}
                }

                if (side == CryptoOrderSide.Sell) // tp or sl
                {
                    AddLocked(assetBase, status, quantity);
                    if (status.IsFilled())
                    {
                        assetBase.Total -= quantity;
                        assetQuote.Total += quoteQuantity;
                    }
                    //if (status == CryptoOrderStatus.New) assetBase.Locked += quantity; else assetBase.Locked -= quantity;
                    //if (status.IsFilled()) { assetBase.Total -= quantity; asset.Total += quoteQuantity; }
                }
            }
            else
            {
                // TODO, when going short it will increase the quote (if successfull, but we do not now how much here)

                if (side == CryptoOrderSide.Sell) // entry
                {
                    AddLocked(assetQuote, status, quoteQuantity);
                    //if (status == CryptoOrderStatus.New) asset.Locked += quoteQuantity; else asset.Locked -= quoteQuantity;
                    if (status.IsFilled())
                    {
                        assetBase.Total += quantity;
                        assetQuote.Total -= quoteQuantity;
                    }
                }
                if (side == CryptoOrderSide.Buy) // tp or sl
                {
                    AddLocked(assetBase, status, quantity);
                    //if (status == CryptoOrderStatus.New) assetBase.Locked += quantity; else assetBase.Locked -= quantity;
                    if (status.IsFilled())
                    {
                        assetBase.Total -= quantity;
                        assetQuote.Total += quoteQuantity;
                    }
                }
            }



            using CryptoDatabase database = new();
            database.Open();
            using var transaction = database.BeginTransaction();
            UpdateAsset(tradeAccount, database, transaction, assetBase);
            UpdateAsset(tradeAccount, database, transaction, assetQuote);

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
            tradeAccount.Data.AssetListSemaphore.Release();
        }
    }


}
