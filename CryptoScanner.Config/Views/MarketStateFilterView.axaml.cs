using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class MarketStateFilterView : UserControl
{
    public MarketStateFilterView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new MarketStateFilterViewModel();
        }
    }
}
