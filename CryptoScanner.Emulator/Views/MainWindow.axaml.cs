using Avalonia.Controls;

using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
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
        ApplyExchangeIcon();
    }

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
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
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
}
