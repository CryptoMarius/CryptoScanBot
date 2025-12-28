using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class TraderTabView : UserControl
{
    public TraderTabView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new TraderTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}