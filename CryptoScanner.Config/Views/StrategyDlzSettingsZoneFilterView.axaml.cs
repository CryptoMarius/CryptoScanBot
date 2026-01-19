using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

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
