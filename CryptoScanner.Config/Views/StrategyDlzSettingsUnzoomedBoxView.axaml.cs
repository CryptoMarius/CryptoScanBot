using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class StrategyDlzSettingsUnzoomedBoxView : UserControl
{
    public StrategyDlzSettingsUnzoomedBoxView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyDlzSettingsUnzoomedBoxViewModel();
        }
    }
}
