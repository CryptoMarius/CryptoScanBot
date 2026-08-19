using System.Globalization;
using System.Text.Json;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot.Socket;

/// <summary>
/// One candle as Bitvavo sends it over the WEBSOCKET, read out of the raw json array.
/// Named apart from the BitvavoCandle of BitvavoRestClient: that one is the rest model and lives in
/// the parent namespace, so a plain "BitvavoCandle" here silently binds to the wrong type.
/// <para>
/// Kept apart from the message model so the reading of that six element array
/// (<c>[timestamp_ms, "open", "high", "low", "close", "volume"]</c>) lives in exactly one place. The
/// mixed types are the reason: element zero is a number and the rest are strings, and a wrong index
/// here would silently produce a candle instead of an error.
/// </para>
/// </summary>
internal readonly struct BitvavoSocketCandle
{
    public DateTime OpenTimeUtc { get; init; }
    public decimal Open { get; init; }
    public decimal High { get; init; }
    public decimal Low { get; init; }
    public decimal Close { get; init; }

    /// <summary>
    /// The volume as Bitvavo reports it: in the BASE asset. See <see cref="QuoteVolume"/>.
    /// </summary>
    public decimal BaseVolume { get; init; }

    /// <summary>
    /// The scanner works in quote volume everywhere (the 24 hour figure of a symbol comes from
    /// volumeQuote), so the base volume has to be converted. The value is cumulative over the open
    /// candle, and multiplying it by the middle of that same candle keeps it growing monotonically -
    /// which is what the Math.Max in the ticker cache expects. Same conversion as Kraken Futures.
    /// </summary>
    public decimal QuoteVolume => BaseVolume * 0.5m * (High + Low);

    /// <summary>
    /// Read one candle array, or null when it does not hold the six values it should.
    /// </summary>
    public static BitvavoSocketCandle? From(JsonElement[]? values)
    {
        if (values == null || values.Length < 6)
            return null;

        try
        {
            return new BitvavoSocketCandle
            {
                OpenTimeUtc = DateTimeOffset.FromUnixTimeMilliseconds(values[0].GetInt64()).UtcDateTime,
                Open = ParseNumber(values[1]),
                High = ParseNumber(values[2]),
                Low = ParseNumber(values[3]),
                Close = ParseNumber(values[4]),
                BaseVolume = ParseNumber(values[5]),
            };
        }
        catch (Exception)
        {
            // A malformed candle is dropped rather than allowed to take down the receive loop of the
            // whole group. The flush timer repeats the previous candle, so one lost message costs a
            // minute of one symbol at worst.
            return null;
        }
    }

    /// <summary>
    /// The prices arrive as strings ("0.00001234"), but accept a json number as well so a change on
    /// their side does not silently produce zeros. Invariant culture: the decimal point is a point.
    /// </summary>
    private static decimal ParseNumber(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number)
            return element.GetDecimal();

        return decimal.Parse(element.GetString()!, NumberStyles.Float, CultureInfo.InvariantCulture);
    }
}
