using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Dlz.Config;

public partial class StrategyDlzSettingsView : UserControl
{
    public StrategyDlzSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyDlzSettingsViewModel();
        }
    }
}
