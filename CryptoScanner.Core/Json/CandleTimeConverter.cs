using CryptoScanner.Core.Model;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Json;

public class CandleTimeConverter : JsonConverter<CandleTime>
{
    public override CandleTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            return new CandleTime(reader.GetUInt32());

        throw new JsonException($"Cannot convert token '{reader.TokenType}' to CandleTime; expected a number.");
    }

    public override void Write(Utf8JsonWriter writer, CandleTime value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value.Minutes);
    }
}
