using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class TraderEntryConditionsView : UserControl
{
    public TraderEntryConditionsView()
    {
        InitializeComponent();

        if (Design.IsDesignMode && DataContext == null)
        {
            DataContext = new TraderEntryConditionsViewModel();
        }
    }
}
