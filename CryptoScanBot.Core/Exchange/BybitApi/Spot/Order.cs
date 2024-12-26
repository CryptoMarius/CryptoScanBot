using Bybit.Net.Clients;
using Bybit.Net.Enums;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Json;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

using System.Text.Json;

namespace CryptoScanBot.Core.Exchange.BybitApi.Spot;

public class Order() : OrderBase(), IOrder
{
    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderType LocalOrderType(OrderType orderType)
    {
        CryptoOrderType localOrderType = orderType switch
        {
            OrderType.Market => CryptoOrderType.Market,
            OrderType.Limit => CryptoOrderType.Limit,
            OrderType.LimitMaker => CryptoOrderType.StopLimit, /// ????????????????????????????????????????????????
            _ => throw new Exception("Niet ondersteunde ordertype"),
        };

        return localOrderType;
    }

    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderSide LocalOrderSide(OrderSide orderSide)
    {
        CryptoOrderSide localOrderSide = orderSide switch
        {
            OrderSide.Buy => CryptoOrderSide.Buy,
            OrderSide.Sell => CryptoOrderSide.Sell,
            _ => throw new Exception("Niet ondersteunde orderside"),
        };

        return localOrderSide;
    }


    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderStatus LocalOrderStatus(Bybit.Net.Enums.V5.OrderStatus orderStatus)
    {
        CryptoOrderStatus localOrderStatus = orderStatus switch
        {
            Bybit.Net.Enums.V5.OrderStatus.New => CryptoOrderStatus.New,
            Bybit.Net.Enums.V5.OrderStatus.Filled => CryptoOrderStatus.Filled,
            Bybit.Net.Enums.V5.OrderStatus.PartiallyFilled => CryptoOrderStatus.PartiallyFilled,
            Bybit.Net.Enums.V5.OrderStatus.PartiallyFilledCanceled => CryptoOrderStatus.PartiallyAndClosed, // niet alles kon omgezet worden, iets minder gekregen
            //Bybit.Net.Enums.V5.OrderStatus.Expired => CryptoOrderStatus.Expired,
            Bybit.Net.Enums.V5.OrderStatus.Cancelled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Niet ondersteunde orderstatus"),
        };

        return localOrderStatus;
    }


    public static void PickupOrder(CryptoAccount tradeAccount, CryptoSymbol symbol, CryptoOrder order, Bybit.Net.Objects.Models.V5.BybitOrder item)
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
        order.Side = Order.LocalOrderSide(item.Side);
        order.Type = Order.LocalOrderType(item.OrderType);
        order.Status = Order.LocalOrderStatus(item.Status);

        // Bij een marketorder is deze niet gevuld
        // alsnog vullen (zodat de QuoteQuantity gevuld wordt..)
        if (item.Price != 0)
            order.Price = item.Price;
        else
            order.Price = item.AveragePrice;
        order.Quantity = item.Quantity;
        // Bij deze status wordt het aangevraagde hoeveelheid niet goed ingevuld
        if (item.Status == Bybit.Net.Enums.V5.OrderStatus.PartiallyFilledCanceled && item.QuantityFilled.HasValue)
            order.Quantity = item.QuantityFilled.Value;
        if (order.Price.HasValue)
            order.QuoteQuantity = order.Price.Value * order.Quantity;
        else
            order.QuoteQuantity = 0;


        // Filled
        if (item.AveragePrice.HasValue)
            order.AveragePrice = item.AveragePrice;
        else
            order.AveragePrice = 0;

        if (item.QuantityFilled.HasValue)
            order.QuantityFilled = item.QuantityFilled;
        else
            order.QuantityFilled = 0;
        order.QuoteQuantityFilled = order.Price * item.QuantityFilled;


        // Bybit spot does currently not return any info on fees
        order.Commission = 0; // item.ExecutedFee;
        order.CommissionAsset = ""; //  item.FeeAsset;
    }


    public async Task<int> GetOrdersAsync(CryptoDatabase database, CryptoPosition position)
    {
        //ScannerLog.Logger.Trace($"Exchange.BybitSpot.GetOrdersForPositionAsync: Positie {position.Symbol.Name}");
        // Behoorlijk weinig error control ...... 

        int count = 0;
        DateTime? from; // = position.Symbol.LastOrderFetched;
        //if (from == null)
        // altijd alles ophalen, geeft wat veel logging, maar ach..
        from = position.CreateTime.AddMinutes(-1);

        //GlobalData.AddTextToLogTab($"GetOrdersForPositionAsync {position.Symbol.Name} fetching orders from exchange {from}");
        ScannerLog.Logger.Trace($"GetOrdersForPositionAsync {position.Symbol.Name} fetching orders from exchange {from}");

        using var client = new BybitRestClient();
        var info = await client.V5Api.Trading.GetOrderHistoryAsync(Category.Spot, symbol: position.Symbol.Name, startTime: from);
        if (info.Success && info.Data != null)
        {
            //foreach (var item in info.Data.List)
            //{
            //    // problems... exchange geeft meer orders terug dan verwacht
            //    if (item.CreateTime < position.CreateTime)
            //        continue;
            //    GlobalData.AddTextToLogTab($"{item.Symbol} order {item.CreateTime} {item.OrderId} fetched from exchange");
            //}

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
                        Order.PickupOrder(position.Account, position.Symbol, order, item);
                        database.Connection.Update(order);

                        if (oldStatus != order.Status || oldQuoteQuantityFilled != order.QuoteQuantityFilled)
                        {
                            ScannerLog.Logger.Trace($"GetOrdersForPositionAsync {position.Symbol.Name} updated order {item.OrderId}");
                            text = JsonSerializer.Serialize(item, JsonTools.JsonSerializerNotIndented).Trim();
                            ScannerLog.Logger.Trace($"{item.Symbol} order updated json={text}");
                            count++;
                        }
                    }
                    else
                    {
                        text = JsonSerializer.Serialize(item, JsonTools.JsonSerializerNotIndented).Trim();
                        ScannerLog.Logger.Trace($"{item.Symbol} order added json={text}");

                        order = new()
                        {
                            TradeAccount = GlobalData.ActiveAccount!,
                            Exchange = position.Exchange,
                            Symbol = position.Symbol,
                        };

                        Order.PickupOrder(position.Account, position.Symbol, order, item);
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
            GlobalData.AddTextToLogTab($"Error reading orders from {ExchangeBase.ExchangeOptions.ExchangeName} for {position.Symbol.Name} {info.Error}");

        return count;
    }


    //public async Task<WebCallResult<BybitResponse<Bybit.Net.Objects.Models.V5.BybitOrder>>> GetOrderHistoryAsync(
    //    Category category,
    //    string? symbol = null,
    //    string? baseAsset = null,
    //    string? orderId = null,
    //    string? clientOrderId = null,
    //    OrderStatus? status = null,
    //    OrderFilter? orderFilter = null,
    //    DateTime? startTime = null,
    //    DateTime? endTime = null,
    //    int? limit = null,
    //    string? cursor = null,
    //    CancellationToken ct = default)
    //{
    //    var parameters = new Dictionary<string, object>()
    //        {
    //            { "category", EnumConverter.GetString(category) }
    //        };

    //    parameters.AddOptionalParameter("symbol", symbol);
    //    parameters.AddOptionalParameter("baseCoin", baseAsset);
    //    parameters.AddOptionalParameter("orderId", orderId);
    //    parameters.AddOptionalParameter("orderLinkId", clientOrderId);
    //    parameters.AddOptionalParameter("orderFilter", EnumConverter.GetString(orderFilter));
    //    parameters.AddOptionalParameter("orderStatus", EnumConverter.GetString(status));
    //    parameters.AddOptionalParameter("startTime", DateTimeConverter.ConvertToMilliseconds(startTime));
    //    parameters.AddOptionalParameter("endTime", DateTimeConverter.ConvertToMilliseconds(endTime));
    //    parameters.AddOptionalParameter("limit", limit);
    //    parameters.AddOptionalParameter("cursor", cursor);

    //    using var client = new BybitRestClient();

    //    Deze routine is internal, kom ik niet bij voor zover ik dat kan zien..

    //    return await client.V5Api.Trading.SendRequestAsync<BybitResponse<Bybit.Net.Objects.Models.V5.BybitOrder>>(
    //        client.V5Api.Trading.GetUrl("v5/order/History"), HttpMethod.Get, ct, parameters, true).ConfigureAwait(false);

    //    // private readonly BybitRestClientApi _baseClient;
    //}

}
