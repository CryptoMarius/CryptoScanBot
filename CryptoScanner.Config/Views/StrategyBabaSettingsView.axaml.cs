using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class StrategyBabaSettingsView : UserControl
{
    public StrategyBabaSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyBabaSettingsViewModel();
        }
    }
}
