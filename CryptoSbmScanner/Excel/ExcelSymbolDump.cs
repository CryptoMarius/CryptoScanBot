using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Humanizer;

using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;


namespace CryptoSbmScanner.Excel;

public class ExcelSymbolDump : ExcelBase
{
    CryptoSymbol Symbol;


    private void DumpInformation()
    {
        // Overzichts van de aanwezige candles
        HSSFSheet sheet = (HSSFSheet)Book.CreateSheet("Information");
        ICell cell;

        int row = 0;
        WriteCell(sheet, 0, row, "Created");
        cell = WriteCell(sheet, 1, row, DateTime.Now);
        cell.CellStyle = CellStyleDate;
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

        foreach (CryptoSymbolInterval symbolInterval in Symbol.IntervalPeriodList.ToList())
        {
            row++;
            int column = 0;

            WriteCell(sheet, column++, row, Symbol.Exchange.Name);
            WriteCell(sheet, column++, row, Symbol.Name);
            WriteCell(sheet, column++, row, symbolInterval.Interval.Name);
            WriteCell(sheet, column++, row, (double)symbolInterval.CandleList.Count);

            if (symbolInterval.CandleList.Any())
            {
                cell = WriteCell(sheet, column++, row, symbolInterval.CandleList.Values.First().DateLocal);
                cell.CellStyle = CellStyleDate;

                cell = WriteCell(sheet, column++, row, symbolInterval.CandleList.Values.Last().DateLocal);
                cell.CellStyle = CellStyleDate;
            }
        }

        AutoSize(sheet, columns);
    }


    private void DumpInterval(CryptoSymbolInterval symbolInterval)
    {
        HSSFSheet sheet = (HSSFSheet)Book.CreateSheet(symbolInterval.Interval.Name);
        ICell cell;

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

            cell = WriteCell(sheet, column++, row, candle.OpenTime);
            
            cell = WriteCell(sheet, column++, row, candle.DateLocal);
            cell.CellStyle = CellStyleDate;

            cell = WriteCell(sheet, column++, row, candle.DateLocal.AddSeconds(symbolInterval.Interval.Duration));
            cell.CellStyle = CellStyleDate;

            cell = WriteCell(sheet, column++, row, (double)candle.Open);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)candle.High);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)candle.Low);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)candle.Close);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)candle.Volume);
            cell.CellStyle = CellStyleDecimalNormal;

            WriteCell(sheet, column++, row, candle.IsDuplicated.ToString());
        }

        AutoSize(sheet, columns);
    }

    public void ExportToExcel(CryptoSymbol symbol)
    {
        Symbol = symbol;
        try
        {
            CreateBook(Symbol.Name);
            CreateFormats();

            DumpInformation();

            foreach (CryptoSymbolInterval symbolInterval in symbol.IntervalPeriodList.ToList())
                DumpInterval(symbolInterval);

            StartExcell("Candles", Symbol.Name, Symbol.Exchange.Name);
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR candle dump " + error.ToString());
        }
    }
}
