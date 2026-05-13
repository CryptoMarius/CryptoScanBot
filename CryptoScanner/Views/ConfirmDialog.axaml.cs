using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CryptoScanner.Views;

public partial class ConfirmDialog : Window
{
    // Required by the Avalonia XAML runtime loader (designer / hot-reload).
    // Production code uses the message-taking constructor below.
    public ConfirmDialog()
    {
        InitializeComponent();
    }

    public ConfirmDialog(string message, string title = "Confirm") : this()
    {
        Title = title;
        MessageText.Text = message;
    }

    private void OnYesClick(object? sender, RoutedEventArgs e) => Close(true);
    private void OnNoClick(object? sender, RoutedEventArgs e) => Close(false);
}
