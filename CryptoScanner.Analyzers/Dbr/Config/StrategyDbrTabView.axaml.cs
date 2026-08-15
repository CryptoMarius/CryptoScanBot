using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using System.Diagnostics;


namespace CryptoScanner.Analyzers.Dbr.Config;

public partial class StrategyDbrTabView : UserControl
{
    public StrategyDbrTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyDbrTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnWikiTapped(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/CryptoMarius/CryptoScanBot/wiki/Donchian-Breakout-Reversion-(DBR)") { UseShellExecute = true });
    }
}
