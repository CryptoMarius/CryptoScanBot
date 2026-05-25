using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class StrategyWaveTrendSettingsView : UserControl
{
    public StrategyWaveTrendSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyWaveTrendSettingsViewModel();
        }
    }
}
