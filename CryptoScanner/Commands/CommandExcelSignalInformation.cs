using CryptoScanner.Core.Excel;

namespace CryptoScanner.Commands;

public class CommandExcelSignalInformation : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.signal != null)
        {
            _ = Task.Run(() => { new ExcelSignalDump(dto.signal).ExportToExcel(); });
        }
    }
}
