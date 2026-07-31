using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Vbs.Config;

public partial class StrategyVbsSettingsStopLossView : UserControl
{
    public StrategyVbsSettingsStopLossView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyVbsSettingsViewModel();
        }
    }
}
