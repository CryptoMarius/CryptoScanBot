using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace CryptoScanner.Analyzers.AtrRb.Config;

public partial class StrategyAtrRbTabView : UserControl
{
    public StrategyAtrRbTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyAtrRbTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
