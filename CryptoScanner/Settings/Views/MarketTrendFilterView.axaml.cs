using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

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
