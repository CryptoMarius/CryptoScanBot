using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

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
