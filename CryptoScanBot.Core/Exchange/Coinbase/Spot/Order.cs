using Coinbase.Net.Enums;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Exchange.Coinbase.Spot;

public class Order() : OrderBase(), IOrder
{
    public static CryptoOrderType LocalOrderType(OrderType orderType)
    {
        throw new NotImplementedException();
    }

    public static CryptoOrderSide LocalOrderSide(OrderSide orderSide)
    {
        throw new NotImplementedException();
    }


    public static CryptoOrderStatus LocalOrderStatus(OrderStatus orderStatus)
    {
        throw new NotImplementedException();
    }


    public static void PickupOrder(CryptoAccount tradeAccount, CryptoSymbol symbol, CryptoOrder order, OrderStatus item)
    {
        throw new NotImplementedException();
    }

    Task<int> IOrder.GetOrders(CryptoDatabase database, CryptoPosition position)
    {
        throw new NotImplementedException();
    }
}
