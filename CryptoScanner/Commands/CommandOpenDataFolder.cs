using CryptoScanner.Core.Core;

namespace CryptoScanner.Commands;

public class CommandOpenDataFolder : CommandBase
{
    public override void Execute(object? parameter)
    {
        // Open via the external (system) browser
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(GlobalData.AppDataFolder) { UseShellExecute = true });
    }
}
