using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class TraderTakeProfitView : UserControl
{
    public TraderTakeProfitView()
    {
        InitializeComponent();

        if (Design.IsDesignMode && DataContext == null)
        {
            DataContext = new TraderTakeProfitViewModel();
        }
    }
}
