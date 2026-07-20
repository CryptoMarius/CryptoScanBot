using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Dlz.Config;

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
