using Avalonia.Controls;
using Avalonia.Interactivity;

using CryptoScanner.Core.Settings;
using CryptoScanner.Emulator.ViewModels;

namespace CryptoScanner.Emulator.Views;

/// <summary>
/// The Assets dialog of the run parameters: edits the start assets of a run (the Default column of
/// Paper assets). Closes with a bool result (true = take the list over), the list itself is in
/// <see cref="ViewModel"/>.Result.
/// </summary>
public partial class PaperAssetDefaultsWindow : Window
{
    public PaperAssetDefaultsViewModel ViewModel { get; }

    public PaperAssetDefaultsWindow() : this([])
    {
    }


    public PaperAssetDefaultsWindow(IEnumerable<CryptoPaperAssetDefault> defaults)
    {
        InitializeComponent();
        ViewModel = new PaperAssetDefaultsViewModel(defaults);
        DataContext = ViewModel;
    }


    private void OnOk(object? sender, RoutedEventArgs e) => Close(true);


    private void OnCancel(object? sender, RoutedEventArgs e) => Close(false);
}
