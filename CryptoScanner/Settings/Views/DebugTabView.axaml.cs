using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class DebugTabView : UserControl
{
    public DebugTabView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new DebugTabViewModel();
        }
    }
}
