using Binance.Net.Clients;
using Binance.Net.Enums;
using Binance.Net.Objects.Models.Futures;
using Binance.Net.Objects.Models.Futures.Socket;
//using Binance.Net.Objects.Models.Spot;
//using Binance.Net.Objects.Models.Spot.Socket;

using CryptoExchange.Net.Authentication;
using CryptoExchange.Net.Objects;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Exchange.Binance.Futures;

internal class Order
{
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
        order.Type = Api.LocalOrderType(item.Type);
        order.Side = Api.LocalOrderSide(item.Side);
        order.Status = Api.LocalOrderStatus(item.Status);

        order.Price = item.Price;
        order.Quantity = item.Quantity;
        order.QuoteQuantity = item.Price * item.Quantity;

        order.AveragePrice = item.Price;
        order.QuantityFilled = item.AccumulatedQuantityOfFilledTrades;
        order.QuoteQuantityFilled = item.Price * item.AccumulatedQuantityOfFilledTrades;

        order.Commission = item.Fee;
        order.CommissionAsset = item.FeeAsset;
    }

}
