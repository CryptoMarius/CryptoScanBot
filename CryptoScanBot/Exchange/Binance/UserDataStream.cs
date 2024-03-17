using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Spot.Socket;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;

using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange.Binance;

#if TRADEBOT
public class UserDataStream
{
    private BinanceSocketClient socketClient;
    private UpdateSubscription _subscription;

    public async Task StopAsync()
    {
        if (_subscription == null)
            return; // Task.CompletedTask;

        GlobalData.AddTextToLogTab("Stopping user stream");

        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;

        await socketClient.UnsubscribeAllAsync();
        return; // Task.CompletedTask;
    }

    public async Task ExecuteAsync()
    {
        using BinanceRestClient client = new();
        {
            CallResult<string> userStreamResult = await client.SpotApi.Account.StartUserStreamAsync();
            if (!userStreamResult.Success)
            {
                GlobalData.AddTextToLogTab($"{Api.ExchangeName} - Error starting user stream: " + userStreamResult.Error.Message);
                return;
            }


            socketClient = new BinanceSocketClient();
            var subscriptionResult = await socketClient.SpotApi.Account.SubscribeToUserDataUpdatesAsync(
                userStreamResult.Data,
                OnOrderUpdate,
                null,
                OnAccountPositionMessage,
                null
                ).ConfigureAwait(false);

            // Subscribe to network-related stuff
            if (userStreamResult.Success)
            {
                _subscription = subscriptionResult.Data;

                // Events
                _subscription.Exception += Exception;
                _subscription.ConnectionLost += ConnectionLost;
                _subscription.ConnectionRestored += ConnectionRestored;
                return;
            }
            else
            {
                GlobalData.AddTextToLogTab("Error subscribing to spot.userstream " + subscriptionResult.Error.Message);
                return;
            }
        }

    }

    private void OnOrderUpdate(DataEvent<BinanceStreamOrderUpdate> data) //DataEvent<BinanceStreamOrderUpdate> data
    {
        try
        {
            // We zijn slechts geinteresseerd in 3 statussen (de andere zijn niet interessant voor de afhandeling van de order)
            if (data.Data.Status == OrderStatus.New ||
                data.Data.Status == OrderStatus.Filled ||
                data.Data.Status == OrderStatus.PartiallyFilled || 
                data.Data.Status == OrderStatus.Canceled)
            {
                // Nieuwe thread opstarten en de data meegeven zodat er een sell wordt gedaan of administratie wordt bijgewerkt.
                // Het triggeren van een stoploss of een DCA zal op een andere manier gedaan moeten worden (maar hoe en waar?)
                if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
                {
                    if (exchange.SymbolListName.TryGetValue(data.Data.Symbol, out CryptoSymbol symbol))
                    {
                        // Converteer de data naar een (tijdelijke) trade
                        CryptoOrder orderTemp = new();
                        Api.PickupOrder(GlobalData.ExchangeRealTradeAccount, symbol, orderTemp, data.Data);

                        GlobalData.ThreadMonitorOrder.AddToQueue((
                            symbol, 
                            Api.LocalOrderType(data.Data.Type), 
                            Api.LocalOrderSide(data.Data.Side), 
                            Api.LocalOrderStatus(data.Data.Status),
                            orderTemp));
                    }
                }
            }

            // Converteer de data naar een (tijdelijke) trade
            //BinanceApi.PickupOrder(trade, data.Data);
            //GlobalData.ThreadMonitorOrder.AddToQueue(data.Data);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR: OrderUpdate " + error.ToString());
        }
    }

    //private void onOcoOrderUpdateMessage(BinanceStreamOrderList data)
    //{
    //    try
    //    {
    //        Exchange exchange;
    //        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out exchange))
    //        {
    //            Symbol symbol;
    //            if (exchange.SymbolListName.TryGetValue(data.Symbol, out symbol))
    //            {
    //                // Oppassen, bij een OCO wordt ook de OnOrderUpdate aangeroepen (via 2 aparte orders, dus wellicht overbodig?)
    //                MonitorAlgorithm monitorAlgorithm = new MonitorAlgorithm();
    //                monitorAlgorithm.HandleTrade(symbol, data);
    //            }
    //        }
    //    }
    //    catch (Exception error)
    //    {
    //        GlobalData.AddTextToLogTab("ERROR: OcoOrderUpdateMessage " + error.ToString());
    //    }
    //}

    // deze was deprecated en is ondertussen vervallen
    //static private void OnAccountUpdate(BinanceStreamAccountInfo data)
    //{
    //    try                                
    //    {
    //        Exchange exchange = null;
    //        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out exchange))
    //        {
    //            BinanceTools.PickupAssets(exchange, data.Balances);
    //            GlobalData.AssetsHaveChanged("");
    //        }
    //    }
    //    catch (Exception error)
    //    {
    //        GlobalData.AddTextToLogTab("ERROR: AccountUpdate " + error.ToString());
    //    }
    //}

    private void OnAccountPositionMessage(DataEvent<BinanceStreamPositionsUpdate> data)
    {
        try
        {
            if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
            {
                Api.PickupAssets(GlobalData.ExchangeRealTradeAccount, data.Data.Balances);
                GlobalData.AssetsHaveChanged("");
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR: AccountPositionMessage " + error.ToString());
        }
    }


    //private void OnAccountBalanceUpdate(BinanceStreamBalanceUpdate data)
    //{
    //    // Dit rapporteert het verschil, deze staat (nu) niet aan..
    //    try
    //    {
    //        GlobalData.AddTextToLogTab(string.Format("AccountBalanceUpdate {0} usdt={1} free={2}", data.Event.ToString(), data.Asset, data.BalanceDelta));
    //    }
    //    catch (Exception error)
    //    {
    //        ScannerLog.Logger.Error(error, "");
    //        GlobalData.AddTextToLogTab("ERROR: AccountBalanceUpdate " + error.ToString());
    //    }
    //}


    private void ConnectionLost()
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} price ticker connection lost.");
    }

    private void ConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} price ticker connection restored.");
    }

    private void Exception(Exception ex)
    {
        GlobalData.AddTextToLogTab($"{Api.ExchangeName} price ticker connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }
}

#endif