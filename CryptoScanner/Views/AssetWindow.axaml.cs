using Avalonia.Controls;
using Avalonia.Interactivity;

using CryptoScanner.ViewModels;

namespace CryptoScanner.Views;

/// <summary>
/// Shows the paper-trading balances and lets them be corrected by hand. Reset throws everything away
/// and hands out the start capital again - the same thing the emulator does at the start of a run.
/// </summary>
public partial class AssetWindow : Window
{
    public AssetWindow()
    {
        InitializeComponent();
        DataContext = new AssetWindowViewModel();
    }

    private AssetWindowViewModel? ViewModel => DataContext as AssetWindowViewModel;

    private void OnRefreshClick(object? sender, RoutedEventArgs e) => ViewModel?.Reload();

    private void OnApplyClick(object? sender, RoutedEventArgs e) => ViewModel?.Apply();

    private async void OnResetClick(object? sender, RoutedEventArgs e)
    {
        // Deliberately behind a confirmation: this wipes the balances of a running paper session.
        ConfirmDialog dialog = new(
            "Throw every paper balance away and hand out the start capital again?", "Reset paper assets");
        if (await dialog.ShowDialog<bool>(this))
            ViewModel?.Reset();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
