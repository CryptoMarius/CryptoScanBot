using Avalonia.Controls;


namespace CryptoScanner.Analyzers.Storsi.Config;

public partial class StrategyStorsiSettingsView : UserControl
{
    public StrategyStorsiSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyStorsiSettingsViewModel();
        }
    }
}
