using CryptoScanBot.Intern;
using CryptoScanBot.Model;

using NPOI.HPSF;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;


namespace CryptoScanBot.Excel;

public class ExcelExchangeDump : ExcelBase
{
    private Model.CryptoExchange Exchange;

    private static int OHLCHeaders(HSSFSheet sheet, int row)
    {
        int column = 0;

        // Columns...
        WriteCell(sheet, column++, row, "Id");
        WriteCell(sheet, column++, row, "Symbol");
        WriteCell(sheet, column++, row, "Base");
        WriteCell(sheet, column++, row, "Quote");
        WriteCell(sheet, column++, row, "Status");
        WriteCell(sheet, column++, row, "Volume");

        WriteCell(sheet, column++, row, "Price TickSize");
        WriteCell(sheet, column++, row, "Price Minimum");
        WriteCell(sheet, column++, row, "Price Maximum");
        WriteCell(sheet, column++, row, "Price Display");

        WriteCell(sheet, column++, row, "Quantity TickSize");
        WriteCell(sheet, column++, row, "Quantity Minimum");
        WriteCell(sheet, column++, row, "Quantity Maximum");
        WriteCell(sheet, column++, row, "Quantity Display");

        WriteCell(sheet, column++, row, "Quote Min");
        WriteCell(sheet, column++, row, "Quote Max");

        WriteCell(sheet, column++, row, "LastPrice");
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
            WriteCell(sheet, column++, row, symbol.Id);
            WriteCell(sheet, column++, row, symbol.Name);
            WriteCell(sheet, column++, row, symbol.Base);
            WriteCell(sheet, column++, row, symbol.Quote);
            WriteCell(sheet, column++, row, symbol.Status);

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


            cell = WriteCell(sheet, column++, row, (double)symbol.QuoteValueMinimum);
            cell.CellStyle = CellStyleDecimalNormal;

            cell = WriteCell(sheet, column++, row, (double)symbol.QuoteValueMaximum);
            cell.CellStyle = CellStyleDecimalNormal;




            if (symbol.LastPrice.HasValue)
            {
                cell = WriteCell(sheet, column++, row, (double)symbol.LastPrice);
                cell.CellStyle = CellStyleDecimalNormal;
            }
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


    public void ExportToExcel(Model.CryptoExchange exchange)
    {
        Exchange = exchange;
        try
        {
            CreateBook(Exchange.Name);
            CreateFormats();

            DumpSymbols();
            DumpInformation();

            StartExcell("Symbols", Exchange.Name, Exchange.Name);

        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR exchange dump " + error.ToString());
        }
    }
}
