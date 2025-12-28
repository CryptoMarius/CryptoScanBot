using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class TraderStopLossView : UserControl
{
    public TraderStopLossView()
    {
        InitializeComponent();

        if (Design.IsDesignMode && DataContext == null)
        {
            DataContext = new TraderStopLossViewModel();
        }
    }
}
