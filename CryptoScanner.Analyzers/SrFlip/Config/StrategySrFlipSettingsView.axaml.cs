using Avalonia.Controls;

namespace CryptoScanner.Analyzers.SrFlip.Config;

public partial class StrategySrFlipSettingsView : UserControl
{
    public StrategySrFlipSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategySrFlipSettingsViewModel();
        }
    }
}
