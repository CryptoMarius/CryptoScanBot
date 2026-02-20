using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using NPOI.SS.UserModel;

namespace CryptoScanner.Core.Excel;

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
        var trend = Symbol.Data.TrendPrimary;
        WriteCell(sheet, 0, row, "Trend primary");
        if (trend.Time.HasValue)
        {
            DateTime? x = trend.Time?.ToDateTime();
            WriteCell(sheet, 1, row, x?.ToLocalTime(), CellStyleDate);
        }
        else WriteCell(sheet, 1, row, "");
        WriteCell(sheet, 2, row, trend.Trend.ToString());
        WriteCell(sheet, 3, row, trend.Percentage, CellStyleDecimalNormal);

        row++;
        trend = Symbol.Data.TrendSecondary;
        WriteCell(sheet, 0, row, "Trend secondary");
        if (trend.Time.HasValue)
        {
            DateTime? x = trend.Time?.ToDateTime();
            WriteCell(sheet, 1, row, x?.ToLocalTime(), CellStyleDate);
        }
        else WriteCell(sheet, 1, row, "");
        WriteCell(sheet, 2, row, trend.Trend.ToString());
        WriteCell(sheet, 3, row, trend.Percentage, CellStyleDecimalNormal);
        row++;
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
        WriteCell(sheet, columns++, row, "Synchronized (+1)");
        WriteCell(sheet, columns++, row, "LastCandle ref");
        WriteCell(sheet, columns++, row, "Time trend prim");
        WriteCell(sheet, columns++, row, "Trend primary");
        WriteCell(sheet, columns++, row, "Time trend sec");
        WriteCell(sheet, columns++, row, "Trend secondary");

        foreach (CryptoSymbolInterval symbolInterval in Symbol.Data.SymbolIntervalList.ToList())
        {
            row++;
            int column = 0;

            WriteCell(sheet, column++, row, Symbol.Exchange.Name);
            WriteCell(sheet, column++, row, Symbol.Name);
            WriteCell(sheet, column++, row, symbolInterval.Interval?.Name);
            WriteCell(sheet, column++, row, symbolInterval.CandleList.Count);
            WriteCell(sheet, column++, row, symbolInterval.CandleList.Values.FirstOrDefault().DateLocal, CellStyleDate);
            WriteCell(sheet, column++, row, symbolInterval.CandleList.Values.LastOrDefault().DateLocal, CellStyleDate);

            // last candle synchronised with the exchange (or locally fully calculated)
            if (symbolInterval.LastCandleSynchronized.HasValue)
            {
                DateTime x = symbolInterval.LastCandleSynchronized.Value.ToDateTime();
                WriteCell(sheet, column++, row, x.ToLocalTime(), CellStyleDate);
            }
            else WriteCell(sheet, column++, row, "");

            // reference to the last candle system
            WriteCell(sheet, column++, row, symbolInterval.LastCandle.DateLocal, CellStyleDate);

            // primary trend
            trend = symbolInterval.TrendPrimary;
            if (trend.Time.HasValue)
            {
                DateTime? x = trend.Time?.ToDateTime();
                WriteCell(sheet, column++, row, x?.ToLocalTime(), CellStyleDate);
            }
            else WriteCell(sheet, column++, row, "");
            WriteCell(sheet, column++, row, trend.Trend.ToString());

            // secondary trend
            trend = symbolInterval.TrendSecondary;
            if (trend.Time.HasValue)
            {
                DateTime? x = trend.Time?.ToDateTime();
                WriteCell(sheet, column++, row, x?.ToLocalTime(), CellStyleDate);
            }
            else WriteCell(sheet, column++, row, "");
            WriteCell(sheet, column++, row, trend.Trend.ToString());
        }

        AutoSize(sheet, columns);
    }


    private void DumpInterval(CryptoSymbolInterval symbolInterval)
    {
        ISheet sheet = Book.CreateSheet(symbolInterval.Interval?.Name);

        int row = 0;

        // Columns...
        int columns = 0;
        WriteCell(sheet, columns++, row, "OpenTime");
        WriteCell(sheet, columns++, row, "CloseTime");
        WriteCell(sheet, columns++, row, "Open");
        WriteCell(sheet, columns++, row, "High");
        WriteCell(sheet, columns++, row, "Low");
        WriteCell(sheet, columns++, row, "Close");
        WriteCell(sheet, columns++, row, "QuoteVolume");

        //WriteCell(sheet, columns++, row, "Rsi");
        //WriteCell(sheet, columns++, row, "StochOscillator");
        //WriteCell(sheet, columns++, row, "StochSignal");
        //WriteCell(sheet, columns++, row, "bb.low");
        //WriteCell(sheet, columns++, row, "bb.high");
        //WriteCell(sheet, columns++, row, "Sma200");
        //WriteCell(sheet, columns++, row, "Sma50");
        //WriteCell(sheet, columns++, row, "Sma20");
        //WriteCell(sheet, columns++, row, "PSar");
        //WriteCell(sheet, columns++, row, "Lux5mValue");

        CryptoCandle last = default;
        foreach (CryptoCandle candle in symbolInterval.CandleList.Values.ToList())
        {
            row++;
            int column = 0;
            bool attention = false;
            if (Symbol.IsBarometerSymbol())
                attention = last.OpenTime != 0 && last.OpenTime + 1 != candle.OpenTime;
            else
                attention = last.OpenTime != 0 && last.OpenTime + symbolInterval.Interval!.Duration != candle.OpenTime;

            //WriteCell(sheet, column++, row, candle.OpenTime.ToDateTime(), CellStyleDate);
            if (attention)
                WriteCell(sheet, column++, row, candle.DateLocal, CellStyleDateRed);
            else
                WriteCell(sheet, column++, row, candle.DateLocal, CellStyleDate);
            WriteCell(sheet, column++, row, candle.DateLocal.AddMinutes(symbolInterval.Interval?.Duration ?? 0), CellStyleDate);
            WriteCell(sheet, column++, row, candle.Open, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, candle.High, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, candle.Low, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, candle.Close, CellStyleDecimalNormal);

            if (candle.Volume == 0m && !Symbol.IsBarometerSymbol())
                WriteCell(sheet, column++, row, candle.Volume, CellStyleDecimalRed);
            else
                WriteCell(sheet, column++, row, candle.Volume, CellStyleDecimalNormal);

            //if (candle.CandleData != null)
            //{
            //    WriteCell(sheet, column++, row, candle.CandleData.Rsi, CellStyleDecimalNormal);
            //    WriteCell(sheet, column++, row, candle.CandleData.StochOscillator, CellStyleDecimalNormal);
            //    WriteCell(sheet, column++, row, candle.CandleData.StochSignal, CellStyleDecimalNormal);
            //    WriteCell(sheet, column++, row, candle.CandleData.BollingerBandsLowerBand, CellStyleDecimalNormal);
            //    WriteCell(sheet, column++, row, candle.CandleData.BollingerBandsUpperBand, CellStyleDecimalNormal);
            //    WriteCell(sheet, column++, row, candle.CandleData.Sma200, CellStyleDecimalNormal);
            //    WriteCell(sheet, column++, row, candle.CandleData.Sma50, CellStyleDecimalNormal);
            //    WriteCell(sheet, column++, row, candle.CandleData.Sma20, CellStyleDecimalNormal);
            //    WriteCell(sheet, column++, row, candle.CandleData.PSar, CellStyleDecimalNormal);
            //    WriteCell(sheet, column++, row, candle.CandleData.Lux5mValue, CellStyleDecimalNormal);
            //}

            last = candle;
        }

        AutoSize(sheet, columns);
    }


    //private void DumpZigZagInterval(AccountSymbolIntervalData data)
    //{
    //    ISheet sheet = Book.CreateSheet("Zigzag" + data.Interval?.Name);
    //    int row = 0;

    //    //var indicator = data.Indicator;
    //    foreach (var indicator in data.ZigZagIndicators!)
    //    //if (indicator != null)
    //    {
    //        //DumpZigZagInterval(trendDataList.Interval, indicator);
    //        //+
    //        WriteCell(sheet, 0, row, "Deviation");
    //        WriteCell(sheet, 1, row, indicator.Deviation.ToString(), CellStyleDecimalNormal);
    //        WriteCell(sheet, 1, row, "Auto");


    //        // Columns...
    //        row++;
    //        row++;
    //        int columns = 0;
    //        WriteCell(sheet, columns++, row, "OpenTime");
    //        WriteCell(sheet, columns++, row, "Type");
    //        WriteCell(sheet, columns++, row, "Value");

    //        if (indicator.ZigZagList != null)
    //        {
    //            foreach (ZigZagResult zigZag in indicator.ZigZagList)
    //            {
    //                row++;
    //                int column = 0;

    //                WriteCell(sheet, column++, row, zigZag.Candle.DateLocal, CellStyleDate);
    //                WriteCell(sheet, column++, row, zigZag.PointType);
    //                WriteCell(sheet, column++, row, zigZag.Value, CellStyleDecimalNormal);
    //            }
    //        }
    //        row++;
    //        row++;
    //        row++;
    //    }

    //    AutoSize(sheet, 3);
    //}



    public void ExportToExcel()
    {
        GlobalData.AddTextToLogTab($"Dumping symbol {Symbol.Name} to Excel");
        try
        {
            DumpInformation();

            foreach (CryptoSymbolInterval symbolInterval in Symbol.Data.SymbolIntervalList.ToList())
                DumpInterval(symbolInterval);

            //AccountSymbolData accountSymbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(Symbol.Name);
            //foreach (var trendDataList in accountSymbolData.SymbolTrendDataList)
            //{
            //    if (trendDataList.ZigZagIndicators != null)
            //    {
            //        DumpZigZagInterval(trendDataList);
            //    }
            //    //DumpZigZagInterval(trendDataList);
            //}
            StartExcell("Candles", Symbol.Name);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR candle dump " + error.ToString());
        }
    }
}