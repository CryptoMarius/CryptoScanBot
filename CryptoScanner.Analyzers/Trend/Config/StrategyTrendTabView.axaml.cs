using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace CryptoScanner.Analyzers.Trend.Config;

public partial class StrategyTrendTabView : UserControl
{
    public StrategyTrendTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyTrendTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
