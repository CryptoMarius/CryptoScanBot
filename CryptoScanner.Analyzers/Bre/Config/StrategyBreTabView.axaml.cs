using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace CryptoScanner.Analyzers.Bre.Config;

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
