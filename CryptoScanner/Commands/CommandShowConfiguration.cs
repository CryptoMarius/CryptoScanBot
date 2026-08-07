using Avalonia.Controls;

using CryptoScanner.Config.Views;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;

namespace CryptoScanner.Commands;

public class CommandShowConfiguration : CommandBase
{
    public override async void Execute(object? parameter)
    {
        if (parameter is not Window parentWindow)
            return;

        System.Diagnostics.Debug.WriteLine($"Settings");
        var scannerSession = GlobalData.GetService<IScannerSession>()
            ?? throw new InvalidOperationException("ScannerSession service not found");

        // Save some old stuff for reloading stuff
        var previous = ConfigurationApplier.TakeSnapshot();

        try
        {
            var dialog = new ConfigurationWindow
            {
                CanResize = true,
                Title = "Scanner configuration",
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            // Hacky, needs work...
            var result = await dialog.ShowDialog<bool?>(parentWindow!);
            if (result != true)
                return;

            // Save + re-apply. The implementation moved to Core so the Blazor hosts run exactly
            // the same sequence (they only saved the file before, which left SignalExecute and
            // the plugin settings stale until a restart).
            await ConfigurationApplier.SaveAndApplyAsync(scannerSession, previous);
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("ERROR settings " + error.ToString());
        }
    }
}
