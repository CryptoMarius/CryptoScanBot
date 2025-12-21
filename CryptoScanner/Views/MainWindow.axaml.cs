using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using CryptoScanner.Browser.Views;
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
    private BrowserView? _browserView;

    public MainWindow(MainWindowViewModel viewModel, ITradingViewService tradingViewService)
    {
        _tradingViewService = tradingViewService;

        InitializeComponent();

        _mainGrid = this.FindControl<Grid>("MainGrid")
            ?? throw new InvalidOperationException("MainGrid not found");
        Closing += Window_Closing; // - save layout + splitter

        _browserView = this.FindControl<BrowserView>("BrowserView")
            ?? throw new InvalidOperationException("BrowserView not found");

        DataContext = viewModel;

        if (DataContext is MainWindowViewModel vm)
        {
            vm.BrowserView = _browserView;
        }

        // Restore window position, size, state and splitter
        App.GridStateService.RestoreWindowState("MainWindow", this);

        // Restore splitter position
        var position = App.GridStateService.GetSplitterPosition("MainWindow", 300);
        _mainGrid.ColumnDefinitions[0].Width = new GridLength(position);

        // Start TradingView service
        _tradingViewService.Start();

        // Set application title (we have multiple instances)
        Title = $"{GlobalData.AppName} {GlobalData.AppVersion} {GlobalData.Settings.General.ExchangeName} {GlobalData.Settings.General.ExtraCaption}".Trim();
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