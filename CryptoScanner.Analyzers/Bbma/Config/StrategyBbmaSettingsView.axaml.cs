using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Bbma.Config;

public partial class StrategyBbmaSettingsView : UserControl
{
    public StrategyBbmaSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyBbmaSettingsViewModel();
        }
    }
}
