using CryptoScanBot.Core.Core;

using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.HSSF.Util;
using NPOI.SS.UserModel;

using System.Diagnostics;

namespace CryptoScanBot.Core.Excel;

public abstract class ExcelBase
{
    protected HSSFWorkbook Book;
    protected ICellStyle CellStyleDate;
    protected ICellStyle CellStyleDateRed;
    protected ICellStyle CellStyleStringGreen;
    protected ICellStyle CellStyleStringRed;
    protected ICellStyle CellStyleDecimalNormal;
    protected ICellStyle CellStyleDecimalGreen;
    protected ICellStyle CellStyleDecimalRed;
    protected ICellStyle CellStylePercentageNormal;
    protected ICellStyle CellStylePercentageGreen;
    protected ICellStyle CellStylePercentageRed;

    protected ExcelBase(string subject)
    {
        // HSSF => Microsoft Excel(excel 97-2003)
        // XSSF => Office Open XML Workbook(excel 2007)
        Book = new();

        //create a entry of DocumentSummaryInformation
        DocumentSummaryInformation documentSummaryInformation = PropertySetFactory.CreateDocumentSummaryInformation();
        documentSummaryInformation.Company = "Crypto Scanner";
        Book.DocumentSummaryInformation = documentSummaryInformation;

        //create a entry of SummaryInformation
        SummaryInformation summaryInformation = PropertySetFactory.CreateSummaryInformation();
        summaryInformation.Subject = subject;
        Book.SummaryInformation = summaryInformation;

        IDataFormat format = Book.CreateDataFormat();

        CellStyleDate = Book.CreateCellStyle();
        CellStyleDate.DataFormat = format.GetFormat("dd-MM-yyyy HH:mm");
        CellStyleDate.Alignment = HorizontalAlignment.Left;

        CellStyleDateRed = Book.CreateCellStyle();
        CellStyleDateRed.DataFormat = format.GetFormat("dd-MM-yyyy HH:mm");
        CellStyleDateRed.FillForegroundColor = HSSFColor.Red.Index;
        CellStyleDateRed.FillPattern = FillPattern.SolidForeground;
        CellStyleDateRed.FillBackgroundColor = HSSFColor.Red.Index;
        CellStyleDateRed.Alignment = HorizontalAlignment.Left;


        CellStyleStringGreen = Book.CreateCellStyle();
        CellStyleStringGreen.FillForegroundColor = HSSFColor.LightGreen.Index;
        CellStyleStringGreen.FillPattern = FillPattern.SolidForeground;
        CellStyleStringGreen.FillBackgroundColor = HSSFColor.LightGreen.Index;

        CellStyleStringRed = Book.CreateCellStyle();
        CellStyleStringRed.FillForegroundColor = HSSFColor.Red.Index;
        CellStyleStringRed.FillPattern = FillPattern.SolidForeground;
        CellStyleStringRed.FillBackgroundColor = HSSFColor.Red.Index;


        CellStyleDecimalNormal = Book.CreateCellStyle();
        CellStyleDecimalNormal.DataFormat = format.GetFormat("0.0000000000");

        CellStyleDecimalGreen = Book.CreateCellStyle();
        CellStyleDecimalGreen.DataFormat = format.GetFormat("0.0000000000");
        CellStyleDecimalGreen.FillForegroundColor = HSSFColor.LightGreen.Index;
        CellStyleDecimalGreen.FillPattern = FillPattern.SolidForeground;
        CellStyleDecimalGreen.FillBackgroundColor = HSSFColor.LightGreen.Index;

        CellStyleDecimalRed = Book.CreateCellStyle();
        CellStyleDecimalRed.DataFormat = format.GetFormat("0.0000000000");
        CellStyleDecimalRed.FillForegroundColor = HSSFColor.Red.Index;
        CellStyleDecimalRed.FillPattern = FillPattern.SolidForeground;
        CellStyleDecimalRed.FillBackgroundColor = HSSFColor.Red.Index;


        CellStylePercentageNormal = Book.CreateCellStyle();
        CellStylePercentageNormal.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");

        CellStylePercentageGreen = Book.CreateCellStyle();
        CellStylePercentageGreen.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
        CellStylePercentageGreen.FillForegroundColor = HSSFColor.LightGreen.Index;
        CellStylePercentageGreen.FillPattern = FillPattern.SolidForeground;
        CellStylePercentageGreen.FillBackgroundColor = HSSFColor.LightGreen.Index;

        CellStylePercentageRed = Book.CreateCellStyle();
        CellStylePercentageRed.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
        CellStylePercentageRed.FillForegroundColor = HSSFColor.Red.Index;
        CellStylePercentageRed.FillPattern = FillPattern.SolidForeground;
        CellStylePercentageRed.FillBackgroundColor = HSSFColor.Red.Index;

        //// macd.red
        //ICellStyle cellStyleMacdRed = book.CreateCellStyle();
        //cellStyleMacdRed.DataFormat = format.GetFormat("0.0000000000");
        //cellStyleMacdRed.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        //cellStyleMacdRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;
        //cellStyleMacdRed.FillPattern = FillPattern.SolidForeground;
        //cellStyleMacdRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Lime.Index;

        //// macd.roze
        //ICellStyle cellStyleMacdLightRed = book.CreateCellStyle();
        //cellStyleMacdLightRed.DataFormat = format.GetFormat("0.0000000000");
        //cellStyleMacdLightRed.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        //cellStyleMacdLightRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Rose.Index;
        //cellStyleMacdLightRed.FillPattern = FillPattern.SolidForeground;
        //cellStyleMacdLightRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Rose.Index;

        //// macd.green
        //ICellStyle cellStyleMacdGreen = book.CreateCellStyle();
        //cellStyleMacdGreen.DataFormat = format.GetFormat("0.0000000000");
        //cellStyleMacdGreen.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        //cellStyleMacdGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Green.Index;
        //cellStyleMacdGreen.FillPattern = FillPattern.SolidForeground;
        //cellStyleMacdGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Green.Index;

        //// macd.ligh green
        //ICellStyle cellStyleMacdLightGreen = book.CreateCellStyle();
        //cellStyleMacdLightGreen.DataFormat = format.GetFormat("0.0000000000");
        //cellStyleMacdLightGreen.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Right;
        //cellStyleMacdLightGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        //cellStyleMacdLightGreen.FillPattern = FillPattern.SolidForeground;
        //cellStyleMacdLightGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
    }

    protected static ICell WriteCell(ISheet sheet, int columnIndex, int rowIndex, string? value, ICellStyle? cellStyle = null)
        => sheet.GetOrCreateRow(rowIndex).GetOrCreateCell(columnIndex).WithValue(value).WithStyle(cellStyle);

    protected static ICell WriteCell(ISheet sheet, int columnIndex, int rowIndex, double? value, ICellStyle? cellStyle = null)
        => sheet.GetOrCreateRow(rowIndex).GetOrCreateCell(columnIndex).WithValue(value).WithStyle(cellStyle);

    protected static ICell WriteCell(ISheet sheet, int columnIndex, int rowIndex, decimal? value, ICellStyle? cellStyle = null)
        => sheet.GetOrCreateRow(rowIndex).GetOrCreateCell(columnIndex).WithValue((double?)value).WithStyle(cellStyle);

    protected static ICell WriteCell(ISheet sheet, int columnIndex, int rowIndex, int? value, ICellStyle? cellStyle = null)
        => sheet.GetOrCreateRow(rowIndex).GetOrCreateCell(columnIndex).WithValue(value).WithStyle(cellStyle);

    protected static ICell WriteCell(ISheet sheet, int columnIndex, int rowIndex, long? value, ICellStyle? cellStyle = null)
        => sheet.GetOrCreateRow(rowIndex).GetOrCreateCell(columnIndex).WithValue(value).WithStyle(cellStyle);

    protected static ICell WriteCell(ISheet sheet, int columnIndex, int rowIndex, DateTime? value, ICellStyle? cellStyle = null)
        => sheet.GetOrCreateRow(rowIndex).GetOrCreateCell(columnIndex).WithValue(value).WithStyle(cellStyle);

    protected static void AutoSize(ISheet sheet, int columns)
    {
        for (int i = 0; i < columns; i++)
        {
            sheet.AutoSizeColumn(i);
            sheet.SetColumnWidth(i, (int)(1.1 * sheet.GetColumnWidth(i)));
        }
    }

    protected void StartExcell(string title, string symbolName)
    {
        GlobalData.AddTextToLogTab($"Information dump {title} {symbolName}");

        string folder = GlobalData.GetBaseDir() + $@"\Excel\";
        Directory.CreateDirectory(folder);

        string filename = folder + symbolName + " " + title + ".xls";
        using var fs = new FileStream(filename, FileMode.Create);
        Book.Write(fs);

        Process.Start(new ProcessStartInfo(filename) { UseShellExecute = true });
    }
}