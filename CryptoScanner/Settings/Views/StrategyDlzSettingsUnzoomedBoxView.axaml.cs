using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

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
