using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CryptoScanner.Views;

/// <summary>
/// Window for managing column visibility in the Object Grid
/// </summary>
public partial class ColumnWindow : Window
{
    public ColumnWindow()
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
