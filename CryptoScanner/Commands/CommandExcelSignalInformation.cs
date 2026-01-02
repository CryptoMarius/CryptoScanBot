using CryptoScanner.Core.Excel;

namespace CryptoScanner.Commands;

public class CommandExcelSignalInformation : CommandBase
{
    public override async void Execute(object? parameter)
    {
        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.symbol != null)
        {
            _ = Task.Run(() => { new ExcelSymbolDump(dto.symbol).ExportToExcel(); });
        }
    }
}
