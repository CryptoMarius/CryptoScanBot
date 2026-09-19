using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace CryptoScanner.Analyzers.Choch.Config;

public partial class StrategyChochTabView : UserControl
{
    public StrategyChochTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyChochTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
