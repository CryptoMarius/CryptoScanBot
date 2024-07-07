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

        int row = 0;
        WriteCell(sheet, 0, row, "Exchange");
        WriteCell(sheet, 1, row, exchange.Name);

        row++;
        WriteCell(sheet, 0, row, "Symbol count");
        WriteCell(sheet, 1, row, exchange.SymbolListName.Count);

        row += 2;
        WriteCell(sheet, 0, row, "Quote information");
        foreach (var quote in GlobalData.Settings.QuoteCoins.Values)
        {
            row++;
            WriteCell(sheet, 0, row, quote.Name);
            WriteCell(sheet, 1, row, quote.SymbolList.Count);
        }

        AutoSize(sheet, 6);
    }

    public void ExportToExcel()
    {
        try
        {
            DumpInformation();
            DumpSymbols();

            StartExcell("Symbols", exchange.Name);

        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR exchange dump " + error.ToString());
        }
    }
}