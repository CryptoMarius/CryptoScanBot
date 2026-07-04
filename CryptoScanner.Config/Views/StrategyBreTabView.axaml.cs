using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class StrategyBreTabView : UserControl
{
    public StrategyBreTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyBreTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
