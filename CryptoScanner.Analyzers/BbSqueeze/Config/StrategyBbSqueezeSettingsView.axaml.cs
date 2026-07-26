using Avalonia.Controls;

namespace CryptoScanner.Analyzers.BbSqueeze.Config;

public partial class StrategyBbSqueezeSettingsView : UserControl
{
    public StrategyBbSqueezeSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyBbSqueezeSettingsViewModel();
        }
    }
}
