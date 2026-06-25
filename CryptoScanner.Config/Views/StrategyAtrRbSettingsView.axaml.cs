using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

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
