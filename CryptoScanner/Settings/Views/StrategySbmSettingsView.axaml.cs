using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class StrategySbmSettingsView : UserControl
{
    public StrategySbmSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategySbmSettingsViewModel();
        }
    }
}
