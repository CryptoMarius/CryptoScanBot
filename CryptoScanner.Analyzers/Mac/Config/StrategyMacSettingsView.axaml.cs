using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Mac.Config;

public partial class StrategyMacSettingsView : UserControl
{
    public StrategyMacSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyMacSettingsViewModel();
        }
    }
}
