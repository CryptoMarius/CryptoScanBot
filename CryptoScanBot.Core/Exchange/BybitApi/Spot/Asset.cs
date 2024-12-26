using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Exchange.BybitApi.Spot;

public class Asset() : AssetBase(), IAsset
{
    public static void PickupAssets(CryptoAccount tradeAccount, BybitAllAssetBalances balances)
    {
        tradeAccount.Data.AssetListSemaphore.Wait();
        try
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Open();

            using var transaction = databaseThread.BeginTransaction();
            try
            {
                // remove assets with total=0
                foreach (var asset in tradeAccount.Data.AssetList.Values.ToList())
                {
                    asset.Total = 0;
                }

                foreach (var assetInfo in balances.Balances)
                {
                    if (assetInfo.WalletBalance > 0)
                    {
                        if (!tradeAccount.Data.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset? asset))
                        {
                            asset = new()
                            {
                                Name = assetInfo.Asset,
                                TradeAccountId = tradeAccount.Id
                            };
                            tradeAccount.Data.AssetList.Add(asset.Name, asset);
                        }
                        asset.Total = (decimal)assetInfo.WalletBalance;
                        asset.Locked = (decimal)assetInfo.WalletBalance - assetInfo.TransferBalance;
                        asset.Free = asset.Total - asset.Locked;

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
        //ScannerLog.Logger.Trace($"Exchange.BybitSpot.GetAssetsForAccountAsync: Positie {tradeAccount.Name}");
        //if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading asset information from {ExchangeBase.ExchangeOptions.ExchangeName}");

                LimitRate.WaitForFairWeight(1);

                using var client = new BybitRestClient();
                {
                    var accountInfo = await client.V5Api.Account.GetAllAssetBalancesAsync(AccountType.Spot);
                    if (!accountInfo.Success)
                    {
                        GlobalData.AddTextToLogTab($"{Api.ExchangeOptions.ExchangeName} error getting accountinfo " + accountInfo.Error);
                        return;
                    }

                    // Zo af en toe komt er geen data of is de Data niet gezet.
                    // De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                    if (accountInfo?.Data is null)
                    {
                        GlobalData.AddTextToLogTab($"{Api.ExchangeOptions.ExchangeName} No account data received {accountInfo?.Error}");
                        return;
                    }

                    try
                    {
                        PickupAssets(tradeAccount, accountInfo.Data);
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
