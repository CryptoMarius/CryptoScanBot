using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class MarketTrendFilterView : UserControl
{
    public MarketTrendFilterView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new MarketTrendFilterViewModel();
        }
    }
}
