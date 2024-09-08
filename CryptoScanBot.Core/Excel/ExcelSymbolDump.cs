using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

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

        CryptoCandle? last = null;
        foreach (CryptoCandle candle in symbolInterval.CandleList.Values.ToList())
        {
            row++;
            int column = 0;
            bool attention = (last != null && last.OpenTime + symbolInterval.Interval!.Duration != candle.OpenTime || candle.IsDuplicated);

            WriteCell(sheet, column++, row, candle.OpenTime);
            if (attention)
                WriteCell(sheet, column++, row, candle.DateLocal, CellStyleDateRed);
            else
                WriteCell(sheet, column++, row, candle.DateLocal, CellStyleDate);
            WriteCell(sheet, column++, row, candle.DateLocal.AddSeconds(symbolInterval.Interval?.Duration ?? 0), CellStyleDate);
            WriteCell(sheet, column++, row, candle.Open, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, candle.High, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, candle.Low, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, candle.Close, CellStyleDecimalNormal);
            if (candle.Volume == 0m)
                WriteCell(sheet, column++, row, candle.Volume, CellStyleDecimalRed);
            else
                WriteCell(sheet, column++, row, candle.Volume, CellStyleDecimalNormal);

            if (candle.IsDuplicated)
                WriteCell(sheet, column++, row, candle.IsDuplicated.ToString());
            //else
            //    WriteCell(sheet, column++, row, zigZag.IsDuplicated.ToString());


            last = candle;
        }

        AutoSize(sheet, columns);
    }


    private void DumpZigZagInterval(AccountSymbolIntervalData data)
    {
        ISheet sheet = Book.CreateSheet("Zigzag" + data.Interval?.Name);
        int row = 0;

        foreach (var indicator in data.ZigZagIndicators!)
        {
            //DumpZigZagInterval(trendDataList.Interval, indicator);
            //+ 
            WriteCell(sheet, 0, row, "Deviation");
            WriteCell(sheet, 1, row, indicator.Deviation.ToString(), CellStyleDecimalNormal);


            // Columns...
            row++;
            row++;
            int columns = 0;
            WriteCell(sheet, columns++, row, "OpenTime");
            WriteCell(sheet, columns++, row, "Type");
            WriteCell(sheet, columns++, row, "Value");


            foreach (ZigZagResult zigZag in indicator.ZigZagList)
            {
                row++;
                int column = 0;

                WriteCell(sheet, column++, row, zigZag.Candle.DateLocal, CellStyleDate);
                WriteCell(sheet, column++, row, zigZag.PointType);
                WriteCell(sheet, column++, row, zigZag.Value, CellStyleDecimalNormal);
            }

            row++;
            row++;
            row++;
        }

        AutoSize(sheet, 3);
    }



    public void ExportToExcel()
    {
        try
        {
            DumpInformation();

            foreach (CryptoSymbolInterval symbolInterval in Symbol.IntervalPeriodList.ToList())
                DumpInterval(symbolInterval);

            AccountSymbolData accountSymbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(Symbol.Name);
            foreach (var trendDataList in accountSymbolData.SymbolTrendDataList)
            {
                if (trendDataList.ZigZagIndicators != null)
                {
                    DumpZigZagInterval(trendDataList);
                }
            }
            StartExcell("Candles", Symbol.Name);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR candle dump " + error.ToString());
        }
    }
}