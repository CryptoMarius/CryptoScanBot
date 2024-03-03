using System.Buffers;
using System.Drawing;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoScanBot.Intern;

public class RectangleConverter : JsonConverter<Rectangle>
{
    public override Rectangle Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Old Newtonsoft.Json value
        if (reader.TokenType is JsonTokenType.String)
        {
            int valueLength = reader.HasValueSequence ? (int)reader.ValueSequence.Length : reader.ValueSpan.Length;
            char[] buffer = ArrayPool<char>.Shared.Rent(valueLength);
            int charsRead = reader.CopyString(buffer);
            ReadOnlySpan<char> text = buffer.AsSpan()[..charsRead];
            int[] values = new int[4];
            int i = 0;
            foreach (var value in text)
            {
                if (char.IsDigit(value))
                {
                    values[i] *= 10;
                    values[i] += value - '0';
                }
                else 
                if (value == ',')
                    i++;
            }
            ArrayPool<char>.Shared.Return(buffer);
            return new Rectangle(values[0], values[1], values[2], values[3]);
        }

        return JsonSerializer.Deserialize<Rectangle>(ref reader);
    }

    public override void Write(Utf8JsonWriter writer, Rectangle value, JsonSerializerOptions options)
        => JsonSerializer.Serialize(writer, value, value.GetType(), options);
}
