using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Services;
using CryptoScanner.Views;

namespace CryptoScanner.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
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


    public IDialogService? DialogService { get; set; }

    public BrowserView? BrowserView { get; set; }

    [ObservableProperty]
    private bool _analyzerActive = false;
    [ObservableProperty]
    private bool _soundsActive = false;
    [ObservableProperty]
    private bool _traderActive = false;

    public MainWindowViewModel()
    {
        System.Diagnostics.Debug.WriteLine($"MainViewModel default constructor called");
    }

    public MainWindowViewModel(
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


        // Debug output
        System.Diagnostics.Debug.WriteLine($"MainViewModel created");
        System.Diagnostics.Debug.WriteLine($"DashBoardInformationViewModel: {DashBoardInformationViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"SymbolGridViewModel: {SymbolGridViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"LiveDataGridViewModel: {LiveDataGridViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"PositionOpenGridViewModel: {PositionOpenGridViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"PositionClosedGridViewModel: {PositionOpenGridViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"BrowserViewModel: {BrowserViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"LogViewModel: {LogViewModel != null}");

        System.Diagnostics.Debug.WriteLine($"Symbols count: {SymbolGridViewModel?.Symbols?.Count ?? 0}");
        System.Diagnostics.Debug.WriteLine($"Signals count: {SignalGridViewModel?.Signals?.Count ?? 0}");
        System.Diagnostics.Debug.WriteLine($"LiveData count: {LiveDataGridViewModel?.LiveDatas?.Count ?? 0}");
        System.Diagnostics.Debug.WriteLine($"Positions open count: {PositionOpenGridViewModel?.Positions?.Count ?? 0}");
        System.Diagnostics.Debug.WriteLine($"Positions closed count: {PositionClosedGridViewModel?.Positions?.Count ?? 0}");
        System.Diagnostics.Debug.WriteLine($"LogLine count: {LogViewModel?.LogLines?.Count ?? 0}");

        // TODO: Is there a better way
        AnalyzerActive = ApplicationStateService.AnalyzerActive;
        TraderActive = ApplicationStateService.TraderActive;
        SoundsActive = ApplicationStateService.SoundsActive;

        App.EventOpenInInternalBrowser += OnOpenInInternalBrowserRequested;
    }

    private void OnOpenInInternalBrowserRequested(object? sender, string  url)
    {
        //BrowserViewModel.NavigateToTradingView(url);
        if (BrowserView != null)
        {
            System.Diagnostics.Debug.WriteLine($"OpenInBrowser: {url}");
            
            // Navigate triggers initialization + tab switch automatically
            BrowserView.Navigate(url);
        }

    }

    partial void OnAnalyzerActiveChanged(bool value)
    {
        // should work, but does nothing except error in immediate output
        System.Diagnostics.Debug.WriteLine($"OnAnalyzerActiveChanged changed to: {AnalyzerActive}");
    }

    partial void OnSoundsActiveChanged(bool value)
    {
        // should work, but does nothing except error in immediate output
        System.Diagnostics.Debug.WriteLine($"OnSoundsActiveChanged changed to: {SoundsActive}");
    }

    partial void OnTraderActiveChanged(bool value)
    {
        // should work, but does nothing except error in immediate output
        // Breakpoint hier - werkt NU wel als je via code triggert
        System.Diagnostics.Debug.WriteLine($"OnTraderActiveChanged changed to: {TraderActive}");
    }

    [RelayCommand]
    private void Close()
    {
        System.Diagnostics.Debug.WriteLine($"Close");
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
    public event EventHandler? CloseRequested;


}