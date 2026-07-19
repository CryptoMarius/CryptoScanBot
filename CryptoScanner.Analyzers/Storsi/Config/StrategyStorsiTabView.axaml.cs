using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace CryptoScanner.Analyzers.Storsi.Config;

public partial class StrategyStorsiTabView : UserControl
{
    public StrategyStorsiTabView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new StrategyStorsiTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}