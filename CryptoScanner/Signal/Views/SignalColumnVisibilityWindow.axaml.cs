using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CryptoScanner.Signal.Views;

/// <summary>
/// Window for managing column visibility in the Signal Grid
/// </summary>
public partial class SignalColumnVisibilityWindow : Window
{
    public SignalColumnVisibilityWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Handle the close button click
    /// </summary>
    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
