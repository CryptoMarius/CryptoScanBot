using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class StrategyWaveTrendTabView : UserControl
{
    public StrategyWaveTrendTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyWaveTrendTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
