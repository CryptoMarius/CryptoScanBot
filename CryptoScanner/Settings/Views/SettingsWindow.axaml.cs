using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new SettingsViewModel();
        }
    }
}
