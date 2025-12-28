using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class BarometerFilterRangeView : UserControl
{
    public BarometerFilterRangeView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new BarometerFilterRangeViewModel();
        }
    }
}
