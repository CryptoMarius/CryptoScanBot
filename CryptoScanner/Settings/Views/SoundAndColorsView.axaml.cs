using Avalonia.Controls;

using CryptoScanner.Settings.ViewModels;

namespace CryptoScanner.Settings.Views;

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
