using Avalonia.Controls;

namespace CryptoScanner.Analyzers.HeikinAshi.Config;

public partial class StrategyHeikinAshiSettingsView : UserControl
{
    public StrategyHeikinAshiSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyHeikinAshiSettingsViewModel();
        }
    }
}
