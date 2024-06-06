using CryptoScanBot.Core.Intern;

namespace CryptoScanBot.Core.Model;

public class CryptoOrderList: SortedList<string, SortedList<string, CryptoOrder>>
{
    // First key=symbolname
    // Second key=OrderId

    public bool HasSymbol(string symbolName)
    {
        return ContainsKey(symbolName);
    }

    private bool AddOrderInternal(CryptoOrder order)
    {
        if (!TryGetValue(order.Symbol.Name, out SortedList<string, CryptoOrder>? list))
        {
            list = [];
            Add(order.Symbol.Name, list);
        }

        if (!list.ContainsKey(order.OrderId))
        {
            list.Add(order.OrderId, order);
            return true;
        }

        return false;
    }

    public void Add(CryptoOrder order, bool log = true)
    {
        if (GlobalData.TradeAccountList.TryGetValue(order.TradeAccountId, out CryptoTradeAccount? tradeAccount))
        {
            order.TradeAccount = tradeAccount;

            if (GlobalData.ExchangeListId.TryGetValue(order.ExchangeId, out Model.CryptoExchange? exchange))
            {
                order.Exchange = exchange;

                if (exchange.SymbolListId.TryGetValue(order.SymbolId, out CryptoSymbol? symbol))
                {
                    order.Symbol = symbol;

                    if (AddOrderInternal(order) && log)
                        GlobalData.AddTextToLogTab($"{order.Symbol.Name} added order {order.CreateTime} {order.OrderId} {order.Status} (#{order.Id})");
                }

            }
        }
    }

    public CryptoOrder? Find(CryptoSymbol symbol, string orderId)
    {
        if (!TryGetValue(symbol.Name, out SortedList<string, CryptoOrder>? list))
            return null;

        if (list.TryGetValue(orderId, out CryptoOrder? order))
            return order;
        
        return null;
    }

    public void ClearOrdersFromSymbol(CryptoSymbol symbol)
    {
        if (ContainsKey(symbol.Name))
            Remove(symbol.Name);
    }

    public SortedList<string, CryptoOrder> GetOrdersForSymbol(CryptoSymbol symbol)
    {
        if (TryGetValue(symbol.Name, out SortedList<string, CryptoOrder>? list))
            return list!;

        return [];
    }
}
