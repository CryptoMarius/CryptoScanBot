using Avalonia.Controls;

namespace CryptoScanner.Analyzers.ImpulseBox.Config;

public partial class StrategyImpulseBoxSettingsView : UserControl
{
    public StrategyImpulseBoxSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyImpulseBoxSettingsViewModel();
        }
    }
}
