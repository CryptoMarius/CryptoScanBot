using System.Text.Json;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;

using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Json;
using CryptoScanBot.Core.Model;

using Kraken.Net.Clients;
using Kraken.Net.Enums;
using Kraken.Net.Objects.Models;
using Kraken.Net.Objects.Models.Socket;

namespace CryptoScanBot.Core.Exchange.Kraken.Spot;

public class SubscriptionUserTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        TickerGroup!.SocketClient ??= new KrakenSocketClient();

        WebCallResult<KrakenWebSocketToken> tokenResult = await new KrakenRestClient().SpotApi.Account.GetWebsocketTokenAsync();
        if (!tokenResult.Success)
            return default;

        CallResult<UpdateSubscription> subscriptionResult = await ((KrakenSocketClient)TickerGroup.SocketClient).SpotApi.SubscribeToOrderUpdatesAsync( //tokenResult.Data.Token,
            OnOrderUpdate, null, null, ExchangeBase.CancellationToken).ConfigureAwait(false);

        return subscriptionResult;
    }
    //Task<CallResult<UpdateSubscription>> SubscribeToOrderUpdatesAsync(
    //    Action<DataEvent<IEnumerable<KrakenOrderUpdate>>> updateHandler,
    //    bool? snapshotOrder = null,
    //    bool? snapshotTrades = null,
    //    CancellationToken ct = default);

    private void OnOrderUpdate(DataEvent<IEnumerable<KrakenOrderUpdate>> dataList)
    {
        try
        {
            foreach (var data in dataList.Data)
            {
                string symbolName = data.Symbol;
                symbolName = symbolName.Replace("/", "");


                // We melden voorlopig alles
                string info = $"{symbolName} UserTicker {data.OrderSide} {data.OrderStatus} order={data.OrderId} quantity={data.OrderQuantity} price={data.LimitPrice} value={data.LimitPrice * data.QuantityFilled}";
                string text = JsonSerializer.Serialize(data, JsonTools.JsonSerializerIndented).Trim();
                GlobalData.AddTextToLogTab(info);
                ScannerLog.Logger.Trace($"{info} json={text}");

                // We zijn slechts geinteresseerd in een paar statussen (de andere zijn niet interessant voor de afhandeling van de order)
                if (data.OrderStatus == OrderStatusUpdate.Pending ||
                    data.OrderStatus == OrderStatusUpdate.PartiallyFilled || //Active, not (fully) filled
                    data.OrderStatus == OrderStatusUpdate.Filled || // Fully filled
                    data.OrderStatus == OrderStatusUpdate.Canceled)
                {
                    if (GlobalData.ExchangeListName.TryGetValue(ExchangeBase.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
                    {
                        if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                        {
                            // Converteer de data naar een (tijdelijke) trade
                            CryptoOrder orderTemp = new()
                            {
                                TradeAccount = GlobalData.ActiveAccount!,
                                Exchange = symbol.Exchange,
                                Symbol = symbol,
                            };
                            Order.PickupOrder(GlobalData.ActiveAccount!, symbol, orderTemp, data);

                            GlobalData.ThreadMonitorOrder?.AddToQueue((
                                symbol,
                                //Api.LocalOrderType(data.OrderDetails.Type),
                                //Api.LocalOrderSide(data.OrderDetails.Side),
                                Order.LocalOrderStatus((OrderStatus)data.OrderStatus),
                                orderTemp));
                        }
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
}

