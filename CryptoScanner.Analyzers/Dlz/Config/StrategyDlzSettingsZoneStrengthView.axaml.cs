using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Dlz.Config;

public partial class StrategyDlzSettingsZoneStrengthView : UserControl
{
    public StrategyDlzSettingsZoneStrengthView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyDlzSettingsViewModel();
        }
    }
}
