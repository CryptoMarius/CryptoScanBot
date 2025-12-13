using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CryptoScanner.Views;

/// <summary>
/// Window for managing column visibility in the Signal Grid
/// </summary>
public partial class ColumnVisibilityWindow : Window
{
    public ColumnVisibilityWindow()
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
