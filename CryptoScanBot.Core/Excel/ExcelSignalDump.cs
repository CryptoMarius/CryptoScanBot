using CryptoScanBot.Core.Const;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Signal;

using NPOI.SS.UserModel;

namespace CryptoScanBot.Core.Excel;

public class ExcelSignalDump(CryptoSignal Signal) : ExcelBase(Signal.Symbol.Name)
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
        int columns = 2;
        int startRow = row;
        WriteCell(sheet, 0, row++, "Exchange");
        WriteCell(sheet, 0, row++, "Symbol");
        WriteCell(sheet, 0, row++, "Strategy");
        WriteCell(sheet, 0, row++, "Interval");

        row = startRow;
        WriteCell(sheet, 1, row++, Signal.Symbol.Exchange.Name);
        WriteCell(sheet, 1, row++, Signal.Symbol.Name);
        WriteCell(sheet, 1, row++, Signal.StrategyText);
        WriteCell(sheet, 1, row++, Signal.Interval.Name);

        AutoSize(sheet, columns);
    }


    private void DumpCandeData()
    {
        int columns = 0;
        ISheet sheet = Book.CreateSheet("Signal");

        if (Signal.Symbol.Exchange.SymbolListName.TryGetValue(Constants.SymbolNameBarometerPrice + Signal.Symbol.Quote, out CryptoSymbol? bmSymbol))
        {
            CryptoSymbolInterval bmSymbolInterval = bmSymbol.GetSymbolInterval(CryptoIntervalPeriod.interval1h);
            CryptoCandleList bmCandles = bmSymbolInterval.CandleList;

            CryptoSymbolInterval symbolInterval = Signal.Symbol.GetSymbolInterval(Signal.Interval.IntervalPeriod);
            if (symbolInterval.CandleList.Count > 0)
            {
                CryptoCandle candleLast = symbolInterval.CandleList.Values.Last();
                if (CandleIndicatorData.PrepareIndicators(Signal.Symbol, symbolInterval, candleLast, out _))
                {
                    int row = 0;

                    WriteCell(sheet, columns++, row, "Signal");
                    WriteCell(sheet, columns++, row, "OpenTime");
                    WriteCell(sheet, columns++, row, "Open");
                    WriteCell(sheet, columns++, row, "High");
                    WriteCell(sheet, columns++, row, "Low");
                    WriteCell(sheet, columns++, row, "Close");
                    WriteCell(sheet, columns++, row, "Volume");

                    WriteCell(sheet, columns++, row, "BB.Lower");
                    WriteCell(sheet, columns++, row, "BB.Center");
                    WriteCell(sheet, columns++, row, "BB.Upper");
                    WriteCell(sheet, columns++, row, "BB.Perc");

                    WriteCell(sheet, columns++, row, "SMA20");
                    WriteCell(sheet, columns++, row, "SMA50");
                    WriteCell(sheet, columns++, row, "SMA200");

                    WriteCell(sheet, columns++, row, "PSAR");

                    WriteCell(sheet, columns++, row, "MACD.K");
                    WriteCell(sheet, columns++, row, "MACD.D");
                    WriteCell(sheet, columns++, row, "MACD.H");

                    WriteCell(sheet, columns++, row, "RSI");

                    WriteCell(sheet, columns++, row, "STOCH.S");
                    WriteCell(sheet, columns++, row, "STOCH.O");

                    WriteCell(sheet, columns++, row, "Barometer 1h");

                    foreach (CryptoCandle candle in symbolInterval.CandleList.Values.ToList())
                    {
                        if (candle.CandleData != null)
                        {
                            int column = 0;
                            try
                            {
                                row++;

                                WriteCell(sheet, column++, row, Signal.OpenDate == candle.Date ? "Signal" : "");
                                WriteCell(sheet, column++, row, candle.DateLocal, CellStyleDate);
                                WriteCell(sheet, column++, row, candle.Open, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.High, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.Low, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.Close, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.Volume, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.CandleData.BollingerBandsLowerBand, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.CandleData.Sma20, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.CandleData.BollingerBandsUpperBand, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.CandleData.BollingerBandsPercentage, CellStylePercentageNormal);


                                WriteCell(sheet, column++, row, candle.CandleData.Sma20, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.CandleData.Sma50, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.CandleData.Sma200, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.CandleData.PSar, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.CandleData.MacdValue, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.CandleData.MacdSignal, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.CandleData.MacdHistogram, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.CandleData.Rsi, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.CandleData.StochSignal, CellStyleDecimalNormal);
                                WriteCell(sheet, column++, row, candle.CandleData.StochOscillator, CellStyleDecimalNormal);

                                if (bmCandles.TryGetValue(candle.OpenTime, out CryptoCandle? bmCandle))
                                {
                                    WriteCell(sheet, column++, row, bmCandle.Close, bmCandle.Close < 0 ? CellStylePercentageRed : bmCandle.Close > 0 ? CellStylePercentageGreen : CellStylePercentageNormal);
                                }

                            }
                            catch (Exception error)
                            {
                                // ignore more or less..
                                WriteCell(sheet, column++, row, error.Message);
                            }
                        }
                    }
                }
            }
        }

        AutoSize(sheet, columns);
    }

    public void ExportToExcel()
    {
        GlobalData.AddTextToLogTab($"Dumping signal to Excel");
        try
        {
            DumpCandeData();
            DumpInformation();

            StartExcell("Signal", Signal.Symbol.Name);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR candle dump " + error.ToString());
        }
    }
}