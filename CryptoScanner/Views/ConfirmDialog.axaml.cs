using Avalonia.Controls;
using Avalonia.Interactivity;

namespace CryptoScanner.Views;

public partial class ConfirmDialog : Window
{
    public ConfirmDialog(string message, string title = "Confirm")
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
    }

    private void OnYesClick(object? sender, RoutedEventArgs e) => Close(true);
    private void OnNoClick(object? sender, RoutedEventArgs e)  => Close(false);
}
