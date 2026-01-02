using CryptoScanner.Core.Excel;

namespace CryptoScanner.Commands;

public class CommandExcelPositionsInformation : CommandBase
{
    public override async void Execute(object? parameter)
    {
        _ = Task.Run(() => { new ExcelPostionsDump().ExportToExcel(); });
    }
}
