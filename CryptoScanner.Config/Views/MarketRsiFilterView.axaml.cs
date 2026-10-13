using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class MarketRsiFilterView : UserControl
{
    public MarketRsiFilterView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new MarketRsiFilterViewModel();
        }
    }
}
