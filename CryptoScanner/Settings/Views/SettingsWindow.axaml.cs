using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        var viewModel = new SettingsViewModel();
        DataContext = viewModel;

        // Subscribe to close event
        viewModel.CloseRequested += (s, e) => Close();
    }
}
