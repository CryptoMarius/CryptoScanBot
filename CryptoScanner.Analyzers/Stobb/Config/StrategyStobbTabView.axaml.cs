using System.Diagnostics;

using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace CryptoScanner.Analyzers.Stobb.Config;

public partial class StrategyStobbTabView : UserControl
{
    public StrategyStobbTabView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new StrategyStobbTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnWikiTapped(object? sender, RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo("https://github.com/CryptoMarius/CryptoScanBot/wiki/analyzers/Stochastic-+-Bollinger-Bands-(STOBB)") { UseShellExecute = true });
    }
}