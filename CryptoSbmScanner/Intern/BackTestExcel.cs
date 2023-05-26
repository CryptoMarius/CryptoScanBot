using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CryptoExchange.Net.CommonObjects;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Signal;

using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.OpenXmlFormats.Wordprocessing;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using NPOI.XWPF.UserModel;

using Org.BouncyCastle.Utilities.Collections;

using Skender.Stock.Indicators;

using ICell = NPOI.SS.UserModel.ICell;

namespace CryptoSbmScanner.Intern;

public class BackTestExcel
{
    private CryptoSymbol Symbol { get; set; }
    private List<CryptoCandle> History { get; set; }

    public BackTestExcel(CryptoSymbol symbol, List<CryptoCandle> history)
    {
        Symbol = symbol;
        History = history;
    }

    public static ICell WriteCell(ISheet sheet, int columnIndex, int rowIndex, string value)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        var cell = row.GetCell(columnIndex) ?? row.CreateCell(columnIndex);

        cell.SetCellValue(value);
        return cell;
    }

    public static ICell WriteCell(ISheet sheet, int columnIndex, int rowIndex, double value)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        var cell = row.GetCell(columnIndex) ?? row.CreateCell(columnIndex);

        cell.SetCellValue(value);
        return cell;
    }

    public static ICell WriteCell(ISheet sheet, int columnIndex, int rowIndex, DateTime value)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        var cell = row.GetCell(columnIndex) ?? row.CreateCell(columnIndex);

        cell.SetCellValue(value);
        return cell;
    }

    public static ICell WriteStyle(ISheet sheet, int columnIndex, int rowIndex, ICellStyle style)
    {
        var row = sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
        ICell cell = row.GetCell(columnIndex) ?? row.CreateCell(columnIndex);

        cell.CellStyle = style;
        return cell;
    }

    private static int ExcellHeaders(HSSFSheet sheet, int row)
    {
        int column = 0;
        // Columns...
        WriteCell(sheet, column++, row, "DateLocal");
        WriteCell(sheet, column++, row, "Open");
        WriteCell(sheet, column++, row, "High");
        WriteCell(sheet, column++, row, "Low");
        WriteCell(sheet, column++, row, "Close");
        WriteCell(sheet, column++, row, "Volume");

        WriteCell(sheet, column++, row, "bb.lower");
        WriteCell(sheet, column++, row, "bb.upper");
        WriteCell(sheet, column++, row, "bb.perc");

        WriteCell(sheet, column++, row, "Sma200");
        WriteCell(sheet, column++, row, "Sma50");
        WriteCell(sheet, column++, row, "Sma20");
        WriteCell(sheet, column++, row, "PSar");

        WriteCell(sheet, column++, row, "macd.value");
        WriteCell(sheet, column++, row, "macd.signal");
        WriteCell(sheet, column++, row, "macd.hist");
        WriteCell(sheet, column++, row, "SBM conditions");
        WriteCell(sheet, column++, row, "200/20");
        WriteCell(sheet, column++, row, "200/50");
        WriteCell(sheet, column++, row, "50/20");
        WriteCell(sheet, column++, row, "recovery text");

        WriteCell(sheet, column++, row, "Rsi");
        WriteCell(sheet, column++, row, "stoch.ocillator");
        WriteCell(sheet, column++, row, "stoch.signal");

        // wat kun je hiermee?     
        WriteCell(sheet, column++, row, "Ema20");
        WriteCell(sheet, column++, row, "Ema50");
        WriteCell(sheet, column++, row, "Ema200");
        return column;
    }

    public void ExportToExcell(TradeDirection mode, SignalStrategy strategy)
    {
        // HSSF => Microsoft Excel(excel 97-2003)
        // XSSF => Office Open XML Workbook(excel 2007)
        HSSFWorkbook book = new();

        //create a entry of DocumentSummaryInformation
        DocumentSummaryInformation documentSummaryInformation = PropertySetFactory.CreateDocumentSummaryInformation();
        documentSummaryInformation.Company = "Crypto Scanner";
        book.DocumentSummaryInformation = documentSummaryInformation;

        //create a entry of SummaryInformation
        SummaryInformation summaryInformation = PropertySetFactory.CreateSummaryInformation();
        summaryInformation.Subject = Symbol.Name;
        book.SummaryInformation = summaryInformation;

        IDataFormat format = book.CreateDataFormat();

        HSSFSheet sheet = (HSSFSheet)book.CreateSheet("Sheet1");


        ICellStyle cellStyleDate = book.CreateCellStyle();
        cellStyleDate.DataFormat = format.GetFormat("dd-MM-yyyy HH:mm");
        cellStyleDate.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;

        ICellStyle cellStyleStringGreen = book.CreateCellStyle();
        cellStyleStringGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        cellStyleStringGreen.FillPattern = FillPattern.SolidForeground;
        cellStyleStringGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;

        ICellStyle cellStyleDecimalNormal = book.CreateCellStyle();
        cellStyleDecimalNormal.DataFormat = format.GetFormat("0.00000000");

        ICellStyle cellStyleDecimalGreen = book.CreateCellStyle();
        cellStyleDecimalGreen.DataFormat = format.GetFormat("0.00000000");
        cellStyleDecimalGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        cellStyleDecimalGreen.FillPattern = FillPattern.SolidForeground;
        cellStyleDecimalGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;

        ICellStyle cellStyleDecimalRed = book.CreateCellStyle();
        cellStyleDecimalRed.DataFormat = format.GetFormat("0.00000000");
        cellStyleDecimalRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;
        cellStyleDecimalRed.FillPattern = FillPattern.SolidForeground;
        cellStyleDecimalRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;


        ICellStyle cellStylePercentageNormal = book.CreateCellStyle();
        cellStylePercentageNormal.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");

        ICellStyle cellStylePercentageGreen = book.CreateCellStyle();
        cellStylePercentageGreen.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
        cellStylePercentageGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        cellStylePercentageGreen.FillPattern = FillPattern.SolidForeground;
        cellStylePercentageGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;


        // macd.red
        ICellStyle cellStyleMacdRed = book.CreateCellStyle();
        cellStyleMacdRed.DataFormat = format.GetFormat("0.00000000");
        cellStyleMacdRed.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        cellStyleMacdRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;
        cellStyleMacdRed.FillPattern = FillPattern.SolidForeground;
        cellStyleMacdRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Lime.Index;

        // macd.roze
        ICellStyle cellStyleMacdLightRed = book.CreateCellStyle();
        cellStyleMacdLightRed.DataFormat = format.GetFormat("0.00000000");
        cellStyleMacdLightRed.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        cellStyleMacdLightRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Rose.Index;
        cellStyleMacdLightRed.FillPattern = FillPattern.SolidForeground;
        cellStyleMacdLightRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Rose.Index;

        // macd.green
        ICellStyle cellStyleMacdGreen = book.CreateCellStyle();
        cellStyleMacdGreen.DataFormat = format.GetFormat("0.00000000");
        cellStyleMacdGreen.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        cellStyleMacdGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Green.Index;
        cellStyleMacdGreen.FillPattern = FillPattern.SolidForeground;
        cellStyleMacdGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Green.Index;

        // macd.ligh green
        ICellStyle cellStyleMacdLightGreen = book.CreateCellStyle();
        cellStyleMacdLightGreen.DataFormat = format.GetFormat("0.00000000");
        cellStyleMacdLightGreen.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        cellStyleMacdLightGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        cellStyleMacdLightGreen.FillPattern = FillPattern.SolidForeground;
        cellStyleMacdLightGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;


        int columns = ExcellHeaders(sheet, 0);

        int row = 0;
        int column;
        CryptoCandle prev = null;
        for (int i = 0; i < History.Count; i++)
        {
            row++;
            column = 0;
            CryptoCandle candle = History[i];

            ICell cell;
            cell = WriteCell(sheet, column++, row, candle.DateLocal);
            cell.CellStyle = cellStyleDate;

            cell = WriteCell(sheet, column++, row, (double)candle.Open);
            cell.CellStyle = cellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)candle.High);
            cell.CellStyle = cellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)candle.Low);
            cell.CellStyle = cellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)candle.Close);
            cell.CellStyle = cellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)candle.Volume);
            cell.CellStyle = cellStyleDecimalNormal;


            if (candle.CandleData != null)
            {
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.BollingerBandsLowerBand);
                cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.BollingerBandsUpperBand);
                cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.BollingerBandsPercentage);
                if (candle.CandleData.BollingerBandsPercentage >= 1.5)
                    cell.CellStyle = cellStylePercentageGreen;
                else
                    cell.CellStyle = cellStylePercentageNormal;

                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.Sma200);
                cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.Sma50);
                if (candle.CandleData.Sma200 >= candle.CandleData.Sma50)
                    cell.CellStyle = cellStyleDecimalGreen;
                else
                    cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.Sma20);
                if (candle.CandleData.Sma50 >= candle.CandleData.Sma20)
                    cell.CellStyle = cellStyleDecimalGreen;
                else
                    cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.PSar);
                if (candle.CandleData.Sma20 > candle.CandleData.PSar)
                    cell.CellStyle = cellStyleDecimalGreen;
                else
                    cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.MacdValue);
                cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.MacdSignal);
                cell.CellStyle = cellStyleDecimalNormal;

                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.MacdHistogram);
                if (candle.CandleData.MacdHistogram >= 0)
                {
                    // above zero line = green
                    if (prev == null || prev.CandleData == null)
                        cell.CellStyle = cellStyleDecimalNormal;
                    else
                    {
                        if (candle.CandleData.MacdHistogram >= prev.CandleData.MacdHistogram)
                            cell.CellStyle = cellStyleDecimalGreen;
                        else
                            cell.CellStyle = cellStyleMacdLightGreen;
                    }
                }
                else
                {
                    // below zero line = red
                    if (prev == null || prev.CandleData == null)
                        cell.CellStyle = cellStyleDecimalNormal;
                    else
                    {
                        if (candle.CandleData.MacdHistogram <= prev.CandleData.MacdHistogram)
                            cell.CellStyle = cellStyleMacdRed;
                        else
                            cell.CellStyle = cellStyleMacdLightRed;
                    }
                }


                if (candle.IsSbmConditionsOversold(true))
                {
                    WriteCell(sheet, column++, row, "yes");
                }
                else
                {
                    WriteCell(sheet, column++, row, "no");
                }

                if (candle.IsSma200AndSma20OkayOversold(GlobalData.Settings.Signal.SbmMa200AndMa20Percentage, out string _))
                {
                    cell = WriteCell(sheet, column++, row, "yes");
                    cell.CellStyle = cellStyleStringGreen;
                }
                else
                {
                    WriteCell(sheet, column++, row, "no");
                }

                if (candle.IsSma200AndSma50OkayOversold(GlobalData.Settings.Signal.SbmMa200AndMa50Percentage, out _))
                {
                    cell = WriteCell(sheet, column++, row, "yes");
                    cell.CellStyle = cellStyleStringGreen;
                }
                else
                {
                    WriteCell(sheet, column++, row, "no");
                }

                if (candle.IsSma50AndSma20OkayOversold(GlobalData.Settings.Signal.SbmMa50AndMa20Percentage, out _))
                {
                    cell = WriteCell(sheet, column++, row, "yes");
                    cell.CellStyle = cellStyleStringGreen;
                }
                else
                {
                    WriteCell(sheet, column++, row, "no");
                }

                //WriteCell(sheet, column++, row, candle.ExtraText);


                // overbodig?

                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.Rsi);
                if (candle.CandleData.Rsi > 30)
                    cell.CellStyle = cellStyleDecimalGreen;
                else
                    cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.StochOscillator);
                if (candle.CandleData.StochOscillator > 20)
                    cell.CellStyle = cellStyleDecimalGreen;
                else
                    cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.StochSignal);
                if (candle.CandleData.StochSignal > 20)
                    cell.CellStyle = cellStyleDecimalGreen;
                else
                    cell.CellStyle = cellStyleDecimalNormal;

                // wat kun je hiermee?
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.Ema20);
                cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.Ema50);
                cell.CellStyle = cellStyleDecimalNormal;
                cell = WriteCell(sheet, column++, row, (double)candle.CandleData.Ema200);
                cell.CellStyle = cellStyleDecimalNormal;
            }
            prev = candle;
        }

        // wel makkelijk als ze ook onderin staan
        ExcellHeaders(sheet, row + 1);

        for (int i = 0; i < columns; i++)
        {
            sheet.AutoSizeColumn(i);
            int width = sheet.GetColumnWidth(i);
            sheet.SetColumnWidth(i, (int)(1.1 * width));
        }


        string text = SignalHelper.GetSignalAlgorithmText(strategy);
        GlobalData.AddTextToLogTab(string.Format("Backtest {0} {1} ready", Symbol.Name, text));

        string folder = GlobalData.GetBaseDir() + @"\BackTest\";
        Directory.CreateDirectory(folder);
        using var fs = new FileStream(folder + Symbol.Name + "-" + text + ".xls", FileMode.Create);

        book.Write(fs);
    }

}
