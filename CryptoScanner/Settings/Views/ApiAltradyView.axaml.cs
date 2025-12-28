using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

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
