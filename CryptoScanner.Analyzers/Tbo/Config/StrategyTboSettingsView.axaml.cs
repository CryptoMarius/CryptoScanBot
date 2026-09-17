using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Tbo.Config;

public partial class StrategyTboSettingsView : UserControl
{
    public StrategyTboSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyTboSettingsViewModel();
        }
    }
}
