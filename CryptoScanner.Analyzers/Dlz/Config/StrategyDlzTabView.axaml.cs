using System.Diagnostics;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace CryptoScanner.Analyzers.Dlz.Config;

public partial class StrategyDlzTabView : UserControl
{
    public StrategyDlzTabView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new StrategyDlzTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnWikiTapped(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/CryptoMarius/CryptoScanBot/wiki/analyzers/Dominant-Liquidity-Zones-(DLZ)") { UseShellExecute = true });
    }
}
