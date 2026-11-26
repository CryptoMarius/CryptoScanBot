using Avalonia.Controls;

namespace CryptoScanner.Analyzers.ChochFvg.Config;

public partial class StrategyChochFvgSettingsView : UserControl
{
    public StrategyChochFvgSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyChochFvgSettingsViewModel();
        }
    }
}
