using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class ApiAltradyView : UserControl
{
    public ApiAltradyView()
    {
        InitializeComponent();

        if (Design.IsDesignMode && DataContext == null)
        {
            DataContext = new ApiAltradyViewModel();
        }
    }
}
