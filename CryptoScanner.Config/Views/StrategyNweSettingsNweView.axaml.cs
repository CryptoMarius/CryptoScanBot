using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class StrategyNweSettingsNweView : UserControl
{
    public StrategyNweSettingsNweView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyNweSettingsNweViewModel();
        }
    }
}
