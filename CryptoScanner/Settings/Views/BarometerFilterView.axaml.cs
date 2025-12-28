using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

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
