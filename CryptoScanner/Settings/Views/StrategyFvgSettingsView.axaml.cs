using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class StrategyFvgSettingsView : UserControl
{
    public StrategyFvgSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyFvgSettingsViewModel();
        }
    }
}
