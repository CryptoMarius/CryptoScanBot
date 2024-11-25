using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

using Dapper;

using NPOI.SS.UserModel;

namespace CryptoScanBot.Core.Excel;

public class ExcelPostionsDump() : ExcelBase("Positions")
{
    readonly List<CryptoPosition> PositionList = [];

    public void LoadPositions(string sql)
    {
        using var database = new CryptoDatabase();
        foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql, new { TradeAccountId = GlobalData.ActiveAccount!.Id }))
        {
            if (GlobalData.TradeAccountList.TryGetValue(position.TradeAccountId, out CryptoAccount? tradeAccount))
            {
                position.Account = tradeAccount;
                if (GlobalData.ExchangeListId.TryGetValue(position.ExchangeId, out Model.CryptoExchange? exchange))
                {
                    position.Exchange = exchange;
                    if (exchange.SymbolListId.TryGetValue(position.SymbolId, out CryptoSymbol? symbol))
                    {
                        position.Symbol = symbol;
                        if (GlobalData.IntervalListId.TryGetValue((int)position.IntervalId!, out CryptoInterval? interval))
                            position.Interval = interval!;

                        PositionList.Add(position);
                    }
                }
            }
        }
    }

    public void DumpPositions()
    {
        ISheet sheet = Book.CreateSheet("Positions");
        int row = 0;

        int columns = 0;
        WriteCell(sheet, columns++, row, "Id");
        WriteCell(sheet, columns++, row, "Exchange");
        WriteCell(sheet, columns++, row, "Symbol");
        WriteCell(sheet, columns++, row, "Interval");

        WriteCell(sheet, columns++, row, "Strategy");
        WriteCell(sheet, columns++, row, "Side");
        WriteCell(sheet, columns++, row, "Status");

        WriteCell(sheet, columns++, row, "Created");
        WriteCell(sheet, columns++, row, "Updated");
        WriteCell(sheet, columns++, row, "Closed");

        WriteCell(sheet, columns++, row, "Invested");
        WriteCell(sheet, columns++, row, "Returned");
        WriteCell(sheet, columns++, row, "Commission");
        WriteCell(sheet, columns++, row, "Profit");
        WriteCell(sheet, columns++, row, "Percentage");
        WriteCell(sheet, columns++, row, "Parts");

        WriteCell(sheet, columns++, row, "QuantityTick");
        WriteCell(sheet, columns++, row, "RemainingDust");
        //WriteCell(sheet, columns++, row, "DustValue");

        WriteCell(sheet, columns++, row, "SignalDate");
        WriteCell(sheet, columns++, row, "SignalPrice");
        WriteCell(sheet, columns++, row, "SignalVolume");
        WriteCell(sheet, columns++, row, "TfTrend");
        WriteCell(sheet, columns++, row, "MarketTrend");
        WriteCell(sheet, columns++, row, "Change24h");
        WriteCell(sheet, columns++, row, "Move24h");
        WriteCell(sheet, columns++, row, "BB%");
        WriteCell(sheet, columns++, row, "BB.Upper");
        WriteCell(sheet, columns++, row, "BB.Lower");
        WriteCell(sheet, columns++, row, "RSI");
        WriteCell(sheet, columns++, row, "Stoch");
        WriteCell(sheet, columns++, row, "Signal");

        WriteCell(sheet, columns++, row, "Sma200");
        WriteCell(sheet, columns++, row, "Sma50");
        WriteCell(sheet, columns++, row, "Sma20");
        WriteCell(sheet, columns++, row, "PSar");
        WriteCell(sheet, columns++, row, "Lux5m");

        WriteCell(sheet, columns++, row, "Trend15m");
        WriteCell(sheet, columns++, row, "Trend30m");
        WriteCell(sheet, columns++, row, "Trend1h");
        WriteCell(sheet, columns++, row, "Trend4h");
        WriteCell(sheet, columns++, row, "Trend1d");

        WriteCell(sheet, columns++, row, "Barometer15m");
        WriteCell(sheet, columns++, row, "Barometer30m");
        WriteCell(sheet, columns++, row, "Barometer1h");
        WriteCell(sheet, columns++, row, "Barometer4h");
        WriteCell(sheet, columns++, row, "Barometer1d");

        WriteCell(sheet, columns++, row, "AltradyId"); 

        foreach (CryptoPosition position in PositionList.ToList())
        {
            ++row;
            int column = 0;

            WriteCell(sheet, column++, row, position.Id);
            WriteCell(sheet, column++, row, position.Exchange.Name);
            WriteCell(sheet, column++, row, position.Symbol!.Name);
            WriteCell(sheet, column++, row, position.Interval!.Name);

            WriteCell(sheet, column++, row, position.StrategyText.ToString());
            WriteCell(sheet, column++, row, position.Side.ToString());
            WriteCell(sheet, column++, row, position.Status.ToString());

            WriteCell(sheet, column++, row, position.CreateTime.ToLocalTime(), CellStyleDate);
            WriteCell(sheet, column++, row, position.UpdateTime?.ToLocalTime(), CellStyleDate);
            WriteCell(sheet, column++, row, position.CloseTime?.ToLocalTime(), CellStyleDate);

            WriteCell(sheet, column++, row, position.Invested, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.Returned, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.Commission, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.Profit, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.Percentage, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.PartCountText(position.CloseTime != null));

            WriteCell(sheet, column++, row, position.Symbol?.QuantityTickSize, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.RemainingDust, CellStyleDecimalNormal);
            //if ()
            //WriteCell(sheet, column++, row, position.RemainingDust * position.Symbol.LastPrice, CellStyleDecimalNormal);

            WriteCell(sheet, column++, row, position.SignalEventTime.ToLocalTime(), CellStyleDate);
            WriteCell(sheet, column++, row, position.SignalPrice, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.SignalVolume, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, TrendTools.TrendIndicatorText(position.TrendIndicator));
            WriteCell(sheet, column++, row, position.TrendPercentage, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.Last24HoursChange, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.Last24HoursEffective, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.BollingerBandsPercentage, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.BollingerBandsUpperBand, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.BollingerBandsLowerBand, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.Rsi, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.StochOscillator, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.StochSignal, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.Sma200, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.Sma50, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.Sma20, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.PSar, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.LuxIndicator5m, CellStyleDecimalNormal);

            WriteCell(sheet, column++, row, TrendTools.TrendIndicatorText(position.Trend15m));
            WriteCell(sheet, column++, row, TrendTools.TrendIndicatorText(position.Trend30m));
            WriteCell(sheet, column++, row, TrendTools.TrendIndicatorText(position.Trend1h));
            WriteCell(sheet, column++, row, TrendTools.TrendIndicatorText(position.Trend4h));
            WriteCell(sheet, column++, row, TrendTools.TrendIndicatorText(position.Trend1d));

            WriteCell(sheet, column++, row, position.Barometer15m, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.Barometer30m, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.Barometer1h, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.Barometer4h, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, position.Barometer1d, CellStyleDecimalNormal);

            WriteCell(sheet, column++, row, position.AltradyPositionId);
        }

        AutoSize(sheet, columns);
    }


   public void ExportToExcel()
    {
        GlobalData.AddTextToLogTab($"Dumping positions to Excel");
        try
        {
            LoadPositions("select * from position where closetime is null and status < 2 and TradeAccountId=@TradeAccountId");
            LoadPositions("select * from position where not closetime is null and TradeAccountId=@TradeAccountId order by id desc");
            DumpPositions();

            StartExcell("position", "Positions");
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR postions dump " + error.ToString());
        }
    }
}