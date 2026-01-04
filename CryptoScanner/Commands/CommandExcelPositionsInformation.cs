using CryptoScanner.Core.Excel;

namespace CryptoScanner.Commands;

public class CommandExcelPositionsInformation : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        _ = Task.Run(() => { new ExcelPostionsDump().ExportToExcel(); });
    }
}
