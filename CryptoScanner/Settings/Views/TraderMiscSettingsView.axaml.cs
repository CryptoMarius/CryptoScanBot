using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

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
