using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CryptoScanner.Analyzers.CandlePattern.Config;

public partial class StrategyCandlePatternTabView : UserControl
{
    public StrategyCandlePatternTabView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new StrategyCandlePatternTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
