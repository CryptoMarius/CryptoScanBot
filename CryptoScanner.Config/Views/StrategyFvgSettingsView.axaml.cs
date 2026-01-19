using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

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
