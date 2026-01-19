using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

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
