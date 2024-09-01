using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Kraken.Net.Enums;
using Kraken.Net.Objects.Models;

namespace CryptoScanBot.Core.Exchange.Kraken.Spot;

public class Order
{
    // Converteer de orderstatus van Exchange naar "intern"
    public static CryptoOrderType LocalOrderType(OrderType orderType)
    {
        CryptoOrderType localOrderType = orderType switch
        {
            OrderType.Market => CryptoOrderType.Market,
            OrderType.Limit => CryptoOrderType.Limit,
            OrderType.StopLoss => CryptoOrderType.StopLimit,
            OrderType.StopLossLimit => CryptoOrderType.Oco,
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
    public static CryptoOrderStatus LocalOrderStatus(OrderStatus orderStatus)
    {
        CryptoOrderStatus localOrderStatus = orderStatus switch
        {
            OrderStatus.Pending => CryptoOrderStatus.New,
            OrderStatus.Open => CryptoOrderStatus.Filled,
            //OrderStatus.PartiallyFilled => CryptoOrderStatus.PartiallyFilled,
            //OrderStatus.Expired => CryptoOrderStatus.Expired,
            OrderStatus.Canceled => CryptoOrderStatus.Canceled,
            _ => throw new Exception("Niet ondersteunde orderstatus"),
        };

        return localOrderStatus;
    }



    static public void PickupOrder(CryptoAccount tradeAccount, CryptoSymbol symbol, CryptoOrder order, KrakenStreamOrder item)
    {
        order.CreateTime = item.CreateTime;
        order.UpdateTime = item.CreateTime; // TODO??? 

        order.TradeAccount = tradeAccount;
        order.TradeAccountId = tradeAccount.Id;
        order.Exchange = symbol.Exchange;
        order.ExchangeId = symbol.ExchangeId;
        order.Symbol = symbol;
        order.SymbolId = symbol.Id;

        order.OrderId = item.Id;
        order.Side = LocalOrderSide(item.OrderDetails.Side);

        order.Price = item.Price;
        order.Quantity = item.Quantity;
        order.QuoteQuantity = item.Price * item.Quantity;

        order.Price = item.Price;
        order.Quantity = item.Quantity;
        order.QuoteQuantity = item.Price * item.Quantity;

        order.Commission = item.Fee;
        order.CommissionAsset = symbol.Quote;
    }

}
