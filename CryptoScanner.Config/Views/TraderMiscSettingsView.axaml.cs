using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class TraderMiscSettingsView : UserControl
{
    public TraderMiscSettingsView()
    {
        InitializeComponent();

        if (Design.IsDesignMode && DataContext == null)
        {
            DataContext = new TraderMiscSettingsViewModel();
        }
    }
}
