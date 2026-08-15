using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using System.Diagnostics;

namespace CryptoScanner.Analyzers.Fvg.Config;

public partial class StrategyFvgTabView : UserControl
{
    public StrategyFvgTabView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new StrategyFvgTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnWikiTapped(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/CryptoMarius/CryptoScanBot/wiki/Fair-Value-Gap-(FVG)") { UseShellExecute = true });
    }
}
