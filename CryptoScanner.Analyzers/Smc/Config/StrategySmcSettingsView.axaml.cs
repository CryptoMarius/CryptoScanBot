using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Smc.Config;

public partial class StrategySmcSettingsView : UserControl
{
    public StrategySmcSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategySmcSettingsViewModel();
        }
    }
}
