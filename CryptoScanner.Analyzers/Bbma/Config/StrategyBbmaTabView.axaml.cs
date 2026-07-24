using System.Diagnostics;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;


namespace CryptoScanner.Analyzers.Bbma.Config;

public partial class StrategyBbmaTabView : UserControl
{
    public StrategyBbmaTabView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new StrategyBbmaTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnWikiTapped(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/CryptoMarius/CryptoScanBot/wiki/analyzers/BBMA-Omni-(Bbma)") { UseShellExecute = true });
    }
}
