using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Trader;

/// <summary>
/// Papertrading asset management 
/// </summary>
public class PaperAssets
{
    public static void Change(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoOrderSide side, CryptoOrderStatus status, decimal quantity, decimal quoteQuantity)
    {
        tradeAccount.AssetListSemaphore.Wait();
        try
        {
            using CryptoDatabase database = new();
            database.Open();

            var transaction = database.BeginTransaction();


            // Base asset
            if (!tradeAccount.AssetList.TryGetValue(symbol.Base, out CryptoAsset? assetBase))
            {
                assetBase = new CryptoAsset()
                {
                    Name = symbol.Base,
                    TradeAccountId = tradeAccount.Id,
                };
                tradeAccount.AssetList.Add(assetBase.Name, assetBase);
            }

            // Quote asset
            if (!tradeAccount.AssetList.TryGetValue(symbol.Quote, out CryptoAsset? assetQuote))
            {
                assetQuote = new CryptoAsset()
                {
                    Name = symbol.Quote,
                    TradeAccountId = tradeAccount.Id,
                };
                tradeAccount.AssetList.Add(assetQuote.Name, assetQuote);
            }


            // Manipulate assets (voorbeeld is BTCUSDT)
            if (side == CryptoOrderSide.Buy)
            {
                if (status == CryptoOrderStatus.New)
                    // Bij het plaatsen van een buy order wordt de hoeveelheid locked USDT hoger
                    assetQuote.Locked += quoteQuantity;
                else
                    // Bij het annuleren/vullen van een buy order wordt de hoeveelheid locked USDT lager
                    assetQuote.Locked -= quoteQuantity;

                if (status.IsFilled())
                {
                    assetBase.Total += quantity;
                    assetQuote.Total -= quoteQuantity;
                }
            }

            if (side == CryptoOrderSide.Sell)
            {
                if (status == CryptoOrderStatus.New)
                    // Bij het plaatsen van een sell order wordt de hoeveelheid locked BTC hoger
                    assetBase.Locked += quantity;
                else
                    // Bij het annuleren/vullen van een sell order wordt de hoeveelheid locked BTC lager
                    assetBase.Locked -= quantity;

                if (status.IsFilled())
                {
                    assetBase.Total -= quantity;
                    assetQuote.Total += quoteQuantity;
                }
            }



            // Base
            assetBase.Free = assetBase.Total - assetBase.Locked;
            if (assetBase.Id == 0)
                database.Connection.Insert(assetBase, transaction);
            else if (assetBase.Total == 0)
            {
                tradeAccount.AssetList.Remove(assetBase.Name);
                database.Connection.Delete(assetBase, transaction);
            }
            else
                database.Connection.Update(assetBase, transaction);

            // Quote
            assetQuote.Free = assetQuote.Total - assetQuote.Locked;
            if (assetQuote.Id == 0)
                database.Connection.Insert(assetQuote, transaction);
            else if (assetQuote.Total == 0)
            {
                tradeAccount.AssetList.Remove(assetQuote.Name);
                database.Connection.Delete(assetQuote, transaction);
            }
            else
                database.Connection.Update(assetQuote, transaction);

            transaction.Commit();
        }
        finally
        {
            tradeAccount.AssetListSemaphore.Release();
        }
    }


}
