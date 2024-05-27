﻿using System.Text.Json;
using Bybit.Net.Clients;
using Bybit.Net.Enums.V5;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.BybitFutures;
#if TRADEBOT
public class SubscriptionUserTicker(ExchangeOptions exchangeOptions) : SubscriptionTicker(exchangeOptions)
{
    public override async Task<CallResult<UpdateSubscription>?> Subscribe()
    {
        TickerGroup!.SocketClient ??= new BybitSocketClient();
        var subscriptionResult = await ((BybitSocketClient)TickerGroup.SocketClient).V5PrivateApi.SubscribeToOrderUpdatesAsync(OnOrderUpdate).ConfigureAwait(false);
        return subscriptionResult;
    }


    private void OnOrderUpdate(DataEvent<IEnumerable<Bybit.Net.Objects.Models.V5.BybitOrderUpdate>> dataList)
    {
        try
        {
            foreach (var data in dataList.Data)
            {
                // We krijgen duplicaat json berichten binnen (even een quick & dirty fix)
                string info = $"{data.Symbol} UserTicker {data.Side} {data.Status} order={data.OrderId} quantity={data.Quantity} price={data.Price} value={data.Price * data.QuantityFilled}";
                string text = JsonSerializer.Serialize(data, ExchangeHelper.JsonSerializerNotIndented).Trim();
                GlobalData.AddTextToLogTab(info);
                ScannerLog.Logger.Trace($"{info} json={text}");

                // We zijn slechts geinteresseerd in 3 statussen (de andere zijn niet interessant voor de afhandeling van de order)
                if (data.Status == OrderStatus.New ||
                    data.Status == OrderStatus.Filled ||
                    data.Status == OrderStatus.PartiallyFilled || 
                    data.Status == OrderStatus.PartiallyFilledCanceled ||
                    data.Status == OrderStatus.Cancelled)
                {
                    if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeOptions.ExchangeName, out Model.CryptoExchange? exchange))
                    {
                        if (exchange.SymbolListName.TryGetValue(data.Symbol, out CryptoSymbol? symbol))
                        {
                            // Converteer de data naar een (tijdelijke) trade
                            CryptoOrder orderTemp = new();
                            Api.PickupOrder(GlobalData.ExchangeRealTradeAccount!, symbol, orderTemp, data);

                            GlobalData.ThreadMonitorOrder?.AddToQueue((
                                symbol,
                                //Api.LocalOrderType(data.OrderType),
                                //Api.LocalOrderSide(data.Side),
                                Api.LocalOrderStatus((OrderStatus)data.Status),
                                orderTemp));
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