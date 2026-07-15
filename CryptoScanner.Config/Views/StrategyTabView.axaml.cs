using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class StrategyTabView : UserControl
{
    public StrategyTabView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new StrategyTabViewModel();
        }

#if !DEBUG
        BbmaTab.IsVisible = false;
#endif
#if !EXPERIMENTAL
        BabaTab.IsVisible = false;
        AtrRbTab.IsVisible = false;
        BreTab.IsVisible = false;
#endif
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}