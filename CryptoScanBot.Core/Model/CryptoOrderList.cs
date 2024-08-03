using CryptoScanBot.Core.Intern;

namespace CryptoScanBot.Core.Model;

public class CryptoOrderList: SortedList<string, CryptoOrder>
{
    public void AddOrder(CryptoOrder order, bool log = true)
    {
        if (GlobalData.TradeAccountList.TryGetValue(order.TradeAccountId, out CryptoAccount? tradeAccount))
        {
            order.TradeAccount = tradeAccount;

            if (GlobalData.ExchangeListId.TryGetValue(order.ExchangeId, out Model.CryptoExchange? exchange))
            {
                order.Exchange = exchange;

                if (exchange.SymbolListId.TryGetValue(order.SymbolId, out CryptoSymbol? symbol))
                {
                    order.Symbol = symbol;

                    if (!ContainsKey(order.OrderId))
                    {
                        this.TryAdd(order.OrderId, order);
                        if (log)
                            GlobalData.AddTextToLogTab($"{order.Symbol.Name} added order {order.CreateTime} {order.OrderId} {order.Status} (#{order.Id})");
                    }

                }

            }
        }
    }

    public CryptoOrder? Find(string orderId)
    {
        if (TryGetValue(orderId, out CryptoOrder? order))
            return order;
        else
            return null;
    }

}
