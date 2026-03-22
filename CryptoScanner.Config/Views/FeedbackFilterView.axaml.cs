using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class FeedbackFilterView : UserControl
{
    public FeedbackFilterView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new FeedbackFilterViewModel();
        }
    }
}
