using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

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
