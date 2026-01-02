using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Commands;
using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;
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



        //MenuItem menuItem;
        var menuFile = this.FindControl<MenuItem>("MenuFile");
        if (menuFile != null)
        {
            // There are already three items bound to the application states (sound, trading and analyzer)
            menuFile.Items.Add(new MenuItem { Header = "-" });
            menuFile.Items.Add(new MenuItem { Header = "Settings", Command = new CommandShowAbout(), CommandParameter = this });
            menuFile.Items.Add(new MenuItem { Header = "Refresh information", Command = new CommandRefreshInformation(), CommandParameter = this });
            menuFile.Items.Add(new MenuItem { Header = "Clear log and ticker count", Command = new CommandClearLogAndTicker(), CommandParameter = this });
            menuFile.Items.Add(new MenuItem { Header = "-" });


            //menuItem = new MenuItem { Header = "E_xit", CommandParameter = this };
            //var binding = new Binding("CloseCommand") { Mode = BindingMode.OneWay, Source = menuItem.DataContext };
            //menuItem.Bind(MenuItem.CommandProperty, binding);
            //menuFile.Items.Add(menuItem);

            menuFile.Items.Add(new MenuItem { Header = "-" });
            //menuFile.Items.Add(new MenuItem { Header = "Export Tradingview import files", Command.TradingViewImportList);
            menuFile.Items.Add(new MenuItem { Header = "Export all exchange information to Excel", Command = new CommandExcelExchangeInformation(), CommandParameter = this });
            menuFile.Items.Add(new MenuItem { Header = "Export all signal information to Excel", Command = new CommandExcelSignalsInformation(), CommandParameter = this });
            menuFile.Items.Add(new MenuItem { Header = "Export all position information to Excel", Command = new CommandExcelPositionsInformation(), CommandParameter = this });

            menuFile.Items.Add(new MenuItem { Header = "-" });
#if DEBUG
            //menuFile.Items.Add(new MenuItem { Header = "Test - Save Candles", Command.None, TestSaveCandlesClick);
            //menuFile.Items.Add(new MenuItem { Header = "Test - Create url testfile", Command.None, TestCreateUrlTestFileClick);
            //menuFile.Items.Add(new MenuItem { Header = "Test - Dump ticker information", Command.None, TestShowTickerInformationClick);
#endif
            //menuFile.Items.Add(new MenuItem { Header = "Scanner internal restart", Command.ScannerSessionDebug);
            menuFile.Items.Add(new MenuItem { Header = "Calculate all liquidity zones (slow!)", Command = new CommandCalculateDlzForAll(), CommandParameter = this });
            menuFile.Items.Add(new MenuItem { Header = "-" });

            menuFile.Items.Add(new MenuItem { Header = "E_xit", Command = new RelayCommand<Window>(window => window?.Close(true)), CommandParameter = this});
        }


        var menuHelp = this.FindControl<MenuItem>("MenuHelp");
        if (menuHelp != null)
        {
            menuHelp.Items.Add(new MenuItem { Header = "About...", Command = new CommandShowAbout(), CommandParameter = this});
        }
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