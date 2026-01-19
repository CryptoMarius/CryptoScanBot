using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class TrendIntervalFilterView : UserControl
{
    public TrendIntervalFilterView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new TrendIntervalFilterViewModel();
        }
    }
}
