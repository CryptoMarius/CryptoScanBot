using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Vbs.Config;

public partial class StrategyVbsSettingsTakeProfitView : UserControl
{
    public StrategyVbsSettingsTakeProfitView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyVbsSettingsViewModel();
        }
    }
}
