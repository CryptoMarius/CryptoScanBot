using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;


namespace CryptoSbmScanner.Excel;

public class ExcelCandleDump : ExcelBase
{
    CryptoSymbol Symbol;


    private static int OHLCHeaders(HSSFSheet sheet, int row)
    {
        int column = 0;

        // Columns...
        WriteCell(sheet, column++, row, "Date");
        WriteCell(sheet, column++, row, "Open");
        WriteCell(sheet, column++, row, "High");
        WriteCell(sheet, column++, row, "Low");
        WriteCell(sheet, column++, row, "Close");
        WriteCell(sheet, column++, row, "Volume");

        return column;
    }


    private void DumpInterval(CryptoSymbolInterval symbolInterval)
    {
        HSSFSheet sheet = (HSSFSheet)Book.CreateSheet(symbolInterval.Interval.Name);

        int row = 0;
        int column;
        int columns = OHLCHeaders(sheet, 0);

        foreach (CryptoCandle candle in symbolInterval.CandleList.Values.ToList())
        {
            row++;
            column = 0;

            ICell cell;
            cell = WriteCell(sheet, column++, row, candle.DateLocal);
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
        }

        AutoSize(sheet, columns);
    }

    public void ExportToExcell(CryptoSymbol symbol)
    {
        Symbol = symbol;
        try
        {
            CreateBook(Symbol.Name);
            CreateFormats();

            foreach (CryptoSymbolInterval symbolInterval in symbol.IntervalPeriodList.ToList())
                DumpInterval(symbolInterval);

            StartExcell("Candles", Symbol.Name, Symbol.Exchange.Name);

        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("ERROR candle dump " + error.ToString());
        }
    }
}
