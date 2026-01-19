using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class ConfigurationWindow : Window
{
    public ConfigurationWindow()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new ConfigurationViewModel();
        }
    }
}
