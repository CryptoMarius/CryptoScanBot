using CryptoScanner.Core.Enums;

using Mexc.Net.Enums;

namespace CryptoScanner.Core.Exchange.Mexc.Spot;

// The converters below used to sit in Api.cs. They live here because that is where the other
// exchanges keep them. Nothing calls them yet: Mexc has no user ticker, order or trade
// implementation, so there is no order coming back from the exchange to convert.
public class Order() : OrderBase()
{
    // Convert the order type from the exchange to "internal"
    public static CryptoOrderType LocalOrderType(OrderType orderType)
    {
        CryptoOrderType localOrderType = orderType switch
        {
            OrderType.Market => CryptoOrderType.Market,
            OrderType.Limit => CryptoOrderType.Limit,
            OrderType.LimitMaker => CryptoOrderType.StopLimit, /// ????????????????????????????????????????????????
            _ => throw new Exception("Unsupported order type"),
        };

        return localOrderType;
    }

    // Convert the order side from the exchange to "internal"
    public static CryptoOrderSide LocalOrderSide(OrderSide orderSide)
    {
        CryptoOrderSide localOrderSide = orderSide switch
        {
            OrderSide.Buy => CryptoOrderSide.Buy,
            OrderSide.Sell => CryptoOrderSide.Sell,
            _ => throw new Exception("Unsupported order side"),
        };

        return localOrderSide;
    }


    // Convert the order status from the exchange to "internal"
    public static CryptoOrderStatus LocalOrderStatus(OrderStatus orderStatus)
    {
        CryptoOrderStatus localOrderStatus = orderStatus switch
        {
            OrderStatus.New => CryptoOrderStatus.New,
            OrderStatus.Filled => CryptoOrderStatus.Filled,
            OrderStatus.PartiallyFilled => CryptoOrderStatus.PartiallyFilled,
            OrderStatus.PartiallyCanceled => CryptoOrderStatus.PartiallyAndClosed, // not everything could be converted, received slightly less
            OrderStatus.Canceled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Unsupported order status"),
        };

        return localOrderStatus;
    }
}
