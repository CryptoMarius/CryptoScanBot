using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace CryptoScanner.Analyzers.IChimokuKumoBreakout.Config;

public partial class StrategyIChimokuKumoBreakoutTabView : UserControl
{
    public StrategyIChimokuKumoBreakoutTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyIChimokuKumoBreakoutTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
