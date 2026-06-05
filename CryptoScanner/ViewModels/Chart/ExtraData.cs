using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Dapper;

namespace CryptoScanner.ViewModels.Chart;

public class ExtraData
{
    public static void LoadSignalsForSymbol(CryptoSymbol symbol, CandleTime from, List<CryptoSignal> signals)
    {
        signals.Clear();
        string sql = "select * from signal where SymbolId = @SymbolId and CloseDate > @CloseDate";

        using var database = new CryptoDatabase();
        try
        {
            foreach (CryptoSignal signal in database.Connection.Query<CryptoSignal>(sql,
                new { SymbolId = symbol.Id, CloseDate = from.ToDateTime() }))
            {
                if (GlobalData.ExchangeListId.TryGetValue(signal.ExchangeId, out Core.Model.CryptoExchange? exchange2))
                {
                    signal.Exchange = exchange2;

                    if (exchange2.SymbolListId.TryGetValue(signal.SymbolId, out CryptoSymbol? symbolX))
                    {
                        signal.Symbol = symbolX;

                        if (GlobalData.IntervalListId.TryGetValue(signal.IntervalId, out CryptoInterval? interval))
                            signal.Interval = interval;

                        signals.Add(signal);
                    }
                }
            }
        }
        finally
        {
            database.Close();
        }
    }

    public static void LoadPositionsForSymbol(CryptoSymbol symbol, CandleTime from, List<CryptoPosition> positions)
    {
        using var database = new CryptoDatabase();
        try
        {

            //Steps.Clear();
            //string sql = "select positionstep.* from positionstep " +
            //    "inner join position on position.id=positionstep.positionid " +
            //    "where position.SymbolId = @SymbolId " +
            //    "and positionstep.status in (0,1,2,3) " +
            //    "and positionstep.CreateTime > @CloseTime";
            ////"and not positionstep.CloseTime is null " +

            //foreach (CryptoPositionStep step in database.Connection.Query<CryptoPositionStep>(sql,
            //   new { SymbolId = data.Symbol.Id, CloseTime = from.ToDateTime().AddDays(-3) }))
            //{
            //    Steps.Add(step);
            //}


            positions.Clear();
            string sql = "select position.* from positionstep " +
                "inner join position on position.id=positionstep.positionid " +
                "where position.SymbolId = @SymbolId " +
                "and positionstep.CreateTime > @CreateTime";

            foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql,
               new { SymbolId = symbol.Id, CreateTime = from.ToDateTime().AddDays(-3) }))
            {
                if (GlobalData.ExchangeListId.TryGetValue(position.ExchangeId, out Core.Model.CryptoExchange? exchange))
                {
                    position.Exchange = exchange;
                    if (exchange.SymbolListId.TryGetValue(position.SymbolId, out CryptoSymbol? symbolX))
                    {
                        position.Symbol = symbolX;
                        if (GlobalData.IntervalListId.TryGetValue((int)position.IntervalId!, out CryptoInterval? interval))
                            position.Interval = interval;

                        positions.Add(position);
                        PositionTools.LoadPosition(database, position);
                    }
                }

            }
        }
        finally
        {
            database.Close();
        }
    }


}
