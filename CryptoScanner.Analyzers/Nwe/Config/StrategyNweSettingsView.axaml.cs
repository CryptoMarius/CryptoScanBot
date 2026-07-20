using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Analyzers.Nwe.Config;

public partial class StrategyNweSettingsView : UserControl
{
    public StrategyNweSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyNweSettingsViewModel();
        }
    }
}
