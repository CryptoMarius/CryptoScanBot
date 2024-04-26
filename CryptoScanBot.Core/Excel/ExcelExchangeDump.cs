using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using NPOI.SS.UserModel;

namespace CryptoScanBot.Core.Excel;

public class ExcelExchangeDump(Model.CryptoExchange exchange) : ExcelBase(exchange.Name)
{
    private static int OHLCHeaders(ISheet sheet, int row)
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
        ISheet sheet = Book.CreateSheet(exchange.Name);

        int row = 0;
        int column;
        int columns = OHLCHeaders(sheet, 0);

        foreach (CryptoSymbol symbol in exchange.SymbolListName.Values.ToList())
        {
            if (symbol.IsBarometerSymbol())
                continue;

            row++;
            column = 0;

            WriteCell(sheet, column++, row, symbol.Id);
            WriteCell(sheet, column++, row, symbol.Name);
            WriteCell(sheet, column++, row, symbol.Base);
            WriteCell(sheet, column++, row, symbol.Quote);
            WriteCell(sheet, column++, row, symbol.Status);
            WriteCell(sheet, column++, row, symbol.Volume, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, symbol.PriceTickSize, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, symbol.PriceMinimum, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, symbol.PriceMaximum, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, symbol.PriceDisplayFormat);
            WriteCell(sheet, column++, row, symbol.QuantityTickSize, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, symbol.QuantityMinimum, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, symbol.QuantityMaximum, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, symbol.QuantityDisplayFormat);
            WriteCell(sheet, column++, row, symbol.QuoteValueMinimum, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, symbol.QuoteValueMaximum, CellStyleDecimalNormal);
            WriteCell(sheet, column++, row, symbol.LastPrice, CellStyleDecimalNormal);
        }

        AutoSize(sheet, columns);
    }

    private void DumpInformation()
    {
        ISheet sheet = Book.CreateSheet("Information");

        WriteCell(sheet, 0, 1, "Exchange");
        WriteCell(sheet, 0, 2, "Aantal symbols");

        WriteCell(sheet, 1, 1, exchange.Name);
        WriteCell(sheet, 1, 2, exchange.SymbolListName.Count);

        AutoSize(sheet, 6);
    }

    public void ExportToExcel()
    {
        try
        {
            DumpSymbols();
            DumpInformation();

            StartExcell("Symbols", exchange.Name, exchange.Name);

        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR exchange dump " + error.ToString());
        }
    }
}