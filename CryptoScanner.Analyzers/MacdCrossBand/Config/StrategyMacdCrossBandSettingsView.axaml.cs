using Avalonia.Controls;

namespace CryptoScanner.Analyzers.MacdCrossBand.Config;

public partial class StrategyMacdCrossBandSettingsView : UserControl
{
    public StrategyMacdCrossBandSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyMacdCrossBandSettingsViewModel();
        }
    }
}
