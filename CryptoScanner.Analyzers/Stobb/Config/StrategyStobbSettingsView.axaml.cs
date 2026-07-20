using Avalonia.Controls;

namespace CryptoScanner.Analyzers.Stobb.Config;

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
