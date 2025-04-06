using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

using Kraken.Net.Clients;
using Kraken.Net.Objects.Models;

namespace CryptoScanBot.Core.Exchange.Kraken.Spot;

public class Asset() : AssetBase(), IAsset
{
    public static void PickupAssets(Model.CryptoExchange activeExchange, Dictionary<string, KrakenBalanceAvailable> balances)
    {
        activeExchange.Data.AssetListSemaphore.Wait();
        try
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Open();

            using var transaction = databaseThread.BeginTransaction();
            try
            {
                foreach (var assetInfo in balances.Values)
                {
                    if (assetInfo.Available > 0)
                    {
                        if (!activeExchange.Data.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset? asset))
                        {
                            asset = new CryptoAsset()
                            {
                                Name = assetInfo.Asset,
                            };
                            activeExchange.Data.AssetList.Add(asset.Name, asset);
                        }
                        asset.Free = assetInfo.Available;
                        asset.Locked = assetInfo.Locked;
                        asset.Total = assetInfo.Total;

                        if (asset.Id == 0)
                            databaseThread.Connection.Insert(asset, transaction);
                        else
                            databaseThread.Connection.Update(asset, transaction);
                    }
                }

                // remove assets with total=0
                foreach (var asset in activeExchange.Data.AssetList.Values.ToList())
                {
                    if (asset.Total == 0)
                    {
                        databaseThread.Connection.Delete(asset, transaction);
                        activeExchange.Data.AssetList.Remove(asset.Name);
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
            activeExchange.Data.AssetListSemaphore.Release();
        }
    }


    public async Task GetAssets(Model.CryptoExchange activeExchange)
    {
        //if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading asset information from {ExchangeBase.ExchangeOptions.ExchangeName}");

                LimitRate.WaitForFairWeight(1);

                using var client = new KrakenRestClient();
                {
                    var accountInfo = await client.SpotApi.Account.GetAvailableBalancesAsync();
                    if (!accountInfo.Success)
                    {
                        GlobalData.AddTextToLogTab("error getting accountinfo " + accountInfo.Error);
                    }

                    //Zo af en toe komt er geen data of is de Data niet gezet.
                    //De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                    if (accountInfo?.Data is null)
                        throw new ExchangeException("Geen account data ontvangen");

                    try
                    {
                        PickupAssets(activeExchange, accountInfo.Data);
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
