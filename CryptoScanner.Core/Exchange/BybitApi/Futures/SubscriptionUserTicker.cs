using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanner.Core.Core;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Model;

using System.Text.Json;

namespace CryptoScanner.Core.Exchange.BybitApi.Futures;

public class SubscriptionUserTicker(ExchangeOptions exchangeOptions) : Subscription(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        SubscriptionBundle!.SocketClient ??= new BybitSocketClient();
        var subscriptionResult = await ((BybitSocketClient)SubscriptionBundle.SocketClient).V5PrivateApi.SubscribeToOrderUpdatesAsync(OnOrderUpdate).ConfigureAwait(false);
        return subscriptionResult;
    }


    private void OnOrderUpdate(DataEvent<BybitOrderUpdate[]> dataList)
    {
        try
        {
            foreach (var data in dataList.Data)
            {
                // We krijgen duplicaat json berichten binnen (even een quick & dirty fix)
                string info = $"{data.Symbol} UserTicker {data.Side} {data.Status} order={data.OrderId} quantity={data.Quantity} price={data.Price} value={data.Price * data.QuantityFilled}";
                string text = JsonSerializer.Serialize(data, JsonTools.JsonSerializerNotIndented).Trim();
                GlobalData.AddTextToLogTab(info);
                ScannerLog.Logger.Trace($"{info} json={text}");

                // We zijn slechts geinteresseerd in 3 statussen (de andere zijn niet interessant voor de afhandeling van de order)
                if (data.Status == OrderStatus.New ||
                    data.Status == OrderStatus.Filled ||
                    data.Status == OrderStatus.PartiallyFilled ||
                    data.Status == OrderStatus.PartiallyFilledCanceled ||
                    data.Status == OrderStatus.Cancelled)
                {
                    if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
                    {
                        if (exchange.SymbolListName.TryGetValue(data.Symbol, out CryptoSymbol? symbol))
                        {
                            // Converteer de data naar een (tijdelijke) trade
                            CryptoOrder orderTemp = new()
                            {
                                Exchange = symbol.Exchange,
                                Symbol = symbol,
                            };
                            Order.PickupOrder(symbol, orderTemp, data);

                            GlobalData.ThreadMonitorOrder?.AddToQueue((
                                symbol,
                                //Api.LocalOrderType(data.OrderType),
                                //Api.LocalOrderSide(data.Side),
                                Order.LocalOrderStatus((OrderStatus)data.Status),
                                orderTemp));
                        }
                    }
                }
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} ERROR: OrderUpdate " + error.ToString());
        }
    }

}
