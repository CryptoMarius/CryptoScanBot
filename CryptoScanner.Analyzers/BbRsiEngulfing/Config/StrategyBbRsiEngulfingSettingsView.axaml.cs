using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace CryptoScanner.Analyzers.BbRsiEngulfing.Config;

public partial class StrategyBbRsiEngulfingSettingsView : UserControl
{
    public StrategyBbRsiEngulfingSettingsView()
    {
        InitializeComponent();
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
