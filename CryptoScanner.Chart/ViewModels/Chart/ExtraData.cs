using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Dapper;

namespace CryptoScanner.ViewModels.Chart;

public class ExtraData
{
    public static void LoadSignalsForSymbol(CryptoSymbol symbol, CandleTime from, CandleTime to, int? emulatorRunId, List<CryptoSignal> signals)
    {
        signals.Clear();

        // Bound to the visible window (from, to] AND to a single source: emulatorRunId set → that
        // emulator run's signals; null → live signals only (EmulatorRunId IS NULL). Without these the
        // query returned every signal of the symbol across ALL runs (and live), so the chart drew them
        // all on top of each other and became unreadable.
        string runFilter = emulatorRunId.HasValue ? "and EmulatorRunId = @RunId " : "and EmulatorRunId is null ";
        string sql = "select * from signal where SymbolId = @SymbolId " +
            "and CloseDate > @From and CloseDate <= @To " + runFilter;

        using var database = new CryptoDatabase();
        try
        {
            foreach (CryptoSignal signal in database.Connection.Query<CryptoSignal>(sql,
                new { SymbolId = symbol.Id, From = from.ToDateTime(), To = to.ToDateTime(), RunId = emulatorRunId }))
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

    public static void LoadPositionsForSymbol(CryptoSymbol symbol, CandleTime from, CandleTime to, int? emulatorRunId, List<CryptoPosition> positions)
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

            // Only positions whose lifetime overlaps the visible window [from, to], and only for one
            // source: emulatorRunId set → that emulator run; null → live positions only (EmulatorRunId
            // IS NULL). Filtering on the position itself (not via a positionstep join) also stops the
            // same position being returned once per step. Previously this loaded every position of the
            // symbol across ALL runs with no upper bound, which made the chart unreadable.
            string runFilter = emulatorRunId.HasValue ? "and EmulatorRunId = @RunId " : "and EmulatorRunId is null ";
            string sql = "select * from position where SymbolId = @SymbolId " +
                "and CreateTime <= @To and (CloseTime is null or CloseTime >= @From) " + runFilter +
                "order by CreateTime";

            foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql,
               new { SymbolId = symbol.Id, From = from.ToDateTime(), To = to.ToDateTime(), RunId = emulatorRunId }))
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
