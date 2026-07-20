using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Jump.Config;

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
