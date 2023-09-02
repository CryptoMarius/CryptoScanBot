using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;


namespace CryptoSbmScanner.Excel;

public class ExcelExchangeDump : ExcelBase
{
    private Model.CryptoExchange Exchange;

    private static int OHLCHeaders(HSSFSheet sheet, int row)
    {
        int column = 0;

        // Columns...
        WriteCell(sheet, column++, row, "Symbol");
        WriteCell(sheet, column++, row, "Base");
        WriteCell(sheet, column++, row, "Quote");
        WriteCell(sheet, column++, row, "Status");
        WriteCell(sheet, column++, row, "Volume");

        WriteCell(sheet, column++, row, "PriceTickSize");
        WriteCell(sheet, column++, row, "PriceMinimum");
        WriteCell(sheet, column++, row, "PriceMaximum");
        WriteCell(sheet, column++, row, "PriceDisplay");

        WriteCell(sheet, column++, row, "QuantityTickSize");
        WriteCell(sheet, column++, row, "QuantityMinimum");
        WriteCell(sheet, column++, row, "QuantityMaximum");
        WriteCell(sheet, column++, row, "QuantityDisplay");
        return column;
    }


    private void DumpSymbols()
    {
        HSSFSheet sheet = (HSSFSheet)Book.CreateSheet(Exchange.Name);

        int row = 0;
        int column;
        int columns = OHLCHeaders(sheet, 0);

        foreach (CryptoSymbol symbol in Exchange.SymbolListName.Values.ToList())
        {
            if (symbol.IsBarometerSymbol())
                continue;

            row++;
            column = 0;

            ICell cell;
            cell = WriteCell(sheet, column++, row, symbol.Name);
            //cell.CellStyle = CellStyleDate;

            cell = WriteCell(sheet, column++, row, symbol.Base);
            //cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, symbol.Quote);
            //cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, symbol.Status);
            //cell.CellStyle = CellStyleDate;

            cell = WriteCell(sheet, column++, row, (double)symbol.Volume);
            cell.CellStyle = CellStyleDecimalNormal;


            cell = WriteCell(sheet, column++, row, (double)symbol.PriceTickSize);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)symbol.PriceMinimum);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)symbol.PriceMaximum);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, symbol.PriceDisplayFormat);


            cell = WriteCell(sheet, column++, row, (double)symbol.QuantityTickSize);
            cell.CellStyle = CellStyleDecimalNormal;
            
            cell = WriteCell(sheet, column++, row, (double)symbol.QuantityMinimum);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)symbol.QuantityMaximum);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, symbol.QuantityDisplayFormat);
        }

        AutoSize(sheet, columns);
    }


    private void DumpInformation()
    {
        HSSFSheet sheet = (HSSFSheet)Book.CreateSheet("Information");

        WriteCell(sheet, 0, 1, "Exchange");
        WriteCell(sheet, 0, 2, "Aantal symbols");

        WriteCell(sheet, 1, 1, Exchange.Name);
        WriteCell(sheet, 1, 2, Exchange.SymbolListName.Count);

        AutoSize(sheet, 6);
    }


    public void ExportToExcell(Model.CryptoExchange exchange)
    {
        Exchange = exchange;
        try
        {
            CreateBook(Exchange.Name);
            CreateFormats();

            DumpInformation();
            DumpSymbols();

            StartExcell("Symbols", Exchange.Name, Exchange.Name);

        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("ERROR exchange dump " + error.ToString());
        }
    }
}
