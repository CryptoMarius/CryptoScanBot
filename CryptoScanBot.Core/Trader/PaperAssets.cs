using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Trader;

/// <summary>
/// Papertrading asset management 
/// </summary>
public class PaperAssets
{
    public static void Change(CryptoAccount tradeAccount, CryptoSymbol symbol, CryptoTradeSide tradeSide, CryptoOrderSide side, CryptoOrderStatus status, decimal quantity, decimal quoteQuantity)
    {
        // No asset management for these available (although, would be very nice for Altraady)
        if (tradeAccount.AccountType == CryptoAccountType.RealTrading || tradeAccount.AccountType == CryptoAccountType.Altrady)
            return;

        tradeAccount.Data.AssetListSemaphore.Wait();
        try
        {
            using CryptoDatabase database = new();
            database.Open();

            var transaction = database.BeginTransaction();

            // Manipulate assets (example BTCUSDT)


            // Base asset (BTC)
            if (!tradeAccount.Data.AssetList.TryGetValue(symbol.Base, out CryptoAsset? assetBase))
            {
                assetBase = new CryptoAsset()
                {
                    Name = symbol.Base,
                    TradeAccountId = tradeAccount.Id,
                };
                tradeAccount.Data.AssetList.Add(assetBase.Name, assetBase);
            }

            // Quote asset (USDT)
            if (!tradeAccount.Data.AssetList.TryGetValue(symbol.Quote, out CryptoAsset? assetQuote))
            {
                assetQuote = new CryptoAsset()
                {
                    Name = symbol.Quote,
                    TradeAccountId = tradeAccount.Id,
                };
                tradeAccount.Data.AssetList.Add(assetQuote.Name, assetQuote);
            }

            GlobalData.AddTextToLogTab($"Debug asset before {symbol.Name} {tradeSide} {side} {assetBase.Name} total={assetBase.Total} locked={assetBase.Locked}  {assetQuote.Name} total={assetQuote.Total} locked={assetQuote.Locked}");


            if (tradeSide == CryptoTradeSide.Long)
            {
                // When placing an order the locked USDT will be higher
                // When cancelling/filling an order the locked USDT will be lower
                // When an order is filled the total USDT will be higher and BTC will be lower
                if (side == CryptoOrderSide.Buy)
                {
                    if (status == CryptoOrderStatus.New)
                        assetQuote.Locked += quoteQuantity;
                    else
                        assetQuote.Locked -= quoteQuantity;

                    if (status.IsFilled())
                    {
                        assetBase.Total += quantity;
                        assetQuote.Total -= quoteQuantity;
                    }
                }

                // When placing an order the locked BTC will be higher
                // When cancelling/filling an order the locked BTC will be lower
                // When an order is filled the total USDT will be lower and BTC will be higher
                if (side == CryptoOrderSide.Sell)
                {
                    if (status == CryptoOrderStatus.New)
                        assetBase.Locked += quantity;
                    else
                        assetBase.Locked -= quantity;

                    if (status.IsFilled())
                    {
                        assetBase.Total -= quantity;
                        assetQuote.Total += quoteQuantity;
                    }
                }
            }
            else
            {
                if (side == CryptoOrderSide.Sell)
                {
                    if (status == CryptoOrderStatus.New)
                        assetQuote.Locked += quoteQuantity;
                    else
                        assetQuote.Locked -= quoteQuantity;

                    if (status.IsFilled())
                    {
                        assetQuote.Total -= quoteQuantity;
                        assetBase.Total += quantity;
                    }
                }

                if (side == CryptoOrderSide.Buy)
                {
                    if (status == CryptoOrderStatus.New)
                        assetBase.Locked += quantity;
                    else
                        assetBase.Locked -= quantity;

                    if (status.IsFilled())
                    {
                        assetQuote.Total += quoteQuantity;
                        assetBase.Total -= quantity;
                    }
                }
            }



            // Base
            //if (assetBase.Total < 0)
            //    assetBase.Total = 0; // fix
            //if (assetBase.Locked < 0)
            //    assetBase.Locked = 0; // fix

            assetBase.Free = assetBase.Total - assetBase.Locked;
            if (assetBase.Id == 0)
                database.Connection.Insert(assetBase, transaction);
            else if (assetBase.Total == 0)
            {
                tradeAccount.Data.AssetList.Remove(assetBase.Name);
                database.Connection.Delete(assetBase, transaction);
            }
            else
                database.Connection.Update(assetBase, transaction);

            // Quote
            //if (assetQuote.Total < 0)
            //    assetQuote.Total = 0; // fix
            //if (assetQuote.Locked < 0)
            //    assetQuote.Locked = 0; // fix

            assetQuote.Free = assetQuote.Total - assetQuote.Locked;
            if (assetQuote.Id == 0)
                database.Connection.Insert(assetQuote, transaction);
            else if (assetQuote.Total == 0)
            {
                tradeAccount.Data.AssetList.Remove(assetQuote.Name);
                database.Connection.Delete(assetQuote, transaction);
            }
            else
                database.Connection.Update(assetQuote, transaction);

            transaction.Commit();

            GlobalData.AddTextToLogTab($"Debug asset after {symbol.Name} {tradeSide} {side} {assetBase.Name} total={assetBase.Total} locked={assetBase.Locked}  {assetQuote.Name} total={assetQuote.Total} locked={assetQuote.Locked}");
        }
        finally
        {
            tradeAccount.Data.AssetListSemaphore.Release();
        }
    }


}
