using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Exchange.Binance.Perpetual;

public class SubscriptionUserTicker(ExchangeOptions exchangeOptions) : Subscription(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        using BinanceRestClient client = new();
        {
            SubscriptionBundle!.SocketClient ??= new BinanceSocketClient();
            CallResult<string> userStreamResult = await client.SpotApi.Account.StartUserStreamAsync();
            //if (!userStreamResult.Success)
            //{
            //    GlobalData.AddErrorToLogTab($"{Api.ExchangeOptions.ExchangeName} - Error starting user stream: " + userStreamResult.Error.Message);
            //    return;
            //}


            var subscriptionResult = await ((BinanceSocketClient)SubscriptionBundle.SocketClient).UsdFuturesApi.Account.SubscribeToUserDataUpdatesAsync(
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
                if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
                {
                    // The exchange names the instrument, not the scanner. SymbolListExchangeName is keyed
                    // on exactly what arrives here.
                    if (exchange.SymbolListExchangeName.TryGetValue(data.Data.UpdateData.Symbol, out CryptoSymbol? symbol))
                    {
                        // Converteer de data naar een (tijdelijke) trade
                        CryptoOrder orderTemp = new()
                        {
                            Exchange = symbol.Exchange,
                            Symbol = symbol,
                        };
                        Order.PickupOrder(symbol, orderTemp, data.Data.UpdateData);

                        GlobalData.ThreadMonitorOrder?.AddToQueue((
                            symbol,
                            //Api.LocalOrderType(data.Data.UpdateData.Type), 
                            //Api.LocalOrderSide(data.Data.UpdateData.Side), 
                            Order.LocalOrderStatus(data.Data.UpdateData.Status),
                            orderTemp));
                    }
                }
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddErrorToLogTab($"{ExchangeOptions.ExchangeName} ERROR: OrderUpdate " + error.ToString());
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
    //        GlobalData.AddErrorToLogTab("ERROR: AccountPositionMessage " + error.ToString());
    //    }
    //}


}

