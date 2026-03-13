using CryptoScanner.Core.Core;

namespace CryptoScanner.Core.Model;

public class CryptoTradeList : SortedList<string, CryptoTrade>
{
    public void AddTrade(CryptoTrade trade, bool log = true)
    {
        if (GlobalData.ExchangeListId.TryGetValue(trade.ExchangeId, out Model.CryptoExchange? exchange))
        {
            trade.Exchange = exchange;

            if (exchange.SymbolListId.TryGetValue(trade.SymbolId, out CryptoSymbol? symbol))
            {
                trade.Symbol = symbol;

                if (!ContainsKey(trade.TradeId))
                {
                    Add(trade.TradeId, trade);
                    if (log)
                        GlobalData.AddTextToLogTab($"{trade.Symbol.Name} {trade.TradeTime} orderid={trade.OrderId} added trade.id={trade.Id} trade.TradeId={trade.TradeId}");
                }
            }

        }
    }

    public CryptoTrade? Find(string tradeId)
    {
        if (TryGetValue(tradeId, out CryptoTrade? trade))
            return trade!;
        else
            return null;
    }

}
