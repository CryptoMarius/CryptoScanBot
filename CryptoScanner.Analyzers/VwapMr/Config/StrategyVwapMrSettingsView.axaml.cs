using Avalonia.Controls;

namespace CryptoScanner.Analyzers.VwapMr.Config;

public partial class StrategyVwapMrSettingsView : UserControl
{
    public StrategyVwapMrSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyVwapMrSettingsViewModel();
        }
    }
}
