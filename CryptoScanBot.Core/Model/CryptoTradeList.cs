using CryptoScanBot.Core.Intern;

namespace CryptoScanBot.Core.Model;

public class CryptoTradeList : SortedList<string, CryptoTrade>
{
    public void AddTrade(CryptoTrade trade, bool log = true)
    {
        if (GlobalData.TradeAccountList.TryGetValue(trade.TradeAccountId, out CryptoAccount? tradeAccount))
        {
            trade.TradeAccount = tradeAccount;

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
                            GlobalData.AddTextToLogTab($"{trade.Symbol.Name} order {trade.TradeTime} {trade.OrderId} added trade {trade.TradeId} (#{trade.Id})");
                    }
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
