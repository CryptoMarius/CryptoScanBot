using Avalonia.Controls;
using Avalonia.Markup.Xaml;


namespace CryptoScanner.Analyzers.SuperTrendBreakout.Config;

public partial class StrategySuperTrendBreakoutTabView : UserControl
{
    public StrategySuperTrendBreakoutTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategySuperTrendBreakoutTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}
