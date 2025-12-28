using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

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
