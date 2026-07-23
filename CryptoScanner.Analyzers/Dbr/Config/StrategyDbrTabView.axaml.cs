using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace CryptoScanner.Analyzers.Dbr.Config;

public partial class StrategyDbrTabView : UserControl
{
    public StrategyDbrTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyDbrTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
