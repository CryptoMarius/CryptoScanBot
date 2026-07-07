using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class StrategyEntryConditionsView : UserControl
{
    public StrategyEntryConditionsView()
    {
        InitializeComponent();

        if (Design.IsDesignMode && DataContext == null)
        {
            DataContext = new StrategyEntryConditionsViewModel();
        }
    }
}
