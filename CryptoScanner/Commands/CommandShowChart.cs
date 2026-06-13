using CryptoScanner.Core.Core;
using CryptoScanner.Views;

namespace CryptoScanner.Commands;

public class CommandShowChart : CommandBase
{
    public override void Execute(object? parameter)
    {
        System.Diagnostics.Debug.WriteLine($"CommandShowGraph");
        if (GetObjectInformation(parameter, out ParameterObjects dto) && dto.symbol != null && dto.parentWindow != null)
        {
            try
            {
                System.Diagnostics.Debug.WriteLine($"Opening {dto.symbol.Name} in internal program");

                // Shared launcher: holds the single reusable window and the already-open handling
                // (update symbol, restore if minimized, bring to front). Same path as the emulator.
                ChartWindowLauncher.Show(dto.symbol.Base, dto.symbol.Quote, dto.interval?.Name);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"error showing chart {dto.symbol.Name} {dto.interval?.Name} {error.Message}");
            }
        }
    }
}
