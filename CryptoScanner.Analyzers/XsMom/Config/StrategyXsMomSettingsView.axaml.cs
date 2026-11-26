using Avalonia.Controls;

namespace CryptoScanner.Analyzers.XsMom.Config;

public partial class StrategyXsMomSettingsView : UserControl
{
    public StrategyXsMomSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyXsMomSettingsViewModel();
        }
    }
}
