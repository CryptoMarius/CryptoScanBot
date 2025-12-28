using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

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
