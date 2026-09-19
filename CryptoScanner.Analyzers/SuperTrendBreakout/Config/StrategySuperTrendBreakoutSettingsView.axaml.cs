using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace CryptoScanner.Analyzers.SuperTrendBreakout.Config;

public partial class StrategySuperTrendBreakoutSettingsView : UserControl
{
    public StrategySuperTrendBreakoutSettingsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
