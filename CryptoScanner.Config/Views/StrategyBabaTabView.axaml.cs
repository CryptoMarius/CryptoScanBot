using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

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
