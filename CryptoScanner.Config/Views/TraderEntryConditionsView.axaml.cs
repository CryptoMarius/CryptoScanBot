using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

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
