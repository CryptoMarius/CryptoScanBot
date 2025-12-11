using Avalonia;
using Avalonia.Media;

namespace CryptoScanner.Helpers;

public static class BrushHelper
{
    public static IBrush GetResource(string resourceKey)
    {
        if (Application.Current?.TryGetResource(resourceKey, null, out var resource) == true
            && resource is IBrush brush)
        {
            return brush;
        }

        // Fallback
        return Brushes.Black;
    }

    public static IBrush PriceUp => GetResource("PriceUpBrush");
    public static IBrush PriceDown => GetResource("PriceDownBrush");
    public static IBrush PriceNeutral => GetResource("PriceNeutralBrush");
}