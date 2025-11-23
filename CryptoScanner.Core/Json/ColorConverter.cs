using System.Drawing;
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
                        return Color.FromArgb(r, g, b);
                }
                return ColorTranslator.FromHtml(text);
            }
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            // ARGB-packed: uint
            uint argb = reader.GetUInt32();
            return Color.FromArgb((byte)(argb >> 24), (byte)(argb >> 16), (byte)(argb >> 8), (byte)argb);
        }

        throw new JsonException("Invalid JSON for Color: expected object with A/R/G/B");
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        // Hexadecimal only "#AARRGGBB" (uppercase, cross-platform)
        string hex = string.Format("#{0:X2}{1:X2}{2:X2}{3:X2}", value.A, value.R, value.G, value.B);
        writer.WriteStringValue(hex);
    }
}