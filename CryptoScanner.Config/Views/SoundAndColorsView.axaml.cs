using Avalonia.Controls;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class SoundAndColorsView : UserControl
{
    public SoundAndColorsView()
    {
        InitializeComponent();

        if (DataContext == null)
        {
            DataContext = new SoundAndColorsViewModel();
        }
    }
}
