using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

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
