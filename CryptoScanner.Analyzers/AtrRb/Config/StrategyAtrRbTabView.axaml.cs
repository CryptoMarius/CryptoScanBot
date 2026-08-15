using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using System.Diagnostics;


namespace CryptoScanner.Analyzers.AtrRb.Config;

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

    private void OnWikiTapped(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/CryptoMarius/CryptoScanBot/wiki/ATR-Range-Breakout-(AtrRb)") { UseShellExecute = true });
    }
}
