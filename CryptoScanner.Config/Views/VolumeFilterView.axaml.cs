using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class VolumeFilterView : UserControl
{
    public VolumeFilterView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new VolumeFilterViewModel();
        }
    }
}
