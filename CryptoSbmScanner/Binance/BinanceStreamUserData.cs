using Binance.Net.Clients;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using CryptoSbmScanner.Intern;

namespace CryptoSbmScanner.Binance;

//Wil dit graag per quote opdelen, of in een aantal verschilende stream's zodat we de candles 
//eerder krijgen (nu zit er nog wel eens een aardige delay van ~15 a 20 seconden in)

public class BinanceStreamUserData
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
        using (var client = new BinanceClient())
        {
            CallResult<string> userStreamResult = await client.SpotApi.Account.StartUserStreamAsync();
            if (!userStreamResult.Success)
            {
                GlobalData.AddTextToLogTab("Error starting user stream: " + userStreamResult.Error.Message);
                return;
            }


            socketClient = new BinanceSocketClient();
            var subscriptionResult = await socketClient.SpotStreams.SubscribeToUserDataUpdatesAsync(
                userStreamResult.Data,
                OnOrderUpdate,
                null,
                onAccountPositionMessage,
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
            GlobalData.ThreadMonitorOrder.AddToQueue(data.Data);
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("ERROR: OrderUpdate " + error.ToString());
        }
    }

    //private void onOcoOrderUpdateMessage(BinanceStreamOrderList data)
    //{
    //    try
    //    {
    //        Exchange exchange;
    //        if (GlobalData.ExchangeListName.TryGetValue("Binance", out exchange))
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
    //        if (GlobalData.ExchangeListName.TryGetValue("Binance", out exchange))
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

    private void onAccountPositionMessage(DataEvent<BinanceStreamPositionsUpdate> data)
    {
        try
        {
            if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
            {
                Helper.PickupAssets(exchange, data.Data.Balances);
                GlobalData.AssetsHaveChanged("");
            }
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("ERROR: AccountPositionMessage " + error.ToString());
        }
    }


    //private void onAccountBalanceUpdate(BinanceStreamBalanceUpdate data)
    //{
    //    // Dit rapporteert het verschil, deze staat (nu) niet aan..
    //    try
    //    {
    //        GlobalData.AddTextToLogTab(string.Format("AccountBalanceUpdate {0} usdt={1} free={2}", data.Event.ToString(), data.Asset, data.BalanceDelta));
    //    }
    //    catch (Exception error)
    //    {
    //        GlobalData.Logger.Error(error);
    //        GlobalData.AddTextToLogTab("ERROR: AccountBalanceUpdate " + error.ToString());
    //    }
    //}


    private void ConnectionLost()
    {
        GlobalData.AddTextToLogTab("Binance price ticker connection lost.");
    }

    private void ConnectionRestored(TimeSpan timeSpan)
    {
        GlobalData.AddTextToLogTab("Binance price ticker connection restored.");
    }

    private void Exception(Exception ex)
    {
        GlobalData.AddTextToLogTab($"Binance price ticker connection error {ex.Message} | Stack trace: {ex.StackTrace}");
    }
}


