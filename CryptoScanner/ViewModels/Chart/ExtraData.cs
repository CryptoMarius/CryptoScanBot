using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

using Dapper;

namespace CryptoScanner.ViewModels.Chart;

public class ExtraData
{

    public static void LoadSignalsForSymbol(ZoneConfig data, long from)
    {
        data.Signals.Clear();
        string sql = "select * from signal where BackTest=0 and SymbolId = @SymbolId and EventTime > @eventTime";

        using var database = new CryptoDatabase();
        try
        {
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
        finally
        {
            database.Close();
        }


    }

    public static void LoadPositionsForSymbol(ZoneConfig data, long from)
    {
        //data.Positions.Clear();
        //using var database = new CryptoDatabase();
        //string sql = "select * from position where TradeAccountId=@TradeAccountId and SymbolId = @SymbolId order by id desc limit 50";
        //foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql, new { TradeAccountId = GlobalData.ActiveAccount!.Id }))
        //{
        //    if (!GlobalData.TradeAccountList.TryGetValue(position.TradeAccountId, out CryptoAccount? tradeAccount))
        //        throw new Exception("No trading account found");

        //    data.Positions.Add(position);
        //}

    }
}
