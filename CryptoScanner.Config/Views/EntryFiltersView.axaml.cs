using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class EntryFiltersView : UserControl
{
    public EntryFiltersView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new EntryFiltersViewModel();
        }
    }
}
