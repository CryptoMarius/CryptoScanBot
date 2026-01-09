using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Commands;
using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.Sounds;
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
        Title = $"{CryptoScanner.Core.Const.Constants.AppName} {GlobalData.AppVersion} {GlobalData.Settings.General.ExchangeName} {GlobalData.Settings.General.ExtraCaption}".Trim();

        Closing += OnWindowClosing; // Save state
        GlobalData.PlaySound += new PlayMediaEvent(PlaySound);


        //MenuItem menuItem;
        var menuFile = this.FindControl<MenuItem>("MenuFile");
        if (menuFile != null)
        {
            // There are already three items bound to the application states (sound, trading and analyzer)
            menuFile.Items.Add(new MenuItem { Header = "-" });
            menuFile.Items.Add(new MenuItem { Header = "Settings", Command = new CommandSettings(), CommandParameter = this });
            menuFile.Items.Add(new MenuItem { Header = "Refresh information", Command = new CommandRefreshInformation(), CommandParameter = this });
            menuFile.Items.Add(new MenuItem { Header = "Clear log and ticker count", Command = new CommandClearLogAndTicker(), CommandParameter = this });
            //menuFile.Items.Add(new MenuItem { Header = "-" });



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

            menuFile.Items.Add(new MenuItem { Header = "E_xit", Command = new RelayCommand<Window>(w => w?.Close()), CommandParameter = this });
        }


        var menuHelp = this.FindControl<MenuItem>("MenuHelp");
        if (menuHelp != null)
        {
            menuHelp.Items.Add(new MenuItem { Header = "About...", Command = new CommandShowAbout(), CommandParameter = this });
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

    private bool hasEnded = false;
    private async void OnWindowClosing(object? sender, CancelEventArgs e)
    {
        if (hasEnded)
            return;

        // Twee possible calls, 1 via windows - close button or via menu->exit
        hasEnded = true;
        Closing -= OnWindowClosing; // observed multiple calls..
        //e.Cancel = true;  // Blokkeer standaard close tot cleanup klaar

        // Save splitter position
        var position = _mainGrid.ColumnDefinitions[0].ActualWidth;
        _applicationStateService.SaveSplitterPosition("MainWindow", position);

        // Save window state
        _applicationStateService.SaveWindowState("MainWindow", this);

        if (DataContext is MainWindowViewModel vm)
        {
            await vm.ExitApp();
        }
    }


    private void OnFilterApply(object? sender, RoutedEventArgs e)
    {
        if (sender is TextBox textBox && DataContext is MainWindowViewModel vm)
        {
            vm.SymbolFilterText = textBox.Text;
        }
    }

    private static void PlaySound(string text, bool test)
    {
        ThreadSoundPlayer.AddToQueue(text, test);
    }

}

