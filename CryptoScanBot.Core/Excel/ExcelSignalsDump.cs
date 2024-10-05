using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

using Dapper;

using NPOI.SS.UserModel;

namespace CryptoScanBot.Core.Excel;

public class ExcelSignalsDump() : ExcelBase("Signals")
{
    readonly List<CryptoSignal> SignalList = [];

    public void LoadSignals()
    {
        string sql = "select * from signal where BackTest=0 order by OpenDate";

        using var database = new CryptoDatabase();
        foreach (CryptoSignal signal in database.Connection.Query<CryptoSignal>(sql, new { FromDate = DateTime.UtcNow }))
        {
            if (GlobalData.ExchangeListId.TryGetValue(signal.ExchangeId, out Model.CryptoExchange? exchange2))
            {
                signal.Exchange = exchange2;

                if (exchange2.SymbolListId.TryGetValue(signal.SymbolId, out CryptoSymbol? symbol))
                {
                    signal.Symbol = symbol;

                    if (GlobalData.IntervalListId.TryGetValue(signal.IntervalId, out CryptoInterval? interval))
                        signal.Interval = interval;

                    SignalList.Add(signal);
                }
            }
        }
    }


    public void DumpSignals()
    {
        ISheet sheet = Book.CreateSheet("Signals");
        int row = 0;

        int columns = 0;
        WriteCell(sheet, columns++, row, "Id");
        WriteCell(sheet, columns++, row, "Exchange");
        WriteCell(sheet, columns++, row, "Symbol");
        WriteCell(sheet, columns++, row, "Interval");

        WriteCell(sheet, columns++, row, "Strategy");
        WriteCell(sheet, columns++, row, "Side");

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

        WriteCell(sheet, columns++, row, "MinPrice");
        WriteCell(sheet, columns++, row, "MaxPrice");
        WriteCell(sheet, columns++, row, "MinPricePerc");
        WriteCell(sheet, columns++, row, "MaxPricePerc");


        foreach (CryptoSignal signal in SignalList.ToList())
        {
            ++row;
            int column = 0;

            WriteCell(sheet, column++, row, signal.Id);
            WriteCell(sheet, column++, row, signal.Exchange.Name);
            WriteCell(sheet, column++, row, signal.Symbol.Name);
            WriteCell(sheet, column++, row, signal.Interval!.Name);

            WriteCell(sheet, column++, row, signal.StrategyText.ToString());
            WriteCell(sheet, column++, row, signal.Side.ToString());
            //if ()
            //WriteCell(sheet, column++, row, signal.RemainingDust * signal.Symbol.LastPrice, CellStyleDecimalNormal);

            WriteCell(sheet, column++, row, signal.CloseDate.ToLocalTime(), CellStyleDate);
            WriteCell(sheet, column++, row, signal.SignalPrice, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.SignalVolume, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, TrendTools.TrendIndicatorText(signal.TrendIndicator));
            WriteCell(sheet, column++, row, signal.TrendPercentage, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.Last24HoursChange, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.Last24HoursEffective, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.BollingerBandsPercentage, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.BollingerBandsUpperBand, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.BollingerBandsLowerBand, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.Rsi, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.StochOscillator, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.StochSignal, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.Sma200, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.Sma50, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.Sma20, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.PSar, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.LuxIndicator5m, CellStyleDecimalNormal);

            WriteCell(sheet, column++, row, TrendTools.TrendIndicatorText(signal.Trend15m));
            WriteCell(sheet, column++, row, TrendTools.TrendIndicatorText(signal.Trend30m));
            WriteCell(sheet, column++, row, TrendTools.TrendIndicatorText(signal.Trend1h));
            WriteCell(sheet, column++, row, TrendTools.TrendIndicatorText(signal.Trend4h));
            WriteCell(sheet, column++, row, TrendTools.TrendIndicatorText(signal.Trend1d));

            WriteCell(sheet, column++, row, signal.Barometer15m, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.Barometer30m, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.Barometer1h, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.Barometer4h, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.Barometer1d, CellStyleDecimalNormal);

#if DEBUG
            WriteCell(sheet, column++, row, signal.PriceMin, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.PriceMax, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.PriceMinPerc, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, signal.PriceMaxPerc, CellStyleDecimalNormal);
#endif
        }

        AutoSize(sheet, columns);
    }


   public void ExportToExcel()
    {
        GlobalData.AddTextToLogTab($"Dumping signals to Excel");
        try
        {
            LoadSignals();
            DumpSignals();

            StartExcell("signals", "Signals");
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR signals dump " + error.ToString());
        }
    }
}