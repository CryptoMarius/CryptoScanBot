using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

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
