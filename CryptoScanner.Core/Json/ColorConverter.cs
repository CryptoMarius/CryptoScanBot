using Avalonia.Media;

using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Json;

public class ColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.String)
        {
            string? text = reader.GetString();
            if (!string.IsNullOrEmpty(text))
            {
                if (text.StartsWith('#') && text.Length == 9)
                {
                    // Hexadecimal only "#AARRGGBB" (uppercase, cross-platform)
                    byte a = byte.Parse(text.Substring(1, 2), NumberStyles.HexNumber);
                    byte r = byte.Parse(text.Substring(3, 2), NumberStyles.HexNumber);
                    byte g = byte.Parse(text.Substring(5, 2), NumberStyles.HexNumber);
                    byte b = byte.Parse(text.Substring(7, 2), NumberStyles.HexNumber);
                    return Color.FromArgb(a, r, g, b);
                }
                else if (text.Contains(','))
                {
                    // Old format: "255, 128, 128" (no alpha)
                    string[] values = text.Split(',');
                    if (int.TryParse(values[0], out int r) &&
                        int.TryParse(values[1], out int g) &&
                        int.TryParse(values[2], out int b))
                        return Color.FromRgb((byte)r, (byte)g, (byte)b);
                }
                // Use Color.Parse for string representations like "#RRGGBB" or "Red"
                return Color.Parse(text);
            }
        }

        return JsonSerializer.Deserialize<Color>(ref reader, JsonTools.DeSerializerOptions);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        // Hexadecimal only "#AARRGGBB" (uppercase, cross-platform)
        string output = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
        writer.WriteStringValue(output);
    }

    //// Was: return ColorTranslator.FromHtml(text); // WinForms-only
    ////return TryParseHtmlColor(text, out Color named) ? named : Color.Empty;


    //// Parses "#RRGGBB" and known named colors without ColorTranslator (cross-platform).
    //private static bool TryParseHtmlColor(string text, out Color color)
    //{
    //    if (text.StartsWith('#') && text.Length == 7)
    //    {
    //        if (byte.TryParse(text.AsSpan(1, 2), NumberStyles.HexNumber, null, out byte r) &&
    //            byte.TryParse(text.AsSpan(3, 2), NumberStyles.HexNumber, null, out byte g) &&
    //            byte.TryParse(text.AsSpan(5, 2), NumberStyles.HexNumber, null, out byte b))
    //        {
    //            color = Color.FromArgb(255, r, g, b);
    //            return true;
    //        }
    //    }
    //    // Named colors (e.g. "Red", "White") — Color.FromName returns Empty when unknown
    //    color = Color.FromName(text);
    //    return color.IsKnownColor;
    //}

}