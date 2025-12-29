using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

using CryptoScanner.Browser.ViewModels;
using CryptoScanner.Browser.Views;
using CryptoScanner.DashBoard.ViewModels;
using CryptoScanner.LiveData.ViewModels;
using CryptoScanner.Log.ViewModels;
using CryptoScanner.Services;
using CryptoScanner.Settings.Views;
using CryptoScanner.Signal.ViewModels;
using CryptoScanner.Symbol.ViewModels;
using CryptoScanner.Views;

namespace CryptoScanner.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public required ApplicationStateService ApplicationStateService { get; set; }
    public required DashBoardViewModel DashBoardViewModel { get; set; }
    public required SymbolGridViewModel SymbolGridViewModel { get; set; }
    public required SignalGridViewModel SignalGridViewModel { get; set; }
    public required LiveDataGridViewModel LiveDataGridViewModel { get; set; }
    public required BrowserViewModel BrowserViewModel { get; set; }
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
        DashBoardViewModel dashBoardViewModel,
        SymbolGridViewModel symbolGridViewModel,
        SignalGridViewModel signalGridViewModel,
        LiveDataGridViewModel liveDataGridViewModel,
        BrowserViewModel browserViewModel,
        LogViewModel logViewModel)
    {
        ApplicationStateService = applicationStateService;
        DashBoardViewModel = dashBoardViewModel;
        SymbolGridViewModel = symbolGridViewModel;
        SignalGridViewModel = signalGridViewModel;
        LiveDataGridViewModel = liveDataGridViewModel;
        BrowserViewModel = browserViewModel;
        LogViewModel = logViewModel;


        // Debug output
        System.Diagnostics.Debug.WriteLine($"MainViewModel created");
        System.Diagnostics.Debug.WriteLine($"DashBoardViewModel: {DashBoardViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"SymbolGridViewModel: {SymbolGridViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"SignalGridViewModel: {SignalGridViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"LiveDataGridViewModel: {LiveDataGridViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"BrowserViewModel: {BrowserViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"LogViewModel: {LogViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"Symbols count: {SymbolGridViewModel?.Symbols?.Count ?? 0}");
        System.Diagnostics.Debug.WriteLine($"Signals count: {SignalGridViewModel?.Signals?.Count ?? 0}");

        // Subscribe to SignalGrid events
        SignalGridViewModel!.EventOpenInInternalBrowser += OnOpenInInternalBrowserRequested;

        // TODO: Is there a better way
        AnalyzerActive = ApplicationStateService.AnalyzerActive;
        TraderActive = ApplicationStateService.TraderActive;
        SoundsActive = ApplicationStateService.SoundsActive;
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


    [RelayCommand]
    private async Task Settings()
    {
        System.Diagnostics.Debug.WriteLine($"Settings");
        if (DialogService != null)
            await DialogService.ShowDialogAsync<SettingsWindow>();
    }

    [RelayCommand]
    private async Task AboutAsync()
    {
        System.Diagnostics.Debug.WriteLine($"About");
        if (DialogService != null)
            await DialogService.ShowDialogAsync<AboutWindow>();
    }
}