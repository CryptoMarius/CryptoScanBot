﻿using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;

using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace CryptoSbmScanner.Intern;

public class PositionDumpDebug
{
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
        //WriteCell(sheet, 00, row, "BUY");
        //WriteCell(sheet, 11, row, "SELL");
        //row++;

        int column = 0;

        // Columns...
        WriteCell(sheet, column++, row, "Side");
        WriteCell(sheet, column++, row, "Create");
        WriteCell(sheet, column++, row, "Type");
        WriteCell(sheet, column++, row, "Close");
        WriteCell(sheet, column++, row, "Status");
        WriteCell(sheet, column++, row, "Trailing");
        WriteCell(sheet, column++, row, "Quantity");
        WriteCell(sheet, column++, row, "Price");
        WriteCell(sheet, column++, row, "StopLimit");
        WriteCell(sheet, column++, row, "Value");
        WriteCell(sheet, column++, row, "Commission");

        column++;

        WriteCell(sheet, column++, row, "Side");
        WriteCell(sheet, column++, row, "Create");
        WriteCell(sheet, column++, row, "Type");
        WriteCell(sheet, column++, row, "Close");
        WriteCell(sheet, column++, row, "Status");
        WriteCell(sheet, column++, row, "Trailing");
        WriteCell(sheet, column++, row, "Quantity");
        WriteCell(sheet, column++, row, "Price");
        WriteCell(sheet, column++, row, "StopLimit");
        WriteCell(sheet, column++, row, "Value");
        WriteCell(sheet, column++, row, "Commission");

        return column;
    }

    public string ExportToExcell(CryptoPosition position)
    {
        try
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
            summaryInformation.Subject = position.Symbol.Name;
            book.SummaryInformation = summaryInformation;

            IDataFormat format = book.CreateDataFormat();


            ICellStyle cellStyleDate = book.CreateCellStyle();
            cellStyleDate.DataFormat = format.GetFormat("dd-MM-yyyy HH:mm");
            cellStyleDate.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;

            //ICellStyle cellStyleStringGreen = book.CreateCellStyle();
            //cellStyleStringGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
            //cellStyleStringGreen.FillPattern = FillPattern.SolidForeground;
            //cellStyleStringGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;

            ICellStyle cellStyleDecimalNormal = book.CreateCellStyle();
            cellStyleDecimalNormal.DataFormat = format.GetFormat("0.00000000");

            //ICellStyle cellStyleDecimalGreen = book.CreateCellStyle();
            //cellStyleDecimalGreen.DataFormat = format.GetFormat("0.00000000");
            //cellStyleDecimalGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
            //cellStyleDecimalGreen.FillPattern = FillPattern.SolidForeground;
            //cellStyleDecimalGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;

            //ICellStyle cellStyleDecimalRed = book.CreateCellStyle();
            //cellStyleDecimalRed.DataFormat = format.GetFormat("0.00000000");
            //cellStyleDecimalRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;
            //cellStyleDecimalRed.FillPattern = FillPattern.SolidForeground;
            //cellStyleDecimalRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;


            //ICellStyle cellStylePercentageNormal = book.CreateCellStyle();
            //cellStylePercentageNormal.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");

            //ICellStyle cellStylePercentageGreen = book.CreateCellStyle();
            //cellStylePercentageGreen.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
            //cellStylePercentageGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
            //cellStylePercentageGreen.FillPattern = FillPattern.SolidForeground;
            //cellStylePercentageGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;


            //// macd.red
            //ICellStyle cellStyleMacdRed = book.CreateCellStyle();
            //cellStyleMacdRed.DataFormat = format.GetFormat("0.00000000");
            //cellStyleMacdRed.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
            //cellStyleMacdRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;
            //cellStyleMacdRed.FillPattern = FillPattern.SolidForeground;
            //cellStyleMacdRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Lime.Index;

            //// macd.roze
            //ICellStyle cellStyleMacdLightRed = book.CreateCellStyle();
            //cellStyleMacdLightRed.DataFormat = format.GetFormat("0.00000000");
            //cellStyleMacdLightRed.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
            //cellStyleMacdLightRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Rose.Index;
            //cellStyleMacdLightRed.FillPattern = FillPattern.SolidForeground;
            //cellStyleMacdLightRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Rose.Index;

            //// macd.green
            //ICellStyle cellStyleMacdGreen = book.CreateCellStyle();
            //cellStyleMacdGreen.DataFormat = format.GetFormat("0.00000000");
            //cellStyleMacdGreen.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
            //cellStyleMacdGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Green.Index;
            //cellStyleMacdGreen.FillPattern = FillPattern.SolidForeground;
            //cellStyleMacdGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Green.Index;

            //// macd.ligh green
            //ICellStyle cellStyleMacdLightGreen = book.CreateCellStyle();
            //cellStyleMacdLightGreen.DataFormat = format.GetFormat("0.00000000");
            //cellStyleMacdLightGreen.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
            //cellStyleMacdLightGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
            //cellStyleMacdLightGreen.FillPattern = FillPattern.SolidForeground;
            //cellStyleMacdLightGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;

            HSSFSheet sheet = (HSSFSheet)book.CreateSheet(position.Symbol.Name);

            // Er zijn 2 rijen met headers
            int columns = ExcellHeaders(sheet, 0);

            ICell cell;
            int row = 1;
            foreach (CryptoPositionPart part in position.Parts.Values.ToList())
            {
                // oude methode
                //foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                //{
                //    // Geannuleerde order of openstaande orders overslagen
                //    if (step.Status == CryptoOrderStatus.Expired || step.Status == CryptoOrderStatus.Canceled || !step.CloseTime.HasValue)
                //        continue;

                //    int column;
                //    if (step.Side == CryptoOrderSide.Buy)
                //    {
                //        ++row;
                //        column = 0;
                //    }
                //    else
                //        column = 7;

                //    cell = WriteCell(sheet, column++, row, (DateTime)step.CloseTime?.ToLocalTime());
                //    cell.CellStyle = cellStyleDate;

                //    cell = WriteCell(sheet, column++, row, step.Status.ToString());
                //    //cell.CellStyle = cellStyleDecimalNormal;

                //    cell = WriteCell(sheet, column++, row, (double)step.Quantity);
                //    cell.CellStyle = cellStyleDecimalNormal;

                //    // wat is de werkelijke prijs (stopprice of normale price)?
                //    // Gekozen om dit ter plekke uit te rekenen (is tevens beter met market orders die over meerdere trades gaan)
                //    cell = WriteCell(sheet, column++, row, (double)step.QuoteQuantityFilled / (double)step.Quantity);
                //    cell.CellStyle = cellStyleDecimalNormal;

                //    cell = WriteCell(sheet, column++, row, (double)step.QuoteQuantityFilled);
                //    cell.CellStyle = cellStyleDecimalNormal;

                //    cell = WriteCell(sheet, column++, row, (double)step.Commission);
                //    cell.CellStyle = cellStyleDecimalNormal;
                //}

                // Nieuwe methode (geen filters)
                //int buyRow = row;
                //int sellRow = row;

                cell = WriteCell(sheet, 0, row, part.Name);
                cell.CellStyle = cellStyleDate;

                //cell = WriteCell(sheet, 0, row, (DateTime)part.CreateTime.ToLocalTime());
                //cell.CellStyle = cellStyleDate;

                //cell = WriteCell(sheet, 0, row, part.SideText);
                //cell.CellStyle = cellStyleDate;


                foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                {
                    ++row;
                    // Geannuleerde order of openstaande orders overslagen
                    //if (step.Status == CryptoOrderStatus.Expired || step.Status == CryptoOrderStatus.Canceled || !step.CloseTime.HasValue)
                    //    continue;

                    int column;
                    if (step.Side == CryptoOrderSide.Buy)
                    {
                        //buyRow++;
                        column = 0;
                        //row = buyRow;
                    }
                    else
                    {
                        //sellRow++;
                        column = 12;
                        //row = sellRow;
                    }

                    cell = WriteCell(sheet, column++, row, step.Side.ToString());
                    //cell.CellStyle = cellStyleDecimalNormal;

                    cell = WriteCell(sheet, column++, row, (DateTime)step.CreateTime.ToLocalTime());
                    cell.CellStyle = cellStyleDate;

                    if (step.CloseTime.HasValue)
                    {
                        cell = WriteCell(sheet, column++, row, (DateTime)step.CloseTime?.ToLocalTime());
                        cell.CellStyle = cellStyleDate;
                    }
                    else column++;

                    cell = WriteCell(sheet, column++, row, step.OrderType.ToString());
                    //cell.CellStyle = cellStyleDecimalNormal;

                    cell = WriteCell(sheet, column++, row, step.Status.ToString());
                    //cell.CellStyle = cellStyleDecimalNormal;

                    cell = WriteCell(sheet, column++, row, step.Trailing.ToString());
                    //cell.CellStyle = cellStyleDecimalNormal;

                    cell = WriteCell(sheet, column++, row, (double)step.Quantity);
                    cell.CellStyle = cellStyleDecimalNormal;

                    // wat is de werkelijke prijs (stopprice of normale price)?
                    // Gekozen om dit ter plekke uit te rekenen (is tevens beter met market orders die over meerdere trades gaan)
                    //cell = WriteCell(sheet, column++, row, (double)step.QuoteQuantityFilled / (double)step.Quantity);
                    cell = WriteCell(sheet, column++, row, (double)step.Price);
                    cell.CellStyle = cellStyleDecimalNormal;

                    if (step.StopPrice.HasValue)
                    {
                        cell = WriteCell(sheet, column++, row, (double)step.StopPrice);
                        cell.CellStyle = cellStyleDecimalNormal;
                    }
                    else column++;

                    cell = WriteCell(sheet, column++, row, (double)step.QuoteQuantityFilled);
                    cell.CellStyle = cellStyleDecimalNormal;

                    cell = WriteCell(sheet, column++, row, (double)step.Commission);
                    cell.CellStyle = cellStyleDecimalNormal;
                }
                //row = Math.Max(buyRow, sellRow);
                ++row;
                ++row;
            }

            ++row;
            ++row;
            int x = 11;
            cell = WriteCell(sheet, x++, row, (double)position.BreakEvenPrice);
            cell.CellStyle = cellStyleDecimalNormal;


            for (int i = 0; i < columns; i++)
            {
                sheet.AutoSizeColumn(i);
                int width = sheet.GetColumnWidth(i);
                sheet.SetColumnWidth(i, (int)(1.1 * width));
            }



            // De BreakEven calculation in een aparte sheet
            sheet = (HSSFSheet)book.CreateSheet("BE");
            row = 2;

            WriteCell(sheet, 0, row, "Side");
            WriteCell(sheet, 1, row, "Create");
            WriteCell(sheet, 2, row, "Closed");
            WriteCell(sheet, 3, row, "Price");
            WriteCell(sheet, 4, row, "Quantity");
            WriteCell(sheet, 5, row, "QuoteQuantity");
            WriteCell(sheet, 6, row, "Commission");
            WriteCell(sheet, 7, row, "BreakEven");

            decimal be;
            decimal totalValue = 0;
            decimal totalQuantity = 0;
            decimal totalCommission = 0;
            foreach (CryptoPositionPart part in position.Parts.Values.ToList())
            {
                foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                {
                    // Geannuleerde order of openstaande orders overslagen
                    if (step.Status == CryptoOrderStatus.Expired || step.Status == CryptoOrderStatus.Canceled || !step.CloseTime.HasValue)
                        continue;

                    ++row;
                    int column = 0;
                    int factor = 1;
                    if (step.Side == CryptoOrderSide.Buy)
                        factor = -1;

                    cell = WriteCell(sheet, column++, row, step.Side.ToString());
                    //cell.CellStyle = cellStyleDecimalNormal;

                    cell = WriteCell(sheet, column++, row, (DateTime)step.CreateTime.ToLocalTime());
                    cell.CellStyle = cellStyleDate;

                    cell = WriteCell(sheet, column++, row, (DateTime)step.CloseTime?.ToLocalTime());
                    cell.CellStyle = cellStyleDate;

                    cell = WriteCell(sheet, column++, row, (double)(step.QuoteQuantityFilled / step.Quantity)); // gem. prijs
                    cell.CellStyle = cellStyleDecimalNormal;

                    cell = WriteCell(sheet, column++, row, (double)step.Quantity);
                    cell.CellStyle = cellStyleDecimalNormal;
                    totalQuantity += factor * step.QuantityFilled;

                    cell = WriteCell(sheet, column++, row, factor  * (double)step.QuoteQuantityFilled);
                    cell.CellStyle = cellStyleDecimalNormal;
                    totalValue += factor * step.QuoteQuantityFilled;

                    cell = WriteCell(sheet, column++, row, (double)step.Commission);
                    cell.CellStyle = cellStyleDecimalNormal;

                    totalCommission += step.Commission;

                    be = 0;
                    if (totalQuantity != 0)
                        be = (totalValue - totalCommission) / totalQuantity;
                    cell = WriteCell(sheet, column++, row, (double)be);
                    cell.CellStyle = cellStyleDecimalNormal;
                }
            }

            for (int i = 0; i < columns; i++)
            {
                sheet.AutoSizeColumn(i);
                int width = sheet.GetColumnWidth(i);
                sheet.SetColumnWidth(i, (int)(1.1 * width));
            }

            // Na de resize de lange title erin zetten
            WriteCell(sheet, 0, 0, "BreakEven Calculation");


            GlobalData.AddTextToLogTab($"Position Debug dump {position.Symbol.Name} {position.Id}");

            string folder = GlobalData.GetBaseDir() + $@"\Debug\";
            Directory.CreateDirectory(folder);

            string filename = folder + position.Symbol.Name + ".xls";
            using var fs = new FileStream(filename, FileMode.Create);
            book.Write(fs);

            return filename;
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("ERROR postion display " + error.ToString());
        }
        return "";
    }
}
