using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

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
}