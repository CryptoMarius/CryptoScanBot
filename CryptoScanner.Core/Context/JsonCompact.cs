using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CryptoScanner.Core.Context;

/// <summary>
/// JSON without the indentation, for the settings and the configuration stored on every emulator
/// run. The indented form was a third whitespace: 39,8 MB against 27,1 MB flat over the 1.436 runs
/// of Session1 (04-10-2026). Flat JSON is the same JSON to every reader, json_extract included.
/// </summary>
public static class JsonCompact
{
    private static readonly JsonWriterOptions WriterOptions = new()
    {
        Indented = false,
        // Keep "+" and accented letters readable instead of + escapes; this text is never
        // embedded in HTML.
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };


    /// <summary>The same JSON on one line, or the input unchanged when it is empty or not valid JSON.</summary>
    public static string? Flatten(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return json;

        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            using MemoryStream buffer = new();
            using (Utf8JsonWriter writer = new(buffer, WriterOptions))
                document.RootElement.WriteTo(writer);
            return Encoding.UTF8.GetString(buffer.ToArray());
        }
        catch (JsonException)
        {
            return json;
        }
    }
}
