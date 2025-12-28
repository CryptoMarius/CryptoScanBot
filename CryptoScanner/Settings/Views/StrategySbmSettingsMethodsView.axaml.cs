using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

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
