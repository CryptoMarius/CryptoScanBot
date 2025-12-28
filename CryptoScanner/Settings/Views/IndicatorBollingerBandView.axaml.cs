using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class IndicatorBollingerBandView : UserControl
{
    public IndicatorBollingerBandView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new IndicatorBollingerBandViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}