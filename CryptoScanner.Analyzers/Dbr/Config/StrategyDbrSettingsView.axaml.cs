using Avalonia.Controls;


namespace CryptoScanner.Analyzers.Dbr.Config;

public partial class StrategyDbrSettingsView : UserControl
{
    public StrategyDbrSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyDbrSettingsViewModel();
        }
    }
}
