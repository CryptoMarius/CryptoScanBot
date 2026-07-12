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
