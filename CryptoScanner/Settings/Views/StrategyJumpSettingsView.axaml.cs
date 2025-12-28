using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class StrategyJumpSettingsView : UserControl
{
    public StrategyJumpSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyJumpSettingsViewModel();
        }
    }
}
