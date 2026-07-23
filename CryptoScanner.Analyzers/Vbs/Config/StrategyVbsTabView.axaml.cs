using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace CryptoScanner.Analyzers.Vbs.Config;

public partial class StrategyVbsTabView : UserControl
{
    public StrategyVbsTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyVbsTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
