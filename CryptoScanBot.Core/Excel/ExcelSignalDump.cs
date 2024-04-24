using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;


namespace CryptoScanBot.Core.Excel;

public class ExcelSignalDump : ExcelBase
{
    Core.Model.CryptoExchange Exchange;
    CryptoSymbol Symbol;
    CryptoSignal Signal;


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
        int columns = 2;
        int startRow = row;
        WriteCell(sheet, 0, row++, "Exchange");
        WriteCell(sheet, 0, row++, "Symbol");
        WriteCell(sheet, 0, row++, "Strategy");
        WriteCell(sheet, 0, row++, "Interval");

        row = startRow;
        WriteCell(sheet, 1, row++, Symbol.Exchange.Name);
        WriteCell(sheet, 1, row++, Symbol.Name);
        WriteCell(sheet, 1, row++, Signal.StrategyText);
        WriteCell(sheet, 1, row++, Signal.Interval.Name);


        AutoSize(sheet, columns);
    }


    private void DumpCandeData()
    {
        ICell cell;
        int columns = 0;
        HSSFSheet sheet = (HSSFSheet)Book.CreateSheet("Signal");

        if (Exchange.SymbolListName.TryGetValue(Constants.SymbolNameBarometerPrice + Symbol.Quote, out CryptoSymbol bmSymbol))
        {
            CryptoSymbolInterval bmSymbolInterval = bmSymbol.GetSymbolInterval(CryptoIntervalPeriod.interval1h);
            SortedList<long, CryptoCandle> bmCandles = bmSymbolInterval.CandleList;

            CryptoSymbolInterval symbolInterval = Symbol.GetSymbolInterval(Signal.Interval.IntervalPeriod);
            if (symbolInterval.CandleList.Count > 0)
            {
                CryptoCandle candleLast = symbolInterval.CandleList.Values.Last();
                if (CandleIndicatorData.PrepareIndicators(Symbol, symbolInterval, candleLast, out _))
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

                    WriteCell(sheet, columns++, row, "K.Lower");
                    WriteCell(sheet, columns++, row, "K.Center");
                    WriteCell(sheet, columns++, row, "K.Upper");
#if EXTRASTRATEGIESSLOPEKELTNER
                    WriteCell(sheet, columns++, row, "K.Slope");
#endif

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
                                if (Signal.OpenDate == candle.Date)
                                    cell = WriteCell(sheet, column++, row, "Signal");
                                else
                                    cell = WriteCell(sheet, column++, row, "");

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

                                cell = WriteCell(sheet, column++, row, candle.CandleData.BollingerBandsLowerBand);
                                cell.CellStyle = CellStyleDecimalNormal;
                                cell = WriteCell(sheet, column++, row, candle.CandleData.Sma20);
                                cell.CellStyle = CellStyleDecimalNormal;
                                cell = WriteCell(sheet, column++, row, candle.CandleData.BollingerBandsUpperBand);
                                cell.CellStyle = CellStyleDecimalNormal;
                                cell = WriteCell(sheet, column++, row, candle.CandleData.BollingerBandsPercentage);
                                cell.CellStyle = CellStylePercentageNormal;


                                cell = WriteCell(sheet, column++, row, candle.CandleData.KeltnerLowerBand);
                                cell.CellStyle = CellStyleDecimalNormal;
                                cell = WriteCell(sheet, column++, row, candle.CandleData.KeltnerCenterLine);
                                cell.CellStyle = CellStyleDecimalNormal;
                                cell = WriteCell(sheet, column++, row, candle.CandleData.KeltnerUpperBand);
                                cell.CellStyle = CellStyleDecimalNormal;
#if EXTRASTRATEGIESSLOPEKELTNER
                                cell = WriteCell(sheet, column++, row, candle.CandleData.KeltnerCenterLineSlope);
                                cell.CellStyle = CellStyleDecimalNormal;
#endif

                                cell = WriteCell(sheet, column++, row, candle.CandleData.Sma20);
                                cell.CellStyle = CellStyleDecimalNormal;
                                cell = WriteCell(sheet, column++, row, candle.CandleData.Sma50);
                                cell.CellStyle = CellStyleDecimalNormal;
                                cell = WriteCell(sheet, column++, row, candle.CandleData.Sma200);
                                cell.CellStyle = CellStyleDecimalNormal;
                                cell = WriteCell(sheet, column++, row, candle.CandleData.PSar);
                                cell.CellStyle = CellStyleDecimalNormal;

                                cell = WriteCell(sheet, column++, row, candle.CandleData.MacdValue);
                                cell.CellStyle = CellStyleDecimalNormal;
                                cell = WriteCell(sheet, column++, row, candle.CandleData.MacdSignal);
                                cell.CellStyle = CellStyleDecimalNormal;
                                cell = WriteCell(sheet, column++, row, candle.CandleData.MacdHistogram);
                                cell.CellStyle = CellStyleDecimalNormal;

                                cell = WriteCell(sheet, column++, row, candle.CandleData.Rsi);
                                cell.CellStyle = CellStyleDecimalNormal;

                                cell = WriteCell(sheet, column++, row, candle.CandleData.StochSignal);
                                cell.CellStyle = CellStyleDecimalNormal;
                                cell = WriteCell(sheet, column++, row, candle.CandleData.StochOscillator);
                                cell.CellStyle = CellStyleDecimalNormal;

                                if (bmCandles.TryGetValue(candle.OpenTime, out CryptoCandle bmCandle))
                                {
                                    cell = WriteCell(sheet, column++, row, (double)bmCandle.Close);
                                    if (bmCandle.Close < 0)
                                        cell.CellStyle = CellStylePercentageRed;
                                    else if (bmCandle.Close > 0)
                                        cell.CellStyle = CellStylePercentageGreen;
                                }

                            }
                            catch (Exception error)
                            {
                                // ignore more or less..
                                cell = WriteCell(sheet, column++, row, error.Message);
                            }
                        }
                    }
                }
            }
        }

        AutoSize(sheet, columns);
    }

    public void ExportToExcel(CryptoSignal signal)
    {
        Signal = signal;
        Symbol = signal.Symbol;
        Exchange = Symbol.Exchange;
        try
        {
            CreateBook(Symbol.Name);
            CreateFormats();

            DumpCandeData();
            DumpInformation();

            StartExcell("Signal", Symbol.Name, Symbol.Exchange.Name);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR candle dump " + error.ToString());
        }
    }
}
