using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CryptoScanner.Analyzers.BbSqueeze.Config;

public partial class StrategyBbSqueezeTabView : UserControl
{
    public StrategyBbSqueezeTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyBbSqueezeTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
