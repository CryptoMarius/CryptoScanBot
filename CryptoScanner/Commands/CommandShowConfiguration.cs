using Avalonia.Controls;

using CryptoScanner.Config.Views;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;
using CryptoScanner.Services;

namespace CryptoScanner.Commands;

public class CommandShowConfiguration : CommandBase
{
    /// <summary>
    /// Where the position and size of the configuration window are stored. Not the same entry as
    /// the Photino/Blazor settings dialog uses: that one lives inside the browser view and stores
    /// coordinates within it, this one stores desktop coordinates.
    /// </summary>
    private const string WindowStateName = "SettingsWindow";

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

            // Reopen the window where it was left, the way the main window and the chart window do.
            // CenterOwner would overrule the restored position, so that only applies the first time.
            var stateService = GlobalData.GetService<ApplicationStateService>();
            if (stateService != null)
            {
                if (!string.IsNullOrEmpty(stateService.GetOrCreateWindowState(WindowStateName).State))
                    dialog.WindowStartupLocation = WindowStartupLocation.Manual;
                stateService.RestoreWindowState(WindowStateName, dialog);
                dialog.Closing += (_, _) => stateService.SaveWindowState(WindowStateName, dialog);
            }

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
