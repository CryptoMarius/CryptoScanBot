using System.ComponentModel;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoScanBot.Core.Intern;

public class ColorConverter : JsonConverter<Color>
{
    public override Color Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType is JsonTokenType.String)
        {
            string text = reader.GetString();
            if (!string.IsNullOrEmpty(text))
            {
                if (text.Contains(','))
                {
                    string[] values = text.Split(',');
                    if (int.TryParse(values[0], out int r) &&
                        int.TryParse(values[1], out int g) &&
                        int.TryParse(values[2], out int b))
                        return Color.FromArgb(r, g, b);
                }
                return ColorTranslator.FromHtml(text);
            }
        }

        return JsonSerializer.Deserialize<Color>(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, Color value, JsonSerializerOptions options)
    {
        string output;

        if (value == Color.Empty)
        {
            output = string.Empty;
        }
        else if (value.IsNamedColor)
        {
            output = value.Name;
        }
        else
        {
            TypeConverter intConverter = TypeDescriptor.GetConverter(typeof(int));
            int nArg = 0;

            string[] args;
            if (value.A < 255)
            {
                args = new string[4];
                args[nArg++] = intConverter.ConvertToString(value.A);
            }
            else
            {
                args = new string[3];
            }

            args[nArg++] = intConverter.ConvertToString(value.R);
            args[nArg++] = intConverter.ConvertToString(value.G);
            args[nArg++] = intConverter.ConvertToString(value.B);

            output = string.Join(", ", args);
        }

        writer.WriteStringValue(output);
    }
}