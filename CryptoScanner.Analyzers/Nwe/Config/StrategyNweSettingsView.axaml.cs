using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Nwe.Config;

public partial class StrategyNweSettingsView : UserControl
{
    public StrategyNweSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyNweSettingsViewModel();
        }
    }
}
