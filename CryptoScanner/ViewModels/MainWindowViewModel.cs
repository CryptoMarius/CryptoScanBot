using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Core.Const;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Services;
using CryptoScanner.Helpers;
using CryptoScanner.Services;
using CryptoScanner.Views;

namespace CryptoScanner.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public required ITradingViewService TradingViewService { get; set; }
    public required LogGridViewModel LogGridViewModel { get; set; }
    public required ApplicationStateService ApplicationStateService { get; set; }
    public required DashBoardInformationViewModel DashBoardInformationViewModel { get; set; }
    public required DashboardPositionsViewModel DashboardPositionsViewModel { get; set; }
    public required SymbolGridViewModel SymbolGridViewModel { get; set; }
    public required BrowserViewModel BrowserViewModel { get; set; }
    public required SignalGridViewModel SignalGridViewModel { get; set; }
    public required LiveDataGridViewModel LiveDataGridViewModel { get; set; }
    public required PositionOpenGridViewModel PositionOpenGridViewModel { get; set; }
    public required PositionClosedGridViewModel PositionClosedGridViewModel { get; set; }


    public BrowserView? BrowserView { get; set; }

    [ObservableProperty]
    private string _title  = Core.Const.Constants.AppName;
    

    //[ObservableProperty]
    //private bool _analyzerActive = false;
    public bool AnalyzerActive
    {
        get => GlobalData.Settings.Signal.Active;
        set
        {
            if (GlobalData.Settings.Signal.Active != value)
            {
                GlobalData.Settings.Signal.Active = value;
                OnPropertyChanged(nameof(AnalyzerActive));
            }
        }
    }

    //[ObservableProperty]
    //private bool _soundsActive = false;
    public bool SoundsActive
    {
        get => GlobalData.Settings.Signal.SoundsActive;
        set
        {
            if (GlobalData.Settings.Signal.SoundsActive != value)
            {
                GlobalData.Settings.Signal.SoundsActive = value;
                OnPropertyChanged(nameof(SoundsActive));
            }
        }
    }

    //[ObservableProperty]
    //private bool _traderActive = false;
    public bool TraderActive
    {
        get => GlobalData.Settings.Trading.Active;
        set
        {
            if (GlobalData.Settings.Trading.Active != value)
            {
                GlobalData.Settings.Trading.Active = value;
                OnPropertyChanged(nameof(TraderActive));
            }
        }
    }


    [ObservableProperty]
    private int _selectedTabIndex = 0;

    [ObservableProperty]
    private string _symbolFilterText = string.Empty;
    public event EventHandler<string>? FilterTextChanged;


    public MainWindowViewModel()
    {
        System.Diagnostics.Debug.WriteLine($"MainViewModel default constructor called");
    }

    public MainWindowViewModel(
        ITradingViewService tradingViewService,
        ApplicationStateService applicationStateService,
        DashBoardInformationViewModel dashBoardInformationViewModel,
        DashboardPositionsViewModel dashBoardPositionsViewModel,
        SymbolGridViewModel symbolGridViewModel,
        SignalGridViewModel signalGridViewModel,
        LiveDataGridViewModel liveDataGridViewModel,
        PositionOpenGridViewModel positionOpenGridViewModel,
        PositionClosedGridViewModel positionClosedGridViewModel,
        BrowserViewModel browserViewModel,
        LogGridViewModel logGridViewModel)
    {
        TradingViewService = tradingViewService;
        ApplicationStateService = applicationStateService;
        DashBoardInformationViewModel = dashBoardInformationViewModel;
        DashboardPositionsViewModel = dashBoardPositionsViewModel;
        SymbolGridViewModel = symbolGridViewModel;
        SignalGridViewModel = signalGridViewModel;
        LiveDataGridViewModel = liveDataGridViewModel;
        PositionOpenGridViewModel = positionOpenGridViewModel;
        PositionClosedGridViewModel = positionClosedGridViewModel;
        BrowserViewModel = browserViewModel;
        LogGridViewModel = logGridViewModel;

        // Subscribe child ViewModels to filter event
        FilterTextChanged += SymbolGridViewModel.OnFilterTextChanged;
        FilterTextChanged += SignalGridViewModel.OnFilterTextChanged;

        App.EventOpenInInternalBrowser += OnOpenInInternalBrowserRequested;

        // Initialize the visible browser to the BTCUSDT (if it exists) - TODO: Move code?
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval5m];
        if (GlobalData.ActiveExchange!.SymbolListName.TryGetValue("BTCUSDT", out CryptoSymbol? symbol))
            CommandHelper.ActivateTradingApp(CryptoTradingApp.TradingView, symbol, interval, CryptoExternalUrlType.Internal, false);
    }

    private void OnOpenInInternalBrowserRequested(string url, bool switchTab)
    {
        //BrowserViewModel.NavigateToTradingView(url);
        if (BrowserView != null)
        {
            System.Diagnostics.Debug.WriteLine($"OpenInBrowser: {url}");

            // switch to the browser tab
            SelectedTabIndex = 1;

            // Navigate triggers initialization + tab switch automatically
            BrowserView.Navigate(url);
        }
    }


    public async Task ExitApp()
    {
        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.TryShutdown();
        }
        else
        {
            // Fallback 
            Environment.Exit(0);
        }
    }


    [RelayCommand]
    private void ApplyFilter(string? filterText)
    {
        if (filterText != null)
        {
            SymbolFilterText = filterText;
            FilterTextChanged?.Invoke(this, filterText);
        }
    }
}