using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class ApiTelegramView : UserControl
{
    public ApiTelegramView()
    {
        InitializeComponent();

        if (Design.IsDesignMode && DataContext == null)
        {
            DataContext = new ApiTelegramViewModel();
        }
    }
}
