using System.Text.Encodings.Web;
using System.Text.Json;

using Bybit.Net.Clients;
using Bybit.Net.Enums.V5;

using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

namespace CryptoScanBot.Exchange.BybitFutures;
#if TRADEBOT
public class TickerUserItem(ExchangeOptions exchangeOptions) : TickerUserBase(exchangeOptions)
{
    private BybitSocketClient socketClient;
    private UpdateSubscription _subscription;

    public override async Task StopAsync()
    {
        if (_subscription == null)
            return;

        GlobalData.AddTextToLogTab($"{Api.ExchangeOptions.ExchangeName} Stopping user ticker");

        _subscription.Exception -= Exception;
        _subscription.ConnectionLost -= ConnectionLost;
        _subscription.ConnectionRestored -= ConnectionRestored;

        await socketClient.UnsubscribeAllAsync();
        return;
    }

    public override async Task StartAsync()
    {
        socketClient = new();

        var subscriptionResult = await socketClient.V5PrivateApi.SubscribeToOrderUpdatesAsync(OnOrderUpdate).ConfigureAwait(false);

        if (subscriptionResult.Success)
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
            GlobalData.AddTextToLogTab($"{Api.ExchangeOptions.ExchangeName} Error subscribing to userstream " + subscriptionResult.Error.Message);
            return;
        }
    }


    private void OnOrderUpdate(DataEvent<IEnumerable<Bybit.Net.Objects.Models.V5.BybitOrderUpdate>> dataList)
    {
        try
        {
            foreach (var data in dataList.Data)
            {
                // We krijgen duplicaat json berichten binnen (even een quick & dirty fix)
                string info = $"{data.Symbol} UserTicker {data.Side} {data.Status} order={data.OrderId} quantity={data.Quantity} price={data.Price} value={data.Price * data.QuantityFilled}";
                string text = JsonSerializer.Serialize(data, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = false }).Trim();
                GlobalData.AddTextToLogTab(info);
                ScannerLog.Logger.Trace($"{info} json={text}");

                // We zijn slechts geinteresseerd in 3 statussen (de andere zijn niet interessant voor de afhandeling van de order)
                if (data.Status == OrderStatus.New ||
                    data.Status == OrderStatus.Filled ||
                    data.Status == OrderStatus.PartiallyFilled || 
                    data.Status == OrderStatus.PartiallyFilledCanceled ||
                    data.Status == OrderStatus.Cancelled)
                {
                    if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeOptions.ExchangeName, out Model.CryptoExchange exchange))
                    {
                        if (exchange.SymbolListName.TryGetValue(data.Symbol, out CryptoSymbol symbol))
                        {
                            // Converteer de data naar een (tijdelijke) trade
                            CryptoOrder order = new();
                            Api.PickupOrder(GlobalData.ExchangeRealTradeAccount, symbol, order, data);

                            GlobalData.ThreadMonitorOrder.AddToQueue((
                                symbol,
                                Api.LocalOrderType(data.OrderType),
                                Api.LocalOrderSide(data.Side),
                                Api.LocalOrderStatus((OrderStatus)data.Status),
                                order));
                        }
                    }
                }
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"{Api.ExchangeOptions.ExchangeName} ERROR: OrderUpdate " + error.ToString());
        }
    }

}

#endif
