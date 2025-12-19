using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;

using CryptoScanner.Core.Core;
using CryptoScanner.DashBoard.Services;
using CryptoScanner.ViewModels;

using System.ComponentModel;
using System.Reflection;

namespace CryptoScanner.Views;

public partial class MainWindow : Window
{
    private readonly ITradingViewService _tradingViewService;
    private readonly Grid _mainGrid = null!;

    public MainWindow(MainWindowViewModel viewModel, ITradingViewService tradingViewService)
    {
        _tradingViewService = tradingViewService;

        // In code - behind of ViewModel
        //var assets = AssetLoader.Open(new Uri("avares://CryptoScanner/Assets/app-icon.ico"));
        //window.Icon = new WindowIcon(assets);

        InitializeComponent();

        _mainGrid = this.FindControl<Grid>("MainGrid")
            ?? throw new InvalidOperationException("MainGrid not found");
        Closing += Window_Closing; // - save layout + splitter

        DataContext = viewModel;

        // Restore window position, size, state and splitter
        App.GridStateService.RestoreWindowState("MainWindow", this);

        // Restore splitter position
        var position = App.GridStateService.GetSplitterPosition("MainWindow", 300);
        _mainGrid.ColumnDefinitions[0].Width = new GridLength(position);

        // Start TradingView service
        _tradingViewService.Start();
    }


    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public static void InitAppVariables()
    {
        GlobalData.AppName = "CryptoScanBot";
        GlobalData.AppPath = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;

        var assembly = Assembly.GetExecutingAssembly().GetName();
        string appVersion = assembly.Version!.ToString();
        while (appVersion.EndsWith(".0.0"))
            appVersion = appVersion[0..^2];

        GlobalData.AppVersion = appVersion;
    }

    private void ExitMenuItem_Click(object? sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnSettingsClick(object? sender, RoutedEventArgs e)
    {
        // Placeholder: Open instellingen-window of dialog later
    }

    private void GridSplitter_DragCompleted(object? sender, Avalonia.Input.VectorEventArgs e)
    {
        // Save splitter position
        var position = _mainGrid.ColumnDefinitions[0].ActualWidth;
        App.GridStateService.SaveSplitterPosition("MainWindow", position);
    }

    private void Window_Closing(object? sender, CancelEventArgs e)
    {
        // Save splitter position
        var position = _mainGrid.ColumnDefinitions[0].ActualWidth;
        App.GridStateService.SaveSplitterPosition("MainWindow", position);

        // Save window state
        App.GridStateService.SaveWindowState("MainWindow", this);
    }
}