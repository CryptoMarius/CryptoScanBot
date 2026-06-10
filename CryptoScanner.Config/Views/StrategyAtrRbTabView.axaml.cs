using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class StrategyAtrRbTabView : UserControl
{
    public StrategyAtrRbTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyAtrRbTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
