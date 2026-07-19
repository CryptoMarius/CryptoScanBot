using Avalonia.Controls;


namespace CryptoScanner.Analyzers.AtrRb.Config;

public partial class StrategyAtrRbSettingsView : UserControl
{
    public StrategyAtrRbSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyAtrRbSettingsViewModel();
        }
    }
}
