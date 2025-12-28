using Avalonia.Controls;
using Avalonia.Markup.Xaml;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class BlackAndWhiteListTabView : UserControl
{
    public BlackAndWhiteListTabView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new BlackAndWhiteListTabViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }
}