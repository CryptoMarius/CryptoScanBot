using CryptoScanner.Core.Model;

using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Json;

public class CandleTimeConverter : JsonConverter<CandleTime>
{
    // Value serialization (e.g. "OpenTime": 7652580)
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

    // Dictionary-key serialization (e.g. { "7652580": { ... } })
    public override CandleTime ReadAsPropertyName(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (uint.TryParse(reader.GetString(), out uint minutes))
            return new CandleTime(minutes);

        throw new JsonException($"Cannot convert property name '{reader.GetString()}' to CandleTime; expected a uint string.");
    }

    public override void WriteAsPropertyName(Utf8JsonWriter writer, CandleTime value, JsonSerializerOptions options)
    {
        writer.WritePropertyName(value.Minutes.ToString());
    }
}
