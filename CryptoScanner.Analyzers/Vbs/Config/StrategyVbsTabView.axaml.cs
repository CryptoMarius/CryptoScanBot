using System.Diagnostics;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;


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
        Process.Start(new ProcessStartInfo("https://github.com/CryptoMarius/CryptoScanBot/wiki/analyzers/VWAP-Band-Strategy-(VBS)") { UseShellExecute = true });
    }
}
