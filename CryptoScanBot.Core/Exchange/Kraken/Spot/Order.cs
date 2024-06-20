using CryptoScanBot.Core.Model;

using Kraken.Net.Objects.Models;

namespace CryptoScanBot.Core.Exchange.Kraken.Spot;

public class Order
{
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
        order.Side = Api.LocalOrderSide(item.OrderDetails.Side);

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
