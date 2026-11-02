using Avalonia.Controls;

namespace CryptoScanner.Analyzers.PsarCross.Config;

public partial class StrategyPsarCrossSettingsView : UserControl
{
    public StrategyPsarCrossSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyPsarCrossSettingsViewModel();
        }
    }
}
