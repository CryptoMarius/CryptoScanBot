using Avalonia.Controls;

namespace CryptoScanner.Analyzers.FvgLadder.Config;

public partial class StrategyFvgLadderSettingsView : UserControl
{
    public StrategyFvgLadderSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyFvgLadderSettingsViewModel();
        }
    }
}
