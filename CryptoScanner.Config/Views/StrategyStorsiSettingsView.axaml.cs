using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

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
