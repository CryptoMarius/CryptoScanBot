using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class StrategyDlzSettingsView : UserControl
{
    public StrategyDlzSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyDlzSettingsViewModel();
        }
    }
}
