using Avalonia.Controls;

namespace CryptoScanner.Analyzers.MacdCross.Config;

public partial class StrategyMacdCrossSettingsView : UserControl
{
    public StrategyMacdCrossSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyMacdCrossSettingsViewModel();
        }
    }
}
