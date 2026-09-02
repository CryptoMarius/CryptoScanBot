using Avalonia.Controls;

namespace CryptoScanner.Analyzers.FailedBreakout.Config;

public partial class StrategyFailedBreakoutSettingsView : UserControl
{
    public StrategyFailedBreakoutSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyFailedBreakoutSettingsViewModel();
        }
    }
}
