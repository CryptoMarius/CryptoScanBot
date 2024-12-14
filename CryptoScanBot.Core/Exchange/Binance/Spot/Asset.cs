using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;
using Binance.Net.Objects.Models.Spot.Socket;

using CryptoExchange.Net.Objects;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Exchange.Binance.Spot;

public class Asset() : AssetBase(), IAsset
{
    public static void PickupAssets(CryptoAccount tradeAccount, IEnumerable<BinanceStreamBalance> balances)
    {
        tradeAccount.Data.AssetListSemaphore.Wait();
        {
            try
            {
                foreach (var assetInfo in balances)
                {
                    if (!tradeAccount.Data.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset? asset))
                    {
                        asset = new CryptoAsset()
                        {
                            Name = assetInfo.Asset,
                        };
                        tradeAccount.Data.AssetList.Add(asset.Name, asset);
                    }
                    asset.Free = assetInfo.Available;
                    asset.Total = assetInfo.Total;
                    asset.Locked = assetInfo.Locked;
                }

                // remove assets with total=0
                foreach (var asset in tradeAccount.Data.AssetList.Values.ToList())
                {
                    if (asset.Total == 0)
                    {
                        //TODO: Save in database?

                        tradeAccount.Data.AssetList.Remove(asset.Name);
                    }
                }

            }
            finally
            {
                tradeAccount.Data.AssetListSemaphore.Release();
            }
        }
    }


    public static void PickupAssets(CryptoAccount tradeAccount, IEnumerable<BinanceBalance> balances)
    {
        tradeAccount.Data.AssetListSemaphore.Wait();
        try
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Open();

            using var transaction = databaseThread.BeginTransaction();
            try
            {
                foreach (var assetInfo in balances)
                {
                    if (assetInfo.Total > 0)
                    {
                        if (!tradeAccount.Data.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset? asset))
                        {
                            asset = new CryptoAsset()
                            {
                                Name = assetInfo.Asset,
                                TradeAccountId = tradeAccount.Id,
                            };
                            tradeAccount.Data.AssetList.Add(asset.Name, asset);
                        }
                        asset.Free = assetInfo.Available;
                        asset.Total = assetInfo.Total;

                        if (asset.Id == 0)
                            databaseThread.Connection.Insert(asset, transaction);
                        else
                            databaseThread.Connection.Update(asset, transaction);
                    }
                }

                // remove assets with total=0
                foreach (var asset in tradeAccount.Data.AssetList.Values.ToList())
                {
                    if (asset.Total == 0)
                    {
                        databaseThread.Connection.Delete(asset, transaction);
                        tradeAccount.Data.AssetList.Remove(asset.Name);
                    }
                }

                transaction.Commit();
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab(error.ToString());
                // Als er ooit een rolback plaatsvindt is de database en objects in het geheugen niet meer in sync..
                transaction.Rollback();
                throw;
            }
        }
        finally
        {
            tradeAccount.Data.AssetListSemaphore.Release();
        }
    }



    public async Task GetAssetsAsync(CryptoAccount tradeAccount)
    {
        //ScannerLog.Logger.Trace($"Exchange.Binance.GetAssetsForAccountAsync: Positie {tradeAccount.Name}");
        //if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading asset information from {Api.ExchangeOptions.ExchangeName}");

                LimitRate.WaitForFairWeight(1);

                using var client = new BinanceRestClient();
                {
                    WebCallResult<BinanceAccountInfo> accountInfo = await client.SpotApi.Account.GetAccountInfoAsync();
                    if (!accountInfo.Success)
                    {
                        GlobalData.AddTextToLogTab("error getting accountinfo " + accountInfo.Error);
                    }

                    //Zo af en toe komt er geen data of is de Data niet gezet.
                    //De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                    if (accountInfo?.Data is null)
                        throw new ExchangeException("No account data received");

                    try
                    {
                        PickupAssets(tradeAccount, accountInfo.Data.Balances);
                        GlobalData.AssetsHaveChanged("");
                    }
                    catch (Exception error)
                    {
                        ScannerLog.Logger.Error(error, "");
                        GlobalData.AddTextToLogTab(error.ToString());
                        throw;
                    }
                }
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab(error.ToString());
                GlobalData.AddTextToLogTab("");
            }

        }
    }


}
