using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoScanner.Emulator.Engine;

/// <summary>
/// Writes a <see cref="DateTime"/> as a plain "yyyy-MM-dd" date instead of the full ISO timestamp
/// System.Text.Json produces by default. The replay window is picked per whole day (the run dialog
/// stores midnight), so the trailing "T00:00:00Z" in CryptoScanBot-Emulator.json was noise in a file
/// that is meant to be hand-edited.
///
/// Reading stays lenient: both the short form and the older full timestamps are accepted, so
/// existing config files and the ConfigJson of older runs still load. A time that is NOT midnight is
/// kept as-is on both read and write — it does reach the replay window through
/// CandleTime.AlignFromDateTime — so hand-edited values are never silently dropped.
///
/// The value is always returned as UTC. The replay window is aligned in UTC and
/// CandleTime.AlignFromDateTime calls ToUniversalTime(), which would shift a DateTime of kind
/// Unspecified by the local time zone offset (two hours in summer here).
/// </summary>
public class DateOnlyJsonConverter : JsonConverter<DateTime>
{
    private const string DateFormat = "yyyy-MM-dd";


    public override DateTime Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        string? text = reader.GetString();
        if (string.IsNullOrWhiteSpace(text))
            return default;

        if (DateTime.TryParseExact(text, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
            return DateTime.SpecifyKind(date, DateTimeKind.Utc);

        // Older files (and hand-edited ones) hold a full timestamp: "2026-07-29T00:00:00Z" or without
        // the Z. Anything without an explicit offset is read as UTC rather than as local time.
        if (DateTime.TryParse(text, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out DateTime parsed))
            return DateTime.SpecifyKind(parsed, DateTimeKind.Utc);

        return default;
    }


    public override void Write(Utf8JsonWriter writer, DateTime value, JsonSerializerOptions options)
    {
        DateTime utc = value.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(value, DateTimeKind.Utc) : value.ToUniversalTime();

        if (utc.TimeOfDay == TimeSpan.Zero)
            writer.WriteStringValue(utc.ToString(DateFormat, CultureInfo.InvariantCulture));
        else
            writer.WriteStringValue(utc.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture));
    }
}
