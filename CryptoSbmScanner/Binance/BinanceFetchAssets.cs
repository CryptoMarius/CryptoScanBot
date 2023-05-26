using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot;

using CryptoExchange.Net.Objects;

using CryptoSbmScanner.Intern;

namespace CryptoSbmScanner.Binance;

public class BinanceFetchAssets
{
    public async Task Execute()
    {
        //We onderteunen momenteel enkel de exchange "binance"
        Model.CryptoExchange exchange;
        if (GlobalData.ExchangeListName.TryGetValue("Binance", out exchange))
        {
            try
            {
                GlobalData.AddTextToLogTab("Reading asset information from Binance");

                BinanceWeights.WaitForFairBinanceWeight(1);

                using (var client = new BinanceClient())
                {
                    WebCallResult<BinanceAccountInfo> accountInfo = await client.SpotApi.Account.GetAccountInfoAsync();

                    if (!accountInfo.Success)
                    {
                        GlobalData.AddTextToLogTab("error getting accountinfo " + accountInfo.Error);
                    }

                    //Zo af en toe komt er geen data of is de Data niet gezet.
                    //De verbindingen naar extern kunnen (tijdelijk) geblokkeerd zijn
                    if ((accountInfo == null) | (accountInfo.Data == null))
                        throw new ExchangeException("Geen account data ontvangen");

                    try
                    {
                        Helper.PickupAssets(exchange, accountInfo.Data.Balances);
                        GlobalData.AssetsHaveChanged("");
                    }
                    catch (Exception error)
                    {
                        GlobalData.Logger.Error(error);
                        GlobalData.AddTextToLogTab(error.ToString());
                        throw;
                    }
                }
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab(error.ToString());
                GlobalData.AddTextToLogTab("");
            }

        }
    }
}

