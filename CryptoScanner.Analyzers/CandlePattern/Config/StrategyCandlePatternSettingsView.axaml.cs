using Avalonia.Controls;

namespace CryptoScanner.Analyzers.CandlePattern.Config;

public partial class StrategyCandlePatternSettingsView : UserControl
{
    public StrategyCandlePatternSettingsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyCandlePatternSettingsViewModel();
        }
    }
}
