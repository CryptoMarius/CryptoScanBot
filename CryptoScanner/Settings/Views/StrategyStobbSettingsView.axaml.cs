using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class StrategyStobbSettingsView : UserControl
{
    public StrategyStobbSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyStobbSettingsViewModel();
        }
    }
}
