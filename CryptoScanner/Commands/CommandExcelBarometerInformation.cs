using CryptoScanner.Core.Excel;

namespace CryptoScanner.Commands;

public class CommandExcelBarometerInformation : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null)
        {
            _ = Task.Run(() => { new ExcelBarometerDump(dto.symbol).ExportToExcel(); });
        }
    }
}
