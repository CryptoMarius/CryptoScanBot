using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class MarketExitView : UserControl
{
    public MarketExitView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new MarketExitViewModel();
        }
    }
}
