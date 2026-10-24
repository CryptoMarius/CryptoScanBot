using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CryptoScanner.Analyzers.FvgLadder.Config;

public partial class StrategyFvgLadderTabView : UserControl
{
    public StrategyFvgLadderTabView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new StrategyFvgLadderTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
