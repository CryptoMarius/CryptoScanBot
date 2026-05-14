using Avalonia.Controls;
using Avalonia.Input;

using CryptoScanner.ViewModels;

namespace CryptoScanner.Views;

public partial class DashBoardInformationView : UserControl
{
    public DashBoardInformationView()
    {
        InitializeComponent();
    }

    private void OnSymbolTapped(object? sender, TappedEventArgs e)
    {
        if (sender is TextBlock { Tag: DashboardSymbolViewModel symbol } && DataContext is DashBoardInformationViewModel vm)
        {
            vm.OnSymbolTapped(symbol);
        }
    }

    private void OnScannerTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is DashBoardInformationViewModel vm)
            vm.ToggleScanner();
    }

    private void OnTraderTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is DashBoardInformationViewModel vm)
            vm.ToggleTrader();
    }

    private void OnSoundTapped(object? sender, TappedEventArgs e)
    {
        if (DataContext is DashBoardInformationViewModel vm)
            vm.ToggleSounds();
    }
}