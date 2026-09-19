using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class SymbolTrendFilterView : UserControl
{
    public SymbolTrendFilterView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new SymbolTrendFilterViewModel();
        }
    }
}
