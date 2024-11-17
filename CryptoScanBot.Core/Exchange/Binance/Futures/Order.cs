using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures.Socket;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.Binance.Futures;

public class Order(ExchangeBase api) : OrderBase(api), IOrder
{
    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderType LocalOrderType(FuturesOrderType orderType)
    {
        CryptoOrderType localOrderType = orderType switch
        {
            FuturesOrderType.Market => CryptoOrderType.Market,
            FuturesOrderType.Limit => CryptoOrderType.Limit,
            //FuturesOrderType.StopLoss => CryptoOrderType.StopLimit,
            //FuturesOrderType.StopLossLimit => CryptoOrderType.Oco, // negatief gevuld (denk ik)
            //FuturesOrderType.LimitMaker => CryptoOrderType.Oco, // postief gevuld
            _ => throw new Exception("Niet ondersteunde ordertype " + orderType.ToString()),
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
            _ => throw new Exception("Niet ondersteunde orderside " + orderSide.ToString()),
        };

        return localOrderSide;
    }

    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderStatus LocalOrderStatus(OrderStatus orderStatus)
    {
        CryptoOrderStatus localOrderStatus = orderStatus switch
        {
            OrderStatus.New => CryptoOrderStatus.New,
            OrderStatus.Filled => CryptoOrderStatus.Filled,
            OrderStatus.PartiallyFilled => CryptoOrderStatus.PartiallyFilled,
            OrderStatus.Expired => CryptoOrderStatus.Expired,
            OrderStatus.Canceled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Niet ondersteunde orderstatus " + orderStatus.ToString()),
        };

        return localOrderStatus;
    }

    public static void PickupOrder(CryptoAccount tradeAccount, CryptoSymbol symbol, CryptoOrder order, BinanceFuturesStreamOrderUpdateData item)
    {
        order.CreateTime = item.UpdateTime; //  CreateTime; ????????????????
        order.UpdateTime = item.UpdateTime;

        order.TradeAccount = tradeAccount;
        order.TradeAccountId = tradeAccount.Id;
        order.Exchange = symbol.Exchange;
        order.ExchangeId = symbol.ExchangeId;
        order.Symbol = symbol;
        order.SymbolId = symbol.Id;

        order.OrderId = item.OrderId.ToString();
        order.Type = Order.LocalOrderType(item.Type);
        order.Side = Order.LocalOrderSide(item.Side);
        order.Status = Order.LocalOrderStatus(item.Status);

        order.Price = item.Price;
        order.Quantity = item.Quantity;
        order.QuoteQuantity = item.Price * item.Quantity;

        order.AveragePrice = item.Price;
        order.QuantityFilled = item.AccumulatedQuantityOfFilledTrades;
        order.QuoteQuantityFilled = item.Price * item.AccumulatedQuantityOfFilledTrades;

        order.Commission = item.Fee;
        order.CommissionAsset = item.FeeAsset;
    }

    public Task<int> GetOrdersAsync(CryptoDatabase database, CryptoPosition position)
    {
        return Task.FromResult<int>(0);
    }
}
