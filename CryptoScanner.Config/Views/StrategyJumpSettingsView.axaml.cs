using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

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
