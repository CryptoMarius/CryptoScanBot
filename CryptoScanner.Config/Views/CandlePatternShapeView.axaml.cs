using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class CandlePatternShapeView : UserControl
{
    public CandlePatternShapeView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new CandlePatternShapeViewModel();
        }
    }
}
