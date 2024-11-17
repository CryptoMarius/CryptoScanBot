using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

using NPOI.SS.UserModel;

namespace CryptoScanBot.Core.Excel;

public class ExcelSymbolDump(CryptoSymbol Symbol) : ExcelBase(Symbol.Name)
{

    //static private void ExportToExcel(Model.CryptoExchange exchange, CryptoSymbol symbol, CryptoInterval interval, CryptoCandleList candleList)
    //{
    //    //Deze is op dit moment specifiek voor de TradeView aanpak gemaakt (datum er ff uitgehaald en vervangen met unix 1000's)
    //    try

    //    {
    //        var csv = new StringBuilder();
    //        var newLine = string.Format("{0},{1},{2},{3},{4},{5},{6}", "Timestamp", "Symbol", "Open", "High", "Low", "Close", "Volume");
    //        csv.AppendLine(newLine);

    //        //Monitor.Enter(candleList);
    //        //try
    //        //{
    //        for (int i = 0; i < candleList.Count; i++)
    //        {
    //            CryptoCandle candle = candleList.Values[i];

    //            newLine = string.Format("{0}000,{1},{2},{3},{4},{5},{6}",
    //            candle.OpenTime.ToString(),
    //            //CandleTools.GetUnixDate(candle.OpenTime).ToString(),
    //            symbol.Name,
    //            //candle.Interval.ToString(),
    //            candle.Open.ToString(),
    //            candle.High.ToString(),
    //            candle.Low.ToString(),
    //            candle.Close.ToString(),
    //            //GetUnixDate(candle.CloseTime).ToString(),
    //            candle.Volume.ToString());
    //            //candle.Trades.ToString());

    //            csv.AppendLine(newLine);
    //        }
    //        //}
    //        //finally
    //        //{
    //        //    Monitor.Exit(candleList);
    //        //}
    //        string filename = GlobalData.GetBaseDir();
    //        filename = filename + @"\data\" + exchange.Name + @"\Candles\" + symbol.Name + @"\"; // + interval.Name + @"\";
    //        Directory.CreateDirectory(filename);
    //        File.WriteAllText(filename + symbol.Name + "-" + interval.Name + ".csv", csv.ToString());

    //    }
    //    catch (Exception error)
    //    {
    //        ScannerLog.Logger.Error(error, "");
    //        // Soms is niet alles goed gevuld en dan krijgen we range errors e.d.
    //        //GlobalData.AddTextToLogTab(error.ToString());
    //    }
    //}


    //static public void ExportToExcelAll()
    //{
    //    foreach (Model.CryptoExchange exchange in GlobalData.ExchangeListName.Values)
    //    {
    //        ExportSymbolsToExcel(exchange);

    //        foreach (var symbol in exchange.SymbolListName.Values)
    //        {
    //            foreach (CryptoInterval interval in GlobalData.IntervalList)
    //            {
    //                CryptoSymbolInterval symbolPeriod = symbol.GetSymbolInterval(interval.IntervalPeriod);
    //                ExportToExcel(exchange, symbol, interval, symbolPeriod.CandleList);
    //            }
    //        }
    //    }
    //}

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
#if SUPPORTBASEVOLUME
        WriteCell(sheet, columns++, row, "BaseVolume");
#endif
        WriteCell(sheet, columns++, row, "QuoteVolume");
        WriteCell(sheet, columns++, row, "Duplicated");

        WriteCell(sheet, columns++, row, "SlopeRsi");
        WriteCell(sheet, columns++, row, "SlopeMacd");
        WriteCell(sheet, columns++, row, "SlopeStoch");
        WriteCell(sheet, columns++, row, "SlopeSma20");
        WriteCell(sheet, columns++, row, "SlopeSma50");
        WriteCell(sheet, columns++, row, "SlopeSma100");
        WriteCell(sheet, columns++, row, "SlopeSma200");

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

#if SUPPORTBASEVOLUME
            if (candle.BaseVolume == 0m)
                WriteCell(sheet, column++, row, candle.BaseVolume, CellStyleDecimalRed);
            else
                WriteCell(sheet, column++, row, candle.BaseVolume, CellStyleDecimalNormal);
#endif
            if (candle.Volume == 0m)
                WriteCell(sheet, column++, row, candle.Volume, CellStyleDecimalRed);
            else
                WriteCell(sheet, column++, row, candle.Volume, CellStyleDecimalNormal);

            if (candle.IsDuplicated)
                WriteCell(sheet, column++, row, candle.IsDuplicated.ToString());
            else
                column++;

            if (candle.CandleData != null)
            {
                List<double?> bla = [];
                bla.Add(candle.CandleData.SlopeRsi);
                bla.Add(candle.CandleData.SlopeMacd);
                bla.Add(candle.CandleData.SlopeStoch);
                bla.Add(candle.CandleData.SlopeSma20);
                bla.Add(candle.CandleData.SlopeSma50);
                bla.Add(candle.CandleData.SlopeSma100);
                bla.Add(candle.CandleData.SlopeSma200);

                foreach (var value in bla)
                {
                    if (value != null)
                    {
                        if (value <= 0)
                            WriteCell(sheet, column, row, value, CellStyleDecimalRed);
                        else
                            WriteCell(sheet, column, row, value, CellStyleDecimalGreen);
                    }
                    column++;
                }
            }

            last = candle;
        }

        AutoSize(sheet, columns);
    }


    private void DumpZigZagInterval(AccountSymbolIntervalData data)
    {
        ISheet sheet = Book.CreateSheet("Zigzag" + data.Interval?.Name);
        int row = 0;

        //var indicator = data.Indicator;
        foreach (var indicator in data.ZigZagIndicators!)
        //if (indicator != null)
        {
            //DumpZigZagInterval(trendDataList.Interval, indicator);
            //+ 
            WriteCell(sheet, 0, row, "Deviation");
            WriteCell(sheet, 1, row, indicator.Deviation.ToString(), CellStyleDecimalNormal);
            WriteCell(sheet, 1, row, "Auto");


            // Columns...
            row++;
            row++;
            int columns = 0;
            WriteCell(sheet, columns++, row, "OpenTime");
            WriteCell(sheet, columns++, row, "Type");
            WriteCell(sheet, columns++, row, "Value");

            if (indicator.ZigZagList != null)
            {
                foreach (ZigZagResult zigZag in indicator.ZigZagList)
                {
                    row++;
                    int column = 0;

                    WriteCell(sheet, column++, row, zigZag.Candle.DateLocal, CellStyleDate);
                    WriteCell(sheet, column++, row, zigZag.PointType);
                    WriteCell(sheet, column++, row, zigZag.Value, CellStyleDecimalNormal);
                }
            }
            row++;
            row++;
            row++;
        }

        AutoSize(sheet, 3);
    }



    public void ExportToExcel()
    {
        GlobalData.AddTextToLogTab($"Dumping symbol to Excel");
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
                //DumpZigZagInterval(trendDataList);
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