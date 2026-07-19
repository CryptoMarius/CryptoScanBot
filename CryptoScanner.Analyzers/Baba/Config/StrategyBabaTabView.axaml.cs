using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace CryptoScanner.Analyzers.Baba.Config;

public partial class StrategyBabaTabView : UserControl
{
    public StrategyBabaTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyBabaTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
