using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

using CryptoScanner.Config.ViewModels;

namespace CryptoScanner.Config.Views;

public partial class StrategyView : UserControl
{
    public StrategyView()
    {
        InitializeComponent();

        // Set DataContext if not already set by parent
        if (DataContext == null)
        {
            DataContext = new StrategyViewModel();
        }
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
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
