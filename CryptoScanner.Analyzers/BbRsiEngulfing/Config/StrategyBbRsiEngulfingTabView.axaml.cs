using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace CryptoScanner.Analyzers.BbRsiEngulfing.Config;

public partial class StrategyBbRsiEngulfingTabView : UserControl
{
    public StrategyBbRsiEngulfingTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyBbRsiEngulfingTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
