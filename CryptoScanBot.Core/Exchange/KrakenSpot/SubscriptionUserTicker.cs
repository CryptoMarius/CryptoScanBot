using System.Text.Json;

using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using Kraken.Net.Clients;
using Kraken.Net.Enums;
using Kraken.Net.Objects.Models;

namespace CryptoScanBot.Core.Exchange.KrakenSpot;

#if TRADEBOT


public class SubscriptionUserTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    public override Task<CallResult<UpdateSubscription>>? Subscribe()
    {
        TickerGroup.SocketClient ??= new KrakenSocketClient();

        //Task<CallResult<UpdateSubscription>> SubscribeToOrderUpdatesAsync(string socketToken,
        //Action<DataEvent<IEnumerable<KrakenStreamOrder>>> handler,
        //CancellationToken ct = default(CancellationToken));

        // TODO:
        //var subscriptionResult = await ((KrakenSocketClient)TickerGroup.SocketClient).SpotApi.SubscribeToOrderUpdatesAsync(
        //    OnOrderUpdate, ExchangeHelper.CancellationToken).ConfigureAwait(false);
        return null; // subscriptionResult;
    }

    private void OnOrderUpdate(DataEvent<IEnumerable<KrakenStreamOrder>> dataList)
    {
        try
        {
            foreach (var data in dataList.Data)
            {
                string symbolName = data.OrderDetails.Symbol;
                symbolName = symbolName.Replace("/", "");


                // We melden voorlopig alles
                string info = $"{symbolName} UserTicker {data.OrderDetails.Side} {data.Status} order={data.Id} quantity={data.Quantity} price={data.Price} value={data.Price * data.QuantityFilled}";
                string text = JsonSerializer.Serialize(data, GlobalData.JsonSerializerIndented).Trim();
                GlobalData.AddTextToLogTab(info);
                ScannerLog.Logger.Trace($"{info} json={text}");

                // We zijn slechts geinteresseerd in een paar statussen (de andere zijn niet interessant voor de afhandeling van de order)
                if (data.Status == OrderStatus.Pending||
                    data.Status == OrderStatus.Open || //Active, not (fully) filled
                    data.Status == OrderStatus.Closed || // Fully filled
                    data.Status == OrderStatus.Canceled)
                {
                    if (GlobalData.ExchangeListName.TryGetValue(ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
                    {
                        if (exchange.SymbolListName.TryGetValue(symbolName, out CryptoSymbol? symbol))
                        {
                            // Converteer de data naar een (tijdelijke) trade
                            CryptoOrder order = new();
                            Api.PickupOrder(GlobalData.ExchangeRealTradeAccount!, symbol, order, data);

                            GlobalData.ThreadMonitorOrder?.AddToQueue((
                                symbol,
                                Api.LocalOrderType(data.OrderDetails.Type),
                                Api.LocalOrderSide(data.OrderDetails.Side),
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
