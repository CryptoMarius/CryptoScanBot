using Binance.Net.Clients;
using Binance.Net.Objects.Models.Futures;

using CryptoExchange.Net.Objects;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Exchange.Binance.Futures;

public class Asset() : AssetBase(), IAsset
{
    public static void PickupAssets(Model.CryptoExchange activeExchange, IEnumerable<BinanceFuturesAccountInfoAsset> assetList)
    {
        activeExchange.Data.AssetListSemaphore.Wait();
        try
        {
            using CryptoDatabase databaseThread = new();
            databaseThread.Open();

            using var transaction = databaseThread.BeginTransaction();
            try
            {
                foreach (var assetInfo in assetList)
                {
                    if (assetInfo.WalletBalance > 0)
                    {
                        if (!activeExchange.Data.AssetList.TryGetValue(assetInfo.Asset, out CryptoAsset? asset))
                        {
                            asset = new CryptoAsset()
                            {
                                Name = assetInfo.Asset,
                            };
                            activeExchange.Data.AssetList.Add(asset.Name, asset);
                        }
                        asset.Free = assetInfo.AvailableBalance;
                        asset.Total = assetInfo.WalletBalance;
                        asset.Locked = asset.Total - asset.Free;

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
        //ScannerLog.Logger.Trace($"Exchange.Binance.GetAssetsForAccountAsync: Positie {tradeAccount.Name}");
        //if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading asset information from {Api.ExchangeOptions.ExchangeName}");

                LimitRate.WaitForFairWeight(1);

                using var client = new BinanceRestClient();
                {
                    WebCallResult<BinanceFuturesAccountInfoV3> accountInfo = await client.UsdFuturesApi.Account.GetAccountInfoV3Async(); //GetAccountInfoAsync();
                    if (!accountInfo.Success)
                    {
                        GlobalData.AddErrorToLogTab("error getting accountinfo " + accountInfo.Error);
                    }

                    //Zo af en toe komt er geen data of is de Data niet gezet.
                    //De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                    if (accountInfo.Data is null)
                        throw new ExchangeException("No account data received");

                    try
                    {
                        PickupAssets(activeExchange, accountInfo.Data.Assets);
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
