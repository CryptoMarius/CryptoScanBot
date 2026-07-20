using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace CryptoScanner.Analyzers.Jump.Config;

public partial class StrategyJumpTabView : UserControl
{
    public StrategyJumpTabView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new StrategyJumpTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}