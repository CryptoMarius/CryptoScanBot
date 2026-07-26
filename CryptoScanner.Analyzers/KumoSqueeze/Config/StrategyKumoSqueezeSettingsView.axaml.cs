using Avalonia.Controls;

namespace CryptoScanner.Analyzers.KumoSqueeze.Config;

public partial class StrategyKumoSqueezeSettingsView : UserControl
{
    public StrategyKumoSqueezeSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyKumoSqueezeSettingsViewModel();
        }
    }
}
