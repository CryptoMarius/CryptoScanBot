using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class StrategySmcSettingsView : UserControl
{
    public StrategySmcSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategySmcSettingsViewModel();
        }
    }
}
