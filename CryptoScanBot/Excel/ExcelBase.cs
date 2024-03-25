using CryptoScanBot.Intern;

using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace CryptoScanBot.Excel;

public class ExcelBase
{
    public HSSFWorkbook Book { get; set; }
    public ICellStyle CellStyleDate { get; set; }

    public ICellStyle CellStyleStringGreen { get; set; }
    public ICellStyle CellStyleStringRed { get; set; }

    public ICellStyle CellStyleDecimalNormal { get; set; }
    public ICellStyle CellStyleDecimalGreen { get; set; }
    public ICellStyle CellStyleDecimalRed { get; set; }

    public ICellStyle CellStylePercentageNormal { get; set; }
    public ICellStyle CellStylePercentageGreen { get; set; }
    public ICellStyle CellStylePercentageRed { get; set; }


    public void CreateBook(string subject)
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
    }


    public void CreateFormats()
    {
        IDataFormat format = Book.CreateDataFormat();

        CellStyleDate = Book.CreateCellStyle();
        CellStyleDate.DataFormat = format.GetFormat("dd-MM-yyyy HH:mm");
        CellStyleDate.Alignment = NPOI.SS.UserModel.HorizontalAlignment.Left;

        CellStyleStringGreen = Book.CreateCellStyle();
        CellStyleStringGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        CellStyleStringGreen.FillPattern = FillPattern.SolidForeground;
        CellStyleStringGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;

        CellStyleStringRed = Book.CreateCellStyle();
        CellStyleStringRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;
        CellStyleStringRed.FillPattern = FillPattern.SolidForeground;
        CellStyleStringRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;


        CellStyleDecimalNormal = Book.CreateCellStyle();
        CellStyleDecimalNormal.DataFormat = format.GetFormat("0.0000000000");

        CellStyleDecimalGreen = Book.CreateCellStyle();
        CellStyleDecimalGreen.DataFormat = format.GetFormat("0.0000000000");
        CellStyleDecimalGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        CellStyleDecimalGreen.FillPattern = FillPattern.SolidForeground;
        CellStyleDecimalGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;

        CellStyleDecimalRed = Book.CreateCellStyle();
        CellStyleDecimalRed.DataFormat = format.GetFormat("0.0000000000");
        CellStyleDecimalRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;
        CellStyleDecimalRed.FillPattern = FillPattern.SolidForeground;
        CellStyleDecimalRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.Red.Index;


        CellStylePercentageNormal = Book.CreateCellStyle();
        CellStylePercentageNormal.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");

        CellStylePercentageGreen = Book.CreateCellStyle();
        CellStylePercentageGreen.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
        CellStylePercentageGreen.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        CellStylePercentageGreen.FillPattern = FillPattern.SolidForeground;
        CellStylePercentageGreen.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;

        CellStylePercentageRed = Book.CreateCellStyle();
        CellStylePercentageRed.DataFormat = HSSFDataFormat.GetBuiltinFormat("0.00");
        CellStylePercentageRed.FillForegroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;
        CellStylePercentageRed.FillPattern = FillPattern.SolidForeground;
        CellStylePercentageRed.FillBackgroundColor = NPOI.HSSF.Util.HSSFColor.LightGreen.Index;

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

    public static void AutoSize(HSSFSheet sheet, int columns)
    {
        for (int i = 0; i < columns; i++)
        {
            sheet.AutoSizeColumn(i);
            double width = sheet.GetColumnWidth(i);
            sheet.SetColumnWidth(i, (int)(1.1 * width));
        }
    }

    public void StartExcell(string title, string symbolName, string exchangeName)
    {
        GlobalData.AddTextToLogTab($"Information dump {title} {symbolName}");

        string folder = GlobalData.GetBaseDir() + $@"\Excel\"; //{exchangeName}\
        Directory.CreateDirectory(folder);

        string filename = folder + symbolName + " " + title + ".xls";
        using var fs = new FileStream(filename, FileMode.Create);
        Book.Write(fs);

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(filename) { UseShellExecute = true });
    }
}

