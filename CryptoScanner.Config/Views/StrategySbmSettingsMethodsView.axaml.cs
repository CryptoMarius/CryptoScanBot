using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class StrategySbmSettingsMethodsView : UserControl
{
    public StrategySbmSettingsMethodsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategySbmSettingsMethodsViewModel();
        }
    }
}
