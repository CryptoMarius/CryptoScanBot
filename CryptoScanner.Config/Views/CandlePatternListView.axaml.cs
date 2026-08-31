using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class CandlePatternListView : UserControl
{
    public CandlePatternListView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new CandlePatternListViewModel();
        }
    }
}
