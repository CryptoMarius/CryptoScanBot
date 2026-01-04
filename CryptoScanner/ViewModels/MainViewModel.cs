using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Services;
using CryptoScanner.Views;

namespace CryptoScanner.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public required ITradingViewService TradingViewService { get; set; }
    public required ApplicationStateService ApplicationStateService { get; set; }
    public required DashBoardInformationViewModel DashBoardInformationViewModel { get; set; }
    public required DashboardPositionsViewModel DashboardPositionsViewModel { get; set; }
    public required SymbolGridViewModel SymbolGridViewModel { get; set; }
    public required BrowserViewModel BrowserViewModel { get; set; }
    public required SignalGridViewModel SignalGridViewModel { get; set; }
    public required LiveDataGridViewModel LiveDataGridViewModel { get; set; }
    public required PositionOpenGridViewModel PositionOpenGridViewModel { get; set; }
    public required PositionClosedGridViewModel PositionClosedGridViewModel { get; set; }

    public required LogViewModel LogViewModel { get; set; }

    public BrowserView? BrowserView { get; set; }

    [ObservableProperty]
    private bool _analyzerActive = false;
    [ObservableProperty]
    private bool _soundsActive = false;
    [ObservableProperty]
    private bool _traderActive = false;

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
        LogViewModel logViewModel)
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
        LogViewModel = logViewModel;

        // TODO: Is there a better way
        AnalyzerActive = ApplicationStateService.AnalyzerActive;
        TraderActive = ApplicationStateService.TraderActive;
        SoundsActive = ApplicationStateService.SoundsActive;

        // Subscribe child ViewModels to filter event
        FilterTextChanged += SymbolGridViewModel.OnFilterTextChanged;
        FilterTextChanged += SignalGridViewModel.OnFilterTextChanged;

        App.EventOpenInInternalBrowser += OnOpenInInternalBrowserRequested;
    }

    private void OnOpenInInternalBrowserRequested(object? sender, string url)
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

    // not needed, but nice experiment
    //partial void OnAnalyzerActiveChanged(bool value)
    //{
    //    // should work, but does nothing except error in immediate output
    //    System.Diagnostics.Debug.WriteLine($"OnAnalyzerActiveChanged changed to: {AnalyzerActive}");
    //}

    //partial void OnSoundsActiveChanged(bool value)
    //{
    //    // should work, but does nothing except error in immediate output
    //    System.Diagnostics.Debug.WriteLine($"OnSoundsActiveChanged changed to: {SoundsActive}");
    //}

    //partial void OnTraderActiveChanged(bool value)
    //{
    //    // should work, but does nothing except error in immediate output
    //    // Breakpoint hier - werkt NU wel als je via code triggert
    //    System.Diagnostics.Debug.WriteLine($"OnTraderActiveChanged changed to: {TraderActive}");
    //}

    //partial void OnSymbolFilterTextChanged(string value)
    //{
    //    FilterTextChanged?.Invoke(this, value);
    //}

    //// Dit wordt NIET bij elke toetsaanslag aangeroepen, alleen bij LostFocus of Enter
    //partial void OnSymbolFilterTextChanged(string value)
    //{
    //    System.Diagnostics.Debug.WriteLine($"Filter changed to: {value}");
    //    FilterTextChanged?.Invoke(this, value);
    //}

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