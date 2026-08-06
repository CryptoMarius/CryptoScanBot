using Avalonia.Media;

using CryptoScanner.Core.Model;

namespace CryptoScanner.Config;

public static class CoreColorExtensions
{
    public static Color ToAvaloniaColor(this CoreColor c) => Color.FromArgb(c.A, c.R, c.G, c.B);
    public static CoreColor ToCoreColor(this Color c) => CoreColor.FromArgb(c.A, c.R, c.G, c.B);
}
