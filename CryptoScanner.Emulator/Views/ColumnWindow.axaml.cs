using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CryptoScanner.Emulator.Views;

/// <summary>
/// Window for choosing which columns of the runs grid are shown. A copy of the scanner's
/// ColumnWindow, because the emulator cannot reference the scanner project.
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
