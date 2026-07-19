using Avalonia.Controls;


namespace CryptoScanner.Analyzers.Baba.Config;

public partial class StrategyBabaSettingsView : UserControl
{
    public StrategyBabaSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyBabaSettingsViewModel();
        }
    }
}
