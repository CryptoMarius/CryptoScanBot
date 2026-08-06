using System.Globalization;

namespace CryptoScanner.Core.Model;

/// <summary>
/// Framework-agnostic ARGB color struct, replacing Avalonia.Media.Color in the Core layer.
/// Convert to/from Avalonia.Media.Color or System.Drawing.Color at the UI boundary.
/// </summary>
public readonly struct CoreColor : IEquatable<CoreColor>
{
    public byte A { get; }
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }

    public CoreColor(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public static CoreColor FromArgb(byte a, byte r, byte g, byte b) => new(a, r, g, b);
    public static CoreColor FromRgb(byte r, byte g, byte b) => new(0xFF, r, g, b);

    public static CoreColor Parse(string text)
    {
        if (string.IsNullOrEmpty(text))
            return default;

        if (text.StartsWith('#'))
        {
            if (text.Length == 9)
            {
                byte a = byte.Parse(text.AsSpan(1, 2), NumberStyles.HexNumber);
                byte r = byte.Parse(text.AsSpan(3, 2), NumberStyles.HexNumber);
                byte g = byte.Parse(text.AsSpan(5, 2), NumberStyles.HexNumber);
                byte b = byte.Parse(text.AsSpan(7, 2), NumberStyles.HexNumber);
                return new CoreColor(a, r, g, b);
            }
            if (text.Length == 7)
            {
                byte r = byte.Parse(text.AsSpan(1, 2), NumberStyles.HexNumber);
                byte g = byte.Parse(text.AsSpan(3, 2), NumberStyles.HexNumber);
                byte b = byte.Parse(text.AsSpan(5, 2), NumberStyles.HexNumber);
                return FromRgb(r, g, b);
            }
        }

        return default;
    }

    public override string ToString() => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

    public bool Equals(CoreColor other) => A == other.A && R == other.R && G == other.G && B == other.B;
    public override bool Equals(object? obj) => obj is CoreColor other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(A, R, G, B);
    public static bool operator ==(CoreColor left, CoreColor right) => left.Equals(right);
    public static bool operator !=(CoreColor left, CoreColor right) => !left.Equals(right);
}
