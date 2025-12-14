using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.DashBoard.ViewModels;
using CryptoScanner.Signal.ViewModels;
using CryptoScanner.Symbol.ViewModels;

namespace CryptoScanner.ViewModels;

public partial class MainWindowViewModel : ObservableObject
{
    public required DashBoardViewModel DashBoardViewModel { get; set; }
    public required SymbolGridViewModel SymbolGridViewModel { get; set; }
    public required SignalGridViewModel SignalGridViewModel { get; set; }

    public MainWindowViewModel()
    {
    }

    public MainWindowViewModel(
        DashBoardViewModel dashBoardViewModel,
        SymbolGridViewModel symbolGridViewModel,
        SignalGridViewModel signalGridViewModel)
    {
        DashBoardViewModel = dashBoardViewModel;
        SymbolGridViewModel = symbolGridViewModel;
        SignalGridViewModel = signalGridViewModel;

        // Debug output
        System.Diagnostics.Debug.WriteLine($"MainViewModel created");
        System.Diagnostics.Debug.WriteLine($"DashBoardViewModel: {DashBoardViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"SymbolGridViewModel: {SymbolGridViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"SignalGridViewModel: {SignalGridViewModel != null}");
        System.Diagnostics.Debug.WriteLine($"Symbols count: {SymbolGridViewModel?.Symbols?.Count ?? 0}");
        System.Diagnostics.Debug.WriteLine($"Signals count: {SignalGridViewModel?.Signals?.Count ?? 0}");
    }
}