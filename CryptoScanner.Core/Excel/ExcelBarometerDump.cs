using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using NPOI.SS.UserModel;

namespace CryptoScanner.Core.Excel;

public class ExcelBarometerDump(CryptoSymbol Symbol) : ExcelBase(Symbol.Name)
{
    private void DumpInterval(CryptoInterval interval)
    {
        var quoteData = Symbol.QuoteData;
        var bmName = Const.Constants.SymbolNameBarometerPrice + quoteData.Name;
        if (!Symbol.Exchange.SymbolListName.TryGetValue(bmName, out CryptoSymbol? bmSymbol))
            return;

        var bmSymbolInterval = bmSymbol.GetSymbolInterval(interval);
        if (bmSymbolInterval.LastCandleSynchronized == null)
            return;

        if (bmSymbolInterval.CandleList.Count == 0)
            return;

        CandleTime unixCandleLast = bmSymbolInterval.LastCandleSynchronized.Value;
        CandleTime unixCandlePrev = unixCandleLast - bmSymbolInterval.Interval.Duration;


        ISheet sheet = Book.CreateSheet(interval.Name);

        int row = 0;

        // Columns...
        int columns = 0;
        WriteCell(sheet, columns++, row, "Symbol");
        WriteCell(sheet, columns++, row, "");
        WriteCell(sheet, columns++, row, "Time1");
        WriteCell(sheet, columns++, row, "Tick1");
        WriteCell(sheet, columns++, row, "Open");
        WriteCell(sheet, columns++, row, "High");
        WriteCell(sheet, columns++, row, "Low");
        WriteCell(sheet, columns++, row, "Close");
        WriteCell(sheet, columns++, row, "");
        WriteCell(sheet, columns++, row, "Time2");
        WriteCell(sheet, columns++, row, "Tick2");
        WriteCell(sheet, columns++, row, "Open2");
        WriteCell(sheet, columns++, row, "High2");
        WriteCell(sheet, columns++, row, "Low2");
        WriteCell(sheet, columns++, row, "Close2");
        WriteCell(sheet, columns++, row, "");
        WriteCell(sheet, columns++, row, "Perc");


        decimal sumPerc = 0;
        int coinsMatching = 0;
        for (int i = 0; i < quoteData.SymbolList.Count; i++) // foreach with ToList() is overkill
        {
            CryptoSymbol symbol = quoteData.SymbolList[i];

            if (symbol.QuoteData!.FetchCandles && !symbol.IsBarometerSymbol() && symbol.EnoughVolume())
            {
                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(Enums.CryptoIntervalPeriod.interval1m);
                if (symbolInterval.CandleList.TryGetValue(unixCandlePrev, out CryptoCandle candlePrev) &&
                    symbolInterval.CandleList.TryGetValue(unixCandleLast, out CryptoCandle candleLast))
                {
                    //if (candlePrev != null && candleLast != null) // Er worden in kucoin null candles toegevoegd?
                    {
                        decimal perc;
                        decimal diff = candleLast.Close - candlePrev.Close;
                        if (!candlePrev.Close.Equals(0))
                            perc = 100m * (diff / candlePrev.Close);
                        else perc = 0;

                        sumPerc += perc;
                        coinsMatching++;

                        row++;
                        int column = 0;

                        WriteCell(sheet, column++, row, symbol.Name);
                        WriteCell(sheet, column++, row, "");
                        WriteCell(sheet, column++, row, candlePrev.DateLocal, CellStyleDate);
                        WriteCell(sheet, column++, row, candlePrev.TickDecimals);
                        WriteCell(sheet, column++, row, candlePrev.Open, CellStyleDecimalNormal);
                        WriteCell(sheet, column++, row, candlePrev.High, CellStyleDecimalNormal);
                        WriteCell(sheet, column++, row, candlePrev.Low, CellStyleDecimalNormal);
                        WriteCell(sheet, column++, row, candlePrev.Close, CellStyleDecimalNormal);
                        WriteCell(sheet, column++, row, "");
                        WriteCell(sheet, column++, row, candleLast.DateLocal, CellStyleDate);
                        WriteCell(sheet, column++, row, candleLast.TickDecimals);
                        WriteCell(sheet, column++, row, candleLast.Open, CellStyleDecimalNormal);
                        WriteCell(sheet, column++, row, candleLast.High, CellStyleDecimalNormal);
                        WriteCell(sheet, column++, row, candleLast.Low, CellStyleDecimalNormal);
                        WriteCell(sheet, column++, row, candleLast.Close, CellStyleDecimalNormal);
                        WriteCell(sheet, column++, row, "");
                        WriteCell(sheet, column++, row, perc, CellStyleDecimalNormal);
                    }
                }
            }
        }

        decimal barometerPerc;
        if (coinsMatching > 0)
        {
            decimal result = sumPerc / coinsMatching;
            barometerPerc = decimal.Round(result, 8);
        }
        else
            barometerPerc = 0m; // not -99 because of long/short.

        // summation row
        row++;
        WriteCell(sheet, 14, row, sumPerc);
        WriteCell(sheet, 15, row, coinsMatching);

        // avaerage
        row++;
        WriteCell(sheet, 14, row, barometerPerc);

        AutoSize(sheet, columns);
    }


    public void ExportToExcel()
    {
        GlobalData.AddTextToLogTab($"Dumping barometer {Symbol.QuoteData.Name} to Excel");
        try
        {
            foreach (var interval in GlobalData.IntervalList)
                DumpInterval(interval);

            StartExcell("Barometer", Symbol.QuoteData.Name);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR barometer dump " + error.ToString());
        }
    }
}