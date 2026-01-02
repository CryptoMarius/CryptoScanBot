using CryptoScanner.Core.Excel;

namespace CryptoScanner.Commands;

public class CommandExcelSignalsInformation : CommandBase
{
    public override async void Execute(object? parameter)
    {
        _ = Task.Run(() => { new ExcelSignalsDump().ExportToExcel(); });
    }
}
