using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

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
