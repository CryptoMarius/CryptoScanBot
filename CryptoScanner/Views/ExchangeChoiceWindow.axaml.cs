using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace CryptoScanner.Views;

/// <summary>
/// Asks a new installation which exchange to scan. It is the first window of the application, shown
/// instead of the main window until a choice is made, see App.CreateExchangeChoiceWindow.
/// </summary>
public partial class ExchangeChoiceWindow : Window
{
    /// <summary>The exchange that was picked; null when the window was closed without a choice.</summary>
    public string? ChosenExchange { get; private set; }

    // Required by the Avalonia XAML runtime loader (designer / hot-reload).
    // Production code uses the constructor that takes the exchange names.
    public ExchangeChoiceWindow()
    {
        InitializeComponent();
    }

    public ExchangeChoiceWindow(IEnumerable<string> exchangeNames) : this()
    {
        ExchangeList.ItemsSource = exchangeNames.ToList();
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        StartButton.IsEnabled = ExchangeList.SelectedItem != null;
    }

    private void OnDoubleTapped(object? sender, TappedEventArgs e) => Choose();
    private void OnStartClick(object? sender, RoutedEventArgs e) => Choose();
    private void OnExitClick(object? sender, RoutedEventArgs e) => Close();

    private void Choose()
    {
        if (ExchangeList.SelectedItem is not string exchangeName)
            return;

        ChosenExchange = exchangeName;
        Close();
    }
}
