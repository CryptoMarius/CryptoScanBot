using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace CryptoScanner.Analyzers.Bbma.Config;

public partial class StrategyBbmaTabView : UserControl
{
    public StrategyBbmaTabView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new StrategyBbmaTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
