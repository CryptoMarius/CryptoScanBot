using CryptoScanner.Core.Excel;

namespace CryptoScanner.Commands;

public class CommandExcelSymbolInformation : CommandBase
{
    public override async void Execute(object? parameter)
    {
        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.signal != null)
        {
            _ = Task.Run(() => { new ExcelSignalDump(dto.signal).ExportToExcel(); });
        }
    }
}
