using CryptoScanner.Core.Model;

namespace CryptoScanner.UI.Models;

/// <summary>
/// Forgiving wrapper around <see cref="CoreColor.Parse"/>. That method throws on anything that is
/// not a clean #RRGGBB / #AARRGGBB, and a half typed value in a text field is exactly that — the
/// editors need a value back, not an exception.
/// </summary>
public static class ColorTextHelper
{
    public static CoreColor Parse(string? text, CoreColor fallback)
    {
        text = text?.Trim() ?? "";
        if (text.Length > 0 && !text.StartsWith('#'))
            text = "#" + text;

        try
        {
            var color = CoreColor.Parse(text);
            return color == default ? fallback : color;
        }
        catch (FormatException)
        {
            return fallback;
        }
    }
}
