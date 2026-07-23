using Avalonia.Controls;


namespace CryptoScanner.Analyzers.Vbs.Config;

public partial class StrategyVbsSettingsView : UserControl
{
    public StrategyVbsSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyVbsSettingsViewModel();
        }
    }
}
