using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class StrategyDlzSettingsZoomedBoxView : UserControl
{
    public StrategyDlzSettingsZoomedBoxView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyDlzSettingsZoomedBoxViewModel();
        }
    }
}
