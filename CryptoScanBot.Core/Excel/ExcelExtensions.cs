using NPOI.SS.UserModel;

namespace CryptoScanBot.Core.Excel;

public static class ExcelExtensions
{
    public static IRow GetOrCreateRow(this ISheet sheet, int rowIndex) => sheet.GetRow(rowIndex) ?? sheet.CreateRow(rowIndex);
    public static ICell GetOrCreateCell (this IRow sheet, int columnIndex) => sheet.GetCell(columnIndex) ?? sheet.CreateCell(columnIndex);
    public static ICell WithValue(this ICell cell, double? value)
    {
        if (value.HasValue)
            cell.SetCellValue(value.Value);
        return cell;
    }
    public static ICell WithValue(this ICell cell, string? value)
    {
        if (!string.IsNullOrEmpty(value))
            cell.SetCellValue(value);
        return cell;
    }
    public static ICell WithValue(this ICell cell, DateTime? value)
    {
        if (value.HasValue) 
            cell.SetCellValue(value.Value);
        return cell;
    }
    public static ICell WithStyle(this ICell cell, ICellStyle? style)
    {      
        if(style != null)
            cell.CellStyle = style;
        return cell;
    }
}