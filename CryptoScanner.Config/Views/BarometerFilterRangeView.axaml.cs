using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

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
