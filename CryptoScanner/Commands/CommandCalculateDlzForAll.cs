using CryptoScanner.Core.Core;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.Commands;

public class CommandCalculateDlzForAll : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Fire-and-forget
        _ = ExecuteAsync(parameter);
    }

    public async Task ExecuteAsync(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"Calculate dlz for all symbols");
        try
        {
            ZoneThreadCalculate.CalculateZonesForAllSymbolsAsync();
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"error calculating dlz {error.Message}");
        }
    }
}
