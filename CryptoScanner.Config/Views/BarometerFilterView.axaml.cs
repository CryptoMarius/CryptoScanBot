using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class BarometerFilterView : UserControl
{
    public BarometerFilterView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new BarometerFilterViewModel();
        }
    }
}
