using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.BinanceFutures;

#if TRADEBOT
public class SubscriptionUserTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        using BinanceRestClient client = new();
        {
            TickerGroup!.SocketClient ??= new BinanceSocketClient();            
            CallResult<string> userStreamResult = await client.SpotApi.Account.StartUserStreamAsync();
            //if (!userStreamResult.Success)
            //{
            //    GlobalData.AddTextToLogTab($"{Api.ExchangeOptions.ExchangeName} - Error starting user stream: " + userStreamResult.Error.Message);
            //    return;
            //}


            var subscriptionResult = await ((BinanceSocketClient)TickerGroup.SocketClient).UsdFuturesApi.SubscribeToUserDataUpdatesAsync(
                userStreamResult.Data,

                onOrderUpdate: OnOrderUpdate

                //null,
                //null,
                //null, //OnAccountPositionMessage,
                //null
                ).ConfigureAwait(false);


            return subscriptionResult;
        }

    }

    private void OnOrderUpdate(DataEvent<BinanceFuturesStreamOrderUpdate> data) //DataEvent<BinanceStreamOrderUpdate> data
    {
        try
        {
            // We zijn slechts geinteresseerd in 3 statussen (de andere zijn niet interessant voor de afhandeling van de order)
            if (data.Data.UpdateData.Status == OrderStatus.New ||
                data.Data.UpdateData.Status == OrderStatus.Filled ||
                data.Data.UpdateData.Status == OrderStatus.PartiallyFilled || 
                data.Data.UpdateData.Status == OrderStatus.Canceled)
            {
                // Nieuwe thread opstarten en de data meegeven zodat er een sell wordt gedaan of administratie wordt bijgewerkt.
                // Het triggeren van een stoploss of een DCA zal op een andere manier gedaan moeten worden (maar hoe en waar?)
                if (GlobalData.ExchangeListName.TryGetValue(ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
                {
                    if (exchange.SymbolListName.TryGetValue(data.Data.UpdateData.Symbol, out CryptoSymbol? symbol))
                    {
                        // Converteer de data naar een (tijdelijke) trade
                        CryptoOrder orderTemp = new();
                        Api.PickupOrder(GlobalData.ActiveAccount!, symbol, orderTemp, data.Data.UpdateData);

                        GlobalData.ThreadMonitorOrder?.AddToQueue((
                            symbol, 
                            //Api.LocalOrderType(data.Data.UpdateData.Type), 
                            //Api.LocalOrderSide(data.Data.UpdateData.Side), 
                            Api.LocalOrderStatus(data.Data.UpdateData.Status),
                            orderTemp));
                    }
                }
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"{ExchangeOptions.ExchangeName} ERROR: OrderUpdate " + error.ToString());
        }
    }


    //private void OnAccountPositionMessage(DataEvent<BinanceStreamPositionsUpdate> data)
    //{
    //    try
    //    {
    //        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
    //        {
    //            Api.PickupAssets(GlobalData.ActiveAccount, data.Data.Balances);
    //            GlobalData.AssetsHaveChanged("");
    //        }
    //    }
    //    catch (Exception error)
    //    {
    //        ScannerLog.Logger.Error(error, "");
    //        GlobalData.AddTextToLogTab("ERROR: AccountPositionMessage " + error.ToString());
    //    }
    //}


}

#endif