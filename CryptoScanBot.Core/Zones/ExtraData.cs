using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Dapper;

namespace CryptoScanBot.Core.Zones;

public class ExtraData
{

    public static void LoadSignalsForSymbol(CryptoZoneData data, long from)
    {
        data.Signals.Clear();
        string sql = "select * from signal where BackTest=0 and SymbolId = @SymbolId and EventTime > @eventTime";

        using var database = new CryptoDatabase();
        foreach (CryptoSignal signal in database.Connection.Query<CryptoSignal>(sql, new { SymbolId = data.Symbol.Id, eventTime = from }))
        {
            if (GlobalData.ExchangeListId.TryGetValue(signal.ExchangeId, out Core.Model.CryptoExchange? exchange2))
            {
                signal.Exchange = exchange2;

                if (exchange2.SymbolListId.TryGetValue(signal.SymbolId, out CryptoSymbol? symbol))
                {
                    signal.Symbol = symbol;

                    if (GlobalData.IntervalListId.TryGetValue(signal.IntervalId, out CryptoInterval? interval))
                        signal.Interval = interval;

                    data.Signals.Add(signal);
                }
            }
        }

    }

    public static void LoadPositionsForSymbol(CryptoZoneData data, long from)
    {
        data.Positions.Clear();
        using var database = new CryptoDatabase();
        //string sql = "select * from position where TradeAccountId=@TradeAccountId and SymbolId = @SymbolId order by id desc limit 50";
        //foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql, new { TradeAccountId = GlobalData.ActiveAccount!.Id }))
        //{
        //    if (!GlobalData.TradeAccountList.TryGetValue(position.TradeAccountId, out CryptoAccount? tradeAccount))
        //        throw new Exception("No trading account found");

        //    data.Positions.Add(position);
        //}

    }
}
