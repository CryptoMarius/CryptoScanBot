using Avalonia.Controls;

namespace CryptoScanner.Analyzers.CandlePattern.Config;

public partial class StrategyCandlePatternShapeView : UserControl
{
    public StrategyCandlePatternShapeView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyCandlePatternSettingsViewModel();
        }
    }
}
