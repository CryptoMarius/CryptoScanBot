using CryptoScanBot.Core.Intern;

namespace CryptoScanBot.Core.Model;

public class CryptoTradeList : SortedList<string, SortedList<string, CryptoTrade>>
{
    // First key=symbolname
    // Second key=TradeId

    public bool HasSymbol(string symbolName)
    {
        return ContainsKey(symbolName);
    }

    private bool AddTradeInternal(CryptoTrade trade)
    {
        if (!TryGetValue(trade.Symbol.Name, out SortedList<string, CryptoTrade>? list))
        {
            list = [];
            Add(trade.Symbol.Name, list);
        }

        if (!list.ContainsKey(trade.OrderId))
        {
            list.Add(trade.OrderId, trade);
            return true;
        }

        return false;
    }

    public void Add(CryptoTrade trade, bool log = true)
    {
        if (GlobalData.TradeAccountList.TryGetValue(trade.TradeAccountId, out CryptoTradeAccount? tradeAccount))
        {
            trade.TradeAccount = tradeAccount;

            if (GlobalData.ExchangeListId.TryGetValue(trade.ExchangeId, out Model.CryptoExchange? exchange))
            {
                trade.Exchange = exchange;

                if (exchange.SymbolListId.TryGetValue(trade.SymbolId, out CryptoSymbol? symbol))
                {
                    trade.Symbol = symbol;

                    if (AddTradeInternal(trade) && log)
                        GlobalData.AddTextToLogTab($"{trade.Symbol.Name} order {trade.TradeTime} {trade.OrderId} added trade {trade.TradeId} (#{trade.Id})");
                }

            }
        }
    }

    public CryptoTrade? Find(CryptoSymbol symbol, string tradeId)
    {
        if (!TryGetValue(symbol.Name, out SortedList<string, CryptoTrade>? list))
            return null;

        if (list.TryGetValue(tradeId, out CryptoTrade? trade))
            return trade!;

        return null;
    }

    public void ClearTradesFromSymbol(CryptoSymbol symbol)
    {
        if (ContainsKey(symbol.Name))
            Remove(symbol.Name);
    }

    public SortedList<string, CryptoTrade> GetTradesForSymbol(CryptoSymbol symbol)
    {
        if (TryGetValue(symbol.Name, out SortedList<string, CryptoTrade>? list))
            return list!;

        return [];
    }
}
