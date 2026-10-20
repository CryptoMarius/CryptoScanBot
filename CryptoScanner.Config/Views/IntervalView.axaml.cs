using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;

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

    /// <summary>
    /// A click on the chip box opens the full list. The "x" of a chip is a button that handles the
    /// press itself, so removing a chip does not open the list as well.
    /// </summary>
    private void ChipBox_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is Control box)
            FlyoutBase.ShowAttachedFlyout(box);
    }
}
