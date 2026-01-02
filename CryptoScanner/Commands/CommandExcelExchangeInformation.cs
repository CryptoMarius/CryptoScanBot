using CryptoScanner.Core.Core;
using CryptoScanner.Core.Excel;

namespace CryptoScanner.Commands;

public class CommandExcelExchangeInformation : CommandBase
{
    public override async void Execute(object? parameter)
    {
        _ = Task.Run(() => { new ExcelExchangeDump(GlobalData.ActiveExchange!).ExportToExcel(); });
    }
}
