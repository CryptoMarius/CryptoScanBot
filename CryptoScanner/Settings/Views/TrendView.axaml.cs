using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class TrendView : UserControl
{
    public TrendView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new TrendViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}