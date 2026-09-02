// list box vc data grid
// (maar vooral: vermijd converters zoveel mogelijk, meh..)
//https://chatgpt.com/share/6974f916-e54c-8012-aeae-4e9528d9705a
//https://chatgpt.com/share/6974f916-e54c-8012-aeae-4e9528d9705a
//https://chatgpt.com/share/6974f916-e54c-8012-aeae-4e9528d9705a

#pragma warning disable AVLN3001 // MainWindow uses DI constructor, no parameterless constructor needed

using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

using CryptoScanner.Commands;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Services;
using CryptoScanner.Core.Sounds;
using CryptoScanner.Services;
using CryptoScanner.ViewModels;

using System.ComponentModel;
using System.Runtime.InteropServices;

namespace CryptoScanner.Views;

public partial class MainWindow : Window
{
    private readonly ApplicationStateService _applicationStateService;
    private readonly ITradingViewService _tradingViewService;

    private double _symbolPanelStoredWidth = 300;
    private bool _isSymbolPanelCollapsed = false;

    private bool _isSplitterDragging = false;
    private double _splitterDragStartWindowX = 0;
    private double _splitterDragStartColWidth = 0;

    private readonly SignalGridView _signalView;
    private readonly LiveDataGridView _liveDataView;
    private readonly DashboardPositionsView _dashboardView;
    private readonly PositionOpenGridView _openPositionsView;
    private readonly PositionClosedGridView _closedPositionsView;
    private readonly LogGridView _logView;


    // Required by the Avalonia runtime loader (AVLN3001). Never called at runtime — DI always
    // uses the parameterized constructor, so the uninitialized fields are intentional here.
    public MainWindow()
    {
        _applicationStateService = null!;
        _tradingViewService = null!;
        InitializeComponent();

        _signalView = null!;
        _liveDataView = null!;
        _dashboardView = null!;
        _openPositionsView = null!;
        _closedPositionsView = null!;
        _logView = null!;

    }

    public MainWindow(MainWindowViewModel viewModel,
        ApplicationStateService applicationStateService,
        ITradingViewService tradingViewService)
    {
        _applicationStateService = applicationStateService;
        _tradingViewService = tradingViewService;
        InitializeComponent();

        // Does not add anything usefull i'm afraid (for monotoring performance)
        //#if DEBUG
        //        this.AttachDevTools(new DevToolsOptions
        //        {
        //            StartupScreenIndex = 1, // Start met Performance tab
        //            ShowAsChildWindow = true
        //        });
        //        Debug.WriteLine("DevTools attached");
        //#endif

        DataContext = viewModel;

        // Restore window position, size, state and splitter
        _applicationStateService.RestoreWindowState("MainWindow", this);
        _applicationStateService.TrackWindowState("MainWindow", this);

        // Restore splitter position
        _symbolPanelStoredWidth = _applicationStateService.GetSplitterPosition("MainWindow", 300);
        MainGrid.ColumnDefinitions[0].Width = new GridLength(_symbolPanelStoredWidth);

        // Restore collapsed state
        if (_applicationStateService.GetSymbolPanelCollapsed())
            CollapseSymbolPanel();

        // Start TradingView service
        _tradingViewService.Start();

        GlobalData.PlaySound += new PlayMediaEvent(PlaySound);

        // macOS: Shift menu to the right to avoid collision with the system buttons
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            if (TitleBarGrid != null)
                TitleBarGrid.Margin = new Thickness(80, 0, 0, 0);  // 80px rechts
        }

        // Through the view model, not through Window.Title: the XAML binds Title to the view model,
        // so a value assigned here loses out as soon as that binding produces one - and its default
        // is the bare application name. Whichever of the two ran last decided what the task manager
        // showed, which is why some instances did not mention their exchange at all.
        viewModel.Title = GlobalData.ApplicationTitle;
        CreateMenuItems();

        _signalView = new SignalGridView { DataContext = viewModel.SignalGridViewModel };
        _liveDataView = new LiveDataGridView { DataContext = viewModel.LiveDataGridViewModel };
        _dashboardView = new DashboardPositionsView { DataContext = viewModel.DashboardPositionsViewModel };
        _openPositionsView = new PositionOpenGridView { DataContext = viewModel.PositionOpenGridViewModel };
        _closedPositionsView = new PositionClosedGridView { DataContext = viewModel.PositionClosedGridViewModel };
        _logView = new LogGridView { DataContext = viewModel.LogGridViewModel };



        //Force initial tab content
        OnTabChanged(MainTabs, null!);
        MainTabs.SelectionChanged += OnTabChanged;

        // Save layout + splitter
        Closing += OnWindowClosing;

        // Initialize the visible browser to the BTCUSDT (if it exists) (TODO: Other Quote perhaps)
        // Implementation moved to Core so the Blazor hosts do the same on startup.
        CryptoScanner.Core.Helpers.ExternalLinkHelper.ActivateStartupSymbol(applicationStateService.BarometerQuote);
    }


    /// <summary>
    /// Handle title bar drag to move window
    /// </summary>
    private void OnTitleBarPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        BeginMoveDrag(e);
    }

    private void OnSplitterPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_isSymbolPanelCollapsed) return;
        if (!e.GetCurrentPoint(SplitterHandle).Properties.IsLeftButtonPressed) return;

        _isSplitterDragging = true;
        _splitterDragStartWindowX = e.GetPosition(this).X;
        _splitterDragStartColWidth = MainGrid.ColumnDefinitions[0].ActualWidth;
        e.Pointer.Capture(SplitterHandle);

        // Show a thin vertical preview line — no layout update yet
        DragPreviewLine.Height = Bounds.Height;
        Canvas.SetLeft(DragPreviewLine, _splitterDragStartWindowX);
        DragPreviewCanvas.IsVisible = true;
        e.Handled = true;
    }

    private void OnSplitterPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_isSplitterDragging) return;

        var mouseX = e.GetPosition(this).X;
        var newWidth = _splitterDragStartColWidth + (mouseX - _splitterDragStartWindowX);
        newWidth = Math.Max(100, newWidth);

        // Move only the preview line — zero layout recalculation
        Canvas.SetLeft(DragPreviewLine, mouseX);
        e.Handled = true;
    }

    private void OnSplitterPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (!_isSplitterDragging) return;

        _isSplitterDragging = false;
        e.Pointer.Capture(null);
        DragPreviewCanvas.IsVisible = false;

        // Single layout update on release
        var mouseX = e.GetPosition(this).X;
        var newWidth = _splitterDragStartColWidth + (mouseX - _splitterDragStartWindowX);
        newWidth = Math.Max(100, newWidth);

        MainGrid.ColumnDefinitions[0].Width = new GridLength(newWidth);
        _symbolPanelStoredWidth = newWidth;
        _applicationStateService.SaveSplitterPosition("MainWindow", newWidth);
        e.Handled = true;
    }

    private void OnToggleSymbolPanel(object? sender, RoutedEventArgs e)
    {
        if (_isSymbolPanelCollapsed)
            ExpandSymbolPanel();
        else
            CollapseSymbolPanel();
    }

    private void CollapseSymbolPanel()
    {
        var currentWidth = MainGrid.ColumnDefinitions[0].ActualWidth;
        if (currentWidth > 28)
            _symbolPanelStoredWidth = currentWidth;

        MainGrid.ColumnDefinitions[0].MinWidth = 28;
        MainGrid.ColumnDefinitions[0].Width = new GridLength(28);
        MainGrid.ColumnDefinitions[1].Width = new GridLength(0); // hide splitter

        SymbolGridContent.IsVisible = false;
        SymbolsLabel.IsVisible = false;
        SymbolsFilterLabel.IsVisible = false;
        FilterTextBox.IsVisible = false;
        TogglePanelButton.Content = "►";
        TogglePanelButton.SetValue(ToolTip.TipProperty, "Toon symbolen");

        _isSymbolPanelCollapsed = true;
        _applicationStateService.SaveSymbolPanelCollapsed(true);
    }

    private void ExpandSymbolPanel()
    {
        MainGrid.ColumnDefinitions[0].MinWidth = 100;
        MainGrid.ColumnDefinitions[0].Width = new GridLength(_symbolPanelStoredWidth);
        MainGrid.ColumnDefinitions[1].Width = new GridLength(4); // restore splitter

        SymbolGridContent.IsVisible = true;
        SymbolsLabel.IsVisible = true;
        SymbolsFilterLabel.IsVisible = true;
        FilterTextBox.IsVisible = true;
        TogglePanelButton.Content = "◄";
        TogglePanelButton.SetValue(ToolTip.TipProperty, "Hide symbols");

        _isSymbolPanelCollapsed = false;
        _applicationStateService.SaveSymbolPanelCollapsed(false);
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

        // Always save the expanded width, not the collapsed 28px strip
        if (!_isSymbolPanelCollapsed)
            _symbolPanelStoredWidth = MainGrid.ColumnDefinitions[0].ActualWidth;
        _applicationStateService.SaveSplitterPosition("MainWindow", _symbolPanelStoredWidth);

        // Save window state
        _applicationStateService.SaveWindowState("MainWindow", this);

        if (DataContext is MainWindowViewModel vm)
        {
            await vm.ExitApp();
        }
    }


    private static void PlaySound(string text, bool test)
    {
        ThreadSoundPlayer.AddToQueue(text, test);
    }

    private void CreateMenuItems()
    {
        //if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        //{
        //    // macOS: Gebruik native title bar en menu bar
        //    //ExtendClientAreaToDecorationsHint = false;
        //    //ExtendClientAreaChromeHints = "NoChrome"; // Of "SystemChrome" als je native wilt houden
        //    //ExtendClientAreaTitleBarHeightHint = -1; // Reset

        //    var titleBarGrid = this.FindControl<Grid>("TitleBarGrid");
        //    //?? throw new InvalidOperationException("TitleBarGrid not found");

        //    // Verberg custom title bar (geen overlap met native buttons)
        //    if (titleBarGrid != null)
        //        titleBarGrid.IsVisible = false;

        //    // Optioneel: Verberg app icon (niet nodig op Mac)
        //    var appIcon = this.FindControl<Image>("AppIcon"); // Optioneel, voor verbergen op Mac
        //    if (appIcon != null)
        //        appIcon.IsVisible = false;

        //    //// Cre�er native menu bar
        //    //var nativeMenu = new NativeMenu(); // Gebruik NativeMenu voor macOS integration
        //    //var fileMenuItem = new NativeMenuItem { Header = "_Filex" };
        //    //var helpMenuItem = new NativeMenuItem { Header = "_Helpx" };

        //    //fileMenuItem.Menu = new NativeMenu();
        //    //fileMenuItem.Menu.Items.Add(new NativeMenuItem { Header = "Play soundxx" });
        //    //fileMenuItem.Menu.Items.Add(new NativeMenuItem { Header = "Create signalsxx" });
        //    //fileMenuItem.Menu.Items.Add(new NativeMenuItem { Header = "Trading bot activexx" });
        //}


        //Type menuItem = NativeMenuItem;

        //MenuItem menuItem;
        //var MenuFile = this.FindControl<MenuItem>("MenuFile");
        //if (MenuFile != null)
        // There are already three items bound to the application states (sound, trading and analyzer)
        MenuFile.Items.Add(new MenuItem { Header = "-" });
        MenuFile.Items.Add(new MenuItem { Header = "Scanner configuration", Command = new CommandShowConfiguration(), CommandParameter = this });
        MenuFile.Items.Add(new MenuItem { Header = "Refresh information", Command = new CommandRefreshInformation(), CommandParameter = this });
        MenuFile.Items.Add(new MenuItem { Header = "Clear log and tickers", Command = new CommandClearLogAndTicker(), CommandParameter = this });
        MenuFile.Items.Add(new MenuItem { Header = "-" });
        MenuFile.Items.Add(new MenuItem { Header = "E_xit", Command = new CommunityToolkit.Mvvm.Input.RelayCommand<Window>(w => w?.Close()), CommandParameter = this });


        //var MenuTools = this.FindControl<MenuItem>("MenuTools");
        //if (MenuTools != null)
        //MenuTools.Items.Add(new MenuItem { Header = "Export Tradingview import files", Command.TradingViewImportList);
        MenuTools.Items.Add(new MenuItem { Header = "Export all exchange information to Excel", Command = new CommandExcelExchangeInformation(), CommandParameter = this });
        MenuTools.Items.Add(new MenuItem { Header = "Export all signal information to Excel", Command = new CommandExcelSignalsInformation(), CommandParameter = this });
        MenuTools.Items.Add(new MenuItem { Header = "Export all position information to Excel", Command = new CommandExcelPositionsInformation(), CommandParameter = this });
        MenuTools.Items.Add(new MenuItem { Header = "Dump memory info", Command = new CommandShowMemoryObjects(), CommandParameter = this });
        MenuTools.Items.Add(new MenuItem { Header = "Paper assets", Command = new CommandShowAssets(), CommandParameter = this });

        MenuTools.Items.Add(new MenuItem { Header = "-" });
#if DEBUG
        //MenuTools.Items.Add(new MenuItem { Header = "Test - Save Candles", Command.None, TestSaveCandlesClick);
        //MenuTools.Items.Add(new MenuItem { Header = "Test - Create url testfile", Command.None, TestCreateUrlTestFileClick);
        //MenuTools.Items.Add(new MenuItem { Header = "Test - Dump ticker information", Command.None, TestShowTickerInformationClick);
        MenuTools.Items.Add(new MenuItem { Header = "Cleanup candles and old files in data folder", Command = new CommandCleanOrphanCandleFiles(), CommandParameter = this });
#endif
        //MenuTools.Items.Add(new MenuItem { Header = "Scanner internal restart", Command.ScannerSessionDebug);
        MenuTools.Items.Add(new MenuItem { Header = "Calculate all liquidity zones (slow!)", Command = new CommandCalculateDlzForAll(), CommandParameter = this });
        MenuTools.Items.Add(new MenuItem { Header = "-" });



        MenuTools.Items.Add(new MenuItem { Header = "Open data folder", Command = new CommandOpenDataFolder(), CommandParameter = this });


        //var MenuHelp = this.FindControl<MenuItem>("MenuHelp");
        //if (MenuHelp != null)
        MenuHelp.Items.Add(new MenuItem { Header = "Wiki", Command = new CommandOpenWiki(), CommandParameter = this });
        MenuHelp.Items.Add(new MenuItem { Header = "-" });
        MenuHelp.Items.Add(new MenuItem { Header = "About...", Command = new CommandShowAbout(), CommandParameter = this });

    }

    private void ManipulateBrowser(bool show)
    {
        if (show)
        {
            BrowserViewHost.Width = double.NaN;   // Auto
            BrowserViewHost.Height = double.NaN;  // Auto
            BrowserViewHost.Opacity = 1;
            BrowserViewHost.IsHitTestVisible = true;
            return;
        }
        else
        {
            BrowserViewHost.Width = 1;
            BrowserViewHost.Height = 1;
            BrowserViewHost.Opacity = 0.01;
            BrowserViewHost.IsHitTestVisible = false;
        }
    }

    private void OnTabChanged(object? sender, SelectionChangedEventArgs e)
    {
        // Workaround, because the browser needs to be visible to work properly

        var selected = MainTabs.SelectedItem as TabItem;
        var header = selected?.Header?.ToString();
        switch (header)
        {
            case "Signals":
                ManipulateBrowser(false);
                MainContent.Content = _signalView;
                break;

            case "Tradingview":
                ManipulateBrowser(true);
                MainContent.Content = null;
                break;

            case "Live Data":
                ManipulateBrowser(false);
                MainContent.Content = _liveDataView;
                break;

            case "Dashboard":
                ManipulateBrowser(false);
                MainContent.Content = _dashboardView;
                break;

            case "Open positions":
                ManipulateBrowser(false);
                MainContent.Content = _openPositionsView;
                break;

            case "Closed positions":
                ManipulateBrowser(false);
                MainContent.Content = _closedPositionsView;
                break;

            case "Log":
                ManipulateBrowser(false);
                MainContent.Content = _logView;
                break;

        }

    }

}