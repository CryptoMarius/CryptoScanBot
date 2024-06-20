using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

using System.Text.Json;

namespace CryptoScanBot.Core.Exchange.BybitApi.Futures;

public class Order
{

    public static void PickupOrder(CryptoAccount tradeAccount, CryptoSymbol symbol, CryptoOrder order, Bybit.Net.Objects.Models.V5.BybitOrderUpdate item)
    {
        order.CreateTime = item.CreateTime;
        order.UpdateTime = item.UpdateTime;

        order.TradeAccount = tradeAccount;
        order.TradeAccountId = tradeAccount.Id;
        order.Exchange = symbol.Exchange;
        order.ExchangeId = symbol.ExchangeId;
        order.Symbol = symbol;
        order.SymbolId = symbol.Id;

        order.OrderId = item.OrderId;
        order.Type = Api.LocalOrderType(item.OrderType);
        order.Side = Api.LocalOrderSide(item.Side);
        order.Status = Api.LocalOrderStatus(item.Status);

        order.Price = item.Price ?? 0;
        order.Quantity = item.Quantity;
        order.QuoteQuantity = item.Price ?? 0 * item.Quantity;

        order.AveragePrice = item.AveragePrice;
        order.QuantityFilled = item.QuantityFilled;
        order.QuoteQuantityFilled = item.AveragePrice * item.QuantityFilled;

        order.Commission = item.ExecutedFee ?? 0;
        order.CommissionAsset = item.FeeAsset ?? "";
    }

    
    public static async Task<int> GetOrdersAsync(CryptoDatabase database, CryptoPosition position)
    {
        //ScannerLog.Logger.Trace($"Exchange.BybitSpot.GetOrdersForPositionAsync: Positie {position.Symbol.Name}");
        // Behoorlijk weinig error control ...... 

        int count = 0;
        DateTime? from; // = position.Symbol.LastOrderFetched;
        //if (from == null)
        // altijd alles ophalen, geeft wat veel logging, maar ach..
        from = position.CreateTime.AddMinutes(-1);

        ScannerLog.Logger.Trace($"GetOrdersForPositionAsync {position.Symbol.Name} fetching orders from exchange {from}");

        using var client = new BybitRestClient();
        var info = await client.V5Api.Trading.GetOrderHistoryAsync(
            Category.Spot, symbol: position.Symbol.Name,
            startTime: from);


        if (info.Success && info.Data != null)
        {
            string text;
            foreach (var item in info.Data.List)
            {
                if (position.StepOrderList.ContainsKey(item.OrderId))
                {
                    CryptoOrder? order = position.OrderList.Find(item.OrderId);
                    if (order != null)
                    {
                        var oldStatus = order.Status;
                        var oldQuoteQuantityFilled = order.QuoteQuantityFilled;
                        PickupOrder(position.Account, position.Symbol, order, (BybitOrderUpdate)item);
                        database.Connection.Update(order);

                        if (oldStatus != order.Status || oldQuoteQuantityFilled != order.QuoteQuantityFilled)
                        {
                            ScannerLog.Logger.Trace($"GetOrdersForPositionAsync {position.Symbol.Name} updated order {item.OrderId}");
                            text = JsonSerializer.Serialize(item, ExchangeHelper.JsonSerializerNotIndented).Trim();
                            ScannerLog.Logger.Trace($"{item.Symbol} order updated json={text}");
                            count++;
                        }
                    }
                    else
                    {
                        text = JsonSerializer.Serialize(item, ExchangeHelper.JsonSerializerNotIndented).Trim();
                        ScannerLog.Logger.Trace($"{item.Symbol} order added json={text}");

                        order = new();
                        PickupOrder(position.Account, position.Symbol, order, (BybitOrderUpdate)item);
                        database.Connection.Insert(order);
                        position.OrderList.AddOrder(order);
                        count++;
                    }

                    //if (position.Symbol.LastOrderFetched == null || order.CreateTime > position.Symbol.LastOrderFetched)
                    //    position.Symbol.LastOrderFetched = order.CreateTime;
                }
            }
            database.Connection.Update(position.Symbol);
        }
        else
            GlobalData.AddTextToLogTab($"Error reading orders from {Api.ExchangeOptions.ExchangeName} for {position.Symbol.Name} {info.Error}");

        return count;
    }
}
