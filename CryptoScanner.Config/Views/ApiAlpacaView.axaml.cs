using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class ApiAlpacaView : UserControl
{
    public ApiAlpacaView()
    {
        InitializeComponent();

        if (Design.IsDesignMode && DataContext == null)
        {
            DataContext = new ApiAlpacaViewModel();
        }
    }
}
