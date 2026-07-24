using System.Diagnostics;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace CryptoScanner.Analyzers.Nwe.Config;

public partial class StrategyNweTabView : UserControl
{
    public StrategyNweTabView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new StrategyNweTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnWikiTapped(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/CryptoMarius/CryptoScanBot/wiki/Nadaraya-Watson-Envelope-(NWE)") { UseShellExecute = true });
    }
}