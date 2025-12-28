using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

public partial class TraderRulesView : UserControl
{
    public TraderRulesView()
    {
        InitializeComponent();

        if (Design.IsDesignMode && DataContext == null)
        {
            DataContext = new TraderRulesViewModel();
        }
    }
}
