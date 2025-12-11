using CommunityToolkit.Mvvm.ComponentModel;

using CryptoScanner.DashBoard.ViewModels;
using CryptoScanner.Signal.ViewModels;
using CryptoScanner.Symbol.ViewModels;

namespace CryptoScanner.MainForm.ViewModels;

public partial class MainViewModel : ObservableObject
{
    public DashBoardViewModel DashBoardViewModel { get; }
    public SymbolGridViewModel SymbolGridViewModel { get; }
    public SignalGridViewModel SignalGridViewModel { get; }

    public MainViewModel(
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