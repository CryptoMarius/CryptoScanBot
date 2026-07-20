using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Sbm.Config;

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
