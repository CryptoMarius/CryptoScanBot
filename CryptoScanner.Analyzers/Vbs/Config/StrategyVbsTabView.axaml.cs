using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

using System.Diagnostics;


namespace CryptoScanner.Analyzers.Vbs.Config;

public partial class StrategyVbsTabView : UserControl
{
    public StrategyVbsTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new StrategyVbsTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnWikiTapped(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/CryptoMarius/CryptoScanBot/wiki/VWAP-Band-Strategy-(VBS)") { UseShellExecute = true });
    }
}
