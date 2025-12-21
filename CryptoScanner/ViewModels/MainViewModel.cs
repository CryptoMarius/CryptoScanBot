using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.Browser.ViewModels;
using CryptoScanner.DashBoard.ViewModels;
using CryptoScanner.Log.ViewModels;
using CryptoScanner.Signal.Model;
using CryptoScanner.Signal.ViewModels;
using CryptoScanner.Symbol.ViewModels;

namespace CryptoScanner.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public required DashBoardViewModel DashBoardViewModel { get; set; }
    public required SymbolGridViewModel SymbolGridViewModel { get; set; }
    public required SignalGridViewModel SignalGridViewModel { get; set; }
    public required BrowserViewModel BrowserViewModel { get; set; }
    public required LogViewModel LogViewModel { get; set; }


    public MainWindowViewModel()
    {
    }

    public MainWindowViewModel(
        DashBoardViewModel dashBoardViewModel,
        SymbolGridViewModel symbolGridViewModel,
        SignalGridViewModel signalGridViewModel,
        BrowserViewModel browserViewModel,
        LogViewModel logViewModel)
    {
        DashBoardViewModel = dashBoardViewModel;
        SymbolGridViewModel = symbolGridViewModel;
        SignalGridViewModel = signalGridViewModel;
        BrowserViewModel = browserViewModel;
        LogViewModel = logViewModel;

        // Debug output
        System.Diagnostics.Debug.WriteLine($"MainViewModel created");
        System.Diagnostics.Debug.WriteLine($"DashBoardViewModel: {DashBoardViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"SymbolGridViewModel: {SymbolGridViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"SignalGridViewModel: {SignalGridViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"BrowserViewModel: {BrowserViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"LogViewModel: {LogViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"Symbols count: {SymbolGridViewModel?.Symbols?.Count ?? 0}");
        System.Diagnostics.Debug.WriteLine($"Signals count: {SignalGridViewModel?.Signals?.Count ?? 0}");

        // Subscribe to SignalGrid events
        SignalGridViewModel!.EventOpenInInternalBrowser += OnOpenInInternalBrowserRequested;
    
    }

    [ObservableProperty]
    private int _selectedTabIndex;

    private void OnOpenInInternalBrowserRequested(object? sender, string  url)
    {
        BrowserViewModel.NavigateToTradingView(url);

        // Switch to browser tab (index 1 of 2, afhankelijk van je layout)
        SelectedTabIndex = 1; // Browser tab
    }
}