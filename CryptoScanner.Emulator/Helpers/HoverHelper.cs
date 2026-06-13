using Avalonia;
using Avalonia.Controls;

namespace CryptoScanner.Emulator.Helpers;

public static class HoverHelper
{
    public static readonly AttachedProperty<bool> IsHoverProperty =
        AvaloniaProperty.RegisterAttached<Control, bool>("IsHover", typeof(HoverHelper));

    public static void SetIsHover(AvaloniaObject element, bool value) =>
        element.SetValue(IsHoverProperty, value);

    public static bool GetIsHover(AvaloniaObject element) =>
        element.GetValue(IsHoverProperty);
}
