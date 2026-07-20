using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Dlz.Config;

public partial class StrategyDlzSettingsZoneFilterView : UserControl
{
    public StrategyDlzSettingsZoneFilterView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyDlzSettingsZoneFilterViewModel();
        }
    }
}
