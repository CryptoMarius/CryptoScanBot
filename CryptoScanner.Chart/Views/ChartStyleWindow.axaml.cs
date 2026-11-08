using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CryptoScanner.Views;

/// <summary>The dialog behind a gear of the Indicators menu; see ChartStyleDialogViewModel.</summary>
public partial class ChartStyleWindow : Window
{
    public ChartStyleWindow()
    {
        InitializeComponent();
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
