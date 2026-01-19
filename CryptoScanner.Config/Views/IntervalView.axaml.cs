using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class IntervalView : UserControl
{
    public IntervalView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new IntervalViewModel();
        }
    }
}
