using Avalonia.Controls;

using CryptoScanner.Chart.Services;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;
using CryptoScanner.Emulator.ViewModels;

using Dapper;

namespace CryptoScanner.Emulator.Views;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainWindowViewModel();
        Closing += OnClosing;
        Closed += OnClosed;
        ApplyExchangeIcon();

        // Restore window position, size, state and monitor, the same way the scanner does. A name of
        // its own, so it never mixes with the scanner's "MainWindow" entry.
        // Note 31-10-2026: stored in CryptoScanBot-window.json of IPlatformService.GetDataDirectory()
        // (the -f folder, or the scanner's standard folder), not in the emulator's chosen data folder.
        StateService?.RestoreWindowState(WindowStateName, this);
        StateService?.TrackWindowState(WindowStateName, this);
    }

    private const string WindowStateName = "EmulatorMainWindow";

    private static ApplicationStateService? StateService => GlobalData.GetService<ApplicationStateService>();

    /// <summary>
    /// The purple emulator icon with the logo of the exchange chosen in the setup dialog. That is
    /// the exchange active now: the bootstrap runs before this window is created, and a different
    /// choice later creates a new main window. A run on another exchange switches the active
    /// exchange only for its duration, so the icon deliberately stays with the setup choice.
    /// </summary>
    private void ApplyExchangeIcon()
    {
        try
        {
            Icon = new WindowIcon(new MemoryStream(AppIconBuilder.BuildCurrent()));
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "ApplyExchangeIcon");
        }

        // The Dock on macOS shows the application, not the window
        AppIconBuilder.ApplyApplicationIcon();
    }

    // Set once the WAL checkpoint of this window's database is done, see CheckpointBeforeDatabaseSwitch
    private bool _checkpointDone;


    /// <summary>
    /// Runs the shutdown checkpoint of this window's database while GlobalData.AppDataFolder still
    /// points at it. App.SwitchDatabaseAsync calls this before it switches the folder: the old window
    /// is closed only after the new one is shown, and its OnClosing then checkpointed the NEW database
    /// (31-10-2026).
    /// </summary>
    internal void CheckpointBeforeDatabaseSwitch()
    {
        CheckpointDatabase();
        _checkpointDone = true;
    }


    /// <summary>
    /// The window is gone: stop what its view models keep running in the background. The live refresh
    /// of the Results tab ticked on every 20 seconds and the Log tab stayed on the static log event,
    /// both after "Change database" replaced this window (31-10-2026).
    /// </summary>
    private void OnClosed(object? sender, EventArgs e)
    {
        if (DataContext is MainWindowViewModel viewModel)
        {
            viewModel.RunResults.StopLiveRefresh();
            viewModel.LogTab.Detach();
        }
    }


    private void CheckpointDatabase()
    {
        try
        {
            using var database = new CryptoDatabase();
            database.Open();
            database.Connection.Execute("PRAGMA wal_checkpoint(TRUNCATE);");
            GlobalData.AddTextToLogTab("WAL checkpoint completed on shutdown");
        }
        catch
        {
            // Non-fatal: WAL will be recovered on next open
        }
    }


    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        StateService?.SaveWindowState(WindowStateName, this);

        // Already done against this window's own database before a "Change database" switched the
        // folder; doing it here would hit the new database instead (31-10-2026)
        if (_checkpointDone)
            return;

        try
        {
            using var database = new CryptoDatabase();
            database.Open();
            database.Connection.Execute("PRAGMA wal_checkpoint(TRUNCATE);");
            GlobalData.AddTextToLogTab("WAL checkpoint completed on shutdown");
        }
        catch
        {
            // Non-fatal: WAL will be recovered on next open
        }
    }
}
