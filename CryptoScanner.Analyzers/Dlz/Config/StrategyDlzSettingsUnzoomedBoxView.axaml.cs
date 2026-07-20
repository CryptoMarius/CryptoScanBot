using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Dlz.Config;

public partial class StrategyDlzSettingsUnzoomedBoxView : UserControl
{
    public StrategyDlzSettingsUnzoomedBoxView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyDlzSettingsUnzoomedBoxViewModel();
        }
    }
}
