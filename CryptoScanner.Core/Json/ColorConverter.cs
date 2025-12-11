using Avalonia.Media;
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
                if (text.Contains(','))
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
        // Write as #AARRGGBB hex string
        string output = $"#{value.A:X2}{value.R:X2}{value.G:X2}{value.B:X2}";
        writer.WriteStringValue(output);
    }
}