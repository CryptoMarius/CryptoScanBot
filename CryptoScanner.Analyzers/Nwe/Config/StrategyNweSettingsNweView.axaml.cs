using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Nwe.Config;

public partial class StrategyNweSettingsNweView : UserControl
{
    public StrategyNweSettingsNweView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyNweSettingsNweViewModel();
        }
    }
}
