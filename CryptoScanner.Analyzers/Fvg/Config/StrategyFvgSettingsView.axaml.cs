using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Fvg.Config;

public partial class StrategyFvgSettingsView : UserControl
{
    public StrategyFvgSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyFvgSettingsViewModel();
        }
    }
}
