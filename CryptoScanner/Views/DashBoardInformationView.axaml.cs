using Avalonia.Controls;
using Avalonia.Input;

namespace CryptoScanner.Views;

public partial class DashBoardInformationView : UserControl
{
    public DashBoardInformationView()
    {
        InitializeComponent();
    }


    /// <summary>
    /// Handle symbol click to open chart or details
    /// </summary>
    private void OnSymbolTapped(object? sender, TappedEventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            System.Diagnostics.Debug.WriteLine($"Symbol clicked: {textBlock.Text}");

            // TODO: Implement navigation to symbol details or TradingView
            // Example: Open TradingView with this symbol
        }
    }

}