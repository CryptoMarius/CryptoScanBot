using Avalonia.Controls;


namespace CryptoScanner.Analyzers.Bre.Config;

public partial class StrategyBreSettingsView : UserControl
{
    public StrategyBreSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyBreSettingsViewModel();
        }
    }
}
