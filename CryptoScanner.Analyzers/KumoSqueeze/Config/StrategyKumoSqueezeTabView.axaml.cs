using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CryptoScanner.Analyzers.KumoSqueeze.Config;

public partial class StrategyKumoSqueezeTabView : UserControl
{
    public StrategyKumoSqueezeTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyKumoSqueezeTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
