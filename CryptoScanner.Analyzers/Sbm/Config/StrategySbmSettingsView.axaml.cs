using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Sbm.Config;

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
