using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using NPOI.SS.UserModel;

namespace CryptoScanBot.Core.Excel;

public class ExcelSymbolDump(CryptoSymbol Symbol) : ExcelBase(Symbol.Name)
{
    private void DumpInformation()
    {
        // Overzichts van de aanwezige candles
        ISheet sheet = Book.CreateSheet("Information");

        int row = 0;
        WriteCell(sheet, 0, row, "Created");
        WriteCell(sheet, 1, row, DateTime.Now, CellStyleDate);
        row++;
        row++;

        // Interval overview
        int columns = 0;
        WriteCell(sheet, columns++, row, "Exchange");
        WriteCell(sheet, columns++, row, "Symbol");
        WriteCell(sheet, columns++, row, "Interval");
        WriteCell(sheet, columns++, row, "Count");
        WriteCell(sheet, columns++, row, "First");
        WriteCell(sheet, columns++, row, "Last");
        WriteCell(sheet, columns++, row, "Synchronized");

        foreach (CryptoSymbolInterval symbolInterval in Symbol.IntervalPeriodList.ToList())
        {
            row++;
            int column = 0;

            WriteCell(sheet, column++, row, Symbol.Exchange.Name);
            WriteCell(sheet, column++, row, Symbol.Name);
            WriteCell(sheet, column++, row, symbolInterval.Interval?.Name);
            WriteCell(sheet, column++, row, symbolInterval.CandleList.Count);
            WriteCell(sheet, column++, row, symbolInterval.CandleList.Values.FirstOrDefault()?.DateLocal, CellStyleDate);
            WriteCell(sheet, column++, row, symbolInterval.CandleList.Values.LastOrDefault()?.DateLocal, CellStyleDate);

            // Debug: There is something not right in the synchronizing or building of candles..
            if (symbolInterval.LastCandleSynchronized.HasValue)
            {
                DateTime x = CandleTools.GetUnixDate(symbolInterval.LastCandleSynchronized);
                WriteCell(sheet, column++, row, x.ToLocalTime(), CellStyleDate);
            }
        }

        AutoSize(sheet, columns);
    }


    private void DumpInterval(CryptoSymbolInterval symbolInterval)
    {
        ISheet sheet = Book.CreateSheet(symbolInterval.Interval?.Name);

        int row = 0;

        // Columns...
        int columns = 0;
        WriteCell(sheet, columns++, row, "UnixTime");
        WriteCell(sheet, columns++, row, "OpenTime");
        WriteCell(sheet, columns++, row, "CloseTime");
        WriteCell(sheet, columns++, row, "Open");
        WriteCell(sheet, columns++, row, "High");
        WriteCell(sheet, columns++, row, "Low");
        WriteCell(sheet, columns++, row, "Close");
        WriteCell(sheet, columns++, row, "Volume");
        WriteCell(sheet, columns++, row, "Duplicated");

        foreach (CryptoCandle candle in symbolInterval.CandleList.Values.ToList())
        {
            row++;
            int column = 0;

            WriteCell(sheet, column++, row, candle.OpenTime);
            WriteCell(sheet, column++, row, candle.DateLocal, CellStyleDate);
            WriteCell(sheet, column++, row, candle.DateLocal.AddSeconds(symbolInterval.Interval?.Duration ?? 0), CellStyleDate);
            WriteCell(sheet, column++, row, candle.Open, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, candle.High, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, candle.Low, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, candle.Close, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, candle.Volume, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, candle.IsDuplicated.ToString());
        }

        AutoSize(sheet, columns);
    }

    public void ExportToExcel()
    {
        try
        {
            DumpInformation();

            foreach (CryptoSymbolInterval symbolInterval in Symbol.IntervalPeriodList.ToList())
                DumpInterval(symbolInterval);

            StartExcell("Candles", Symbol.Name);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR candle dump " + error.ToString());
        }
    }
}