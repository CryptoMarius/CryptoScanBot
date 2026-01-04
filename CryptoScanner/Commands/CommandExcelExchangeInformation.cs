using CryptoScanner.Core.Core;
using CryptoScanner.Core.Excel;

namespace CryptoScanner.Commands;

public class CommandExcelExchangeInformation : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        _ = Task.Run(() => { new ExcelExchangeDump(GlobalData.ActiveExchange!).ExportToExcel(); });
    }
}
