using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.Kucoin.Spot;

public class Asset() : AssetBase(), IAsset
{
    public Task GetAssetsAsync(CryptoAccount tradeAccount)
    {
        //if (GlobalData.ExchangeListName.TryGetValue(ExchangeName, out Model.CryptoExchange? exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab($"Reading asset information from {ExchangeBase.ExchangeOptions.ExchangeName}");

                //LimitRate.WaitForFairWeight(1);

            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab(error.ToString());
                GlobalData.AddTextToLogTab("");
            }

        }

        return Task.CompletedTask;
    }
}
