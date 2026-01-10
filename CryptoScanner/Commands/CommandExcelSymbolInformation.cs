using CryptoScanner.Core.Excel;

namespace CryptoScanner.Commands;

public class CommandExcelSymbolInformation : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        if (GetObjectInformation(parameter, out parameterObjects dto) && dto.symbol != null)
        {
            _ = Task.Run(() => { new ExcelSymbolDump(dto.symbol).ExportToExcel(); });
        }
    }
}
