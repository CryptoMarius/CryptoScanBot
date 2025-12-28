using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using CryptoScanner.Browser.Views;
using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;
using CryptoScanner.DashBoard.Services;
using CryptoScanner.Services;
using CryptoScanner.ViewModels;

using System.ComponentModel;

namespace CryptoScanner.Views;

public partial class MainWindow : Window
{
    private readonly ApplicationStateService _applicationStateService;
    private readonly ITradingViewService _tradingViewService;
    private readonly Grid _mainGrid = null!;
    private readonly BrowserView? _browserView;


    public MainWindow(MainWindowViewModel viewModel,
        ApplicationStateService applicationStateService,
        ITradingViewService tradingViewService)
    {
        _applicationStateService = applicationStateService;
        _tradingViewService = tradingViewService;

        AvaloniaXamlLoader.Load(this);

        _mainGrid = this.FindControl<Grid>("MainGrid")
            ?? throw new InvalidOperationException("MainGrid not found");
        Closing += OnWindowClosing; // - save layout + splitter

        _browserView = this.FindControl<BrowserView>("BrowserView")
            ?? throw new InvalidOperationException("BrowserView not found");

        DataContext = viewModel;

        if (DataContext is MainWindowViewModel vm)
        {
            vm.BrowserView = _browserView;
            vm.CloseRequested += (s, e) => Close();
            vm.DialogService = new DialogService(this);
        }

        // Restore window position, size, state and splitter
        _applicationStateService.RestoreWindowState("MainWindow", this);

        // Restore splitter position
        var position = _applicationStateService.GetSplitterPosition("MainWindow", 300);
        _mainGrid.ColumnDefinitions[0].Width = new GridLength(position);

        // Start TradingView service
        _tradingViewService.Start();

        // TODO: place somewhere else..
        // Set application title (we have multiple instances)
        Title = $"{Constants.AppName} {GlobalData.AppVersion} {GlobalData.Settings.General.ExchangeName} {GlobalData.Settings.General.ExtraCaption}".Trim();
    }

    /// <summary>
    /// Handle title bar drag to move window
    /// </summary>
    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void OnGridSplitterDragCompleted(object? sender, VectorEventArgs e)
    {
        // Save splitter position
        var position = _mainGrid.ColumnDefinitions[0].ActualWidth;
        _applicationStateService.SaveSplitterPosition("MainWindow", position);
    }

    private void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        // Save splitter position
        var position = _mainGrid.ColumnDefinitions[0].ActualWidth;
        _applicationStateService.SaveSplitterPosition("MainWindow", position);

        // Save window state
        _applicationStateService.SaveWindowState("MainWindow", this);
    }

}