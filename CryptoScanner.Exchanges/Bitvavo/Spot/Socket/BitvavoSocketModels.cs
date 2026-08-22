using System.Text.Json;
using System.Text.Json.Serialization;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot.Socket;

/// <summary>
/// The messages of the Bitvavo websocket that this scanner cares about. Bitvavo has no
/// CryptoExchange.Net package of its own, so these models and the handful of classes next to them are
/// what a package like Binance.Net or BitMart.Net would otherwise provide.
/// <para>
/// Only the candle channel is modelled. The scanner subscribes to nothing else there, and every type
/// added here also has to be routed in <see cref="BitvavoSocketMessageConverter"/>, so keeping it to
/// what is actually used keeps the routing honest.
/// </para>
/// </summary>
internal class BitvavoCandleUpdate
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;

    [JsonPropertyName("market")]
    public string Market { get; set; } = string.Empty;

    [JsonPropertyName("interval")]
    public string Interval { get; set; } = string.Empty;

    /// <summary>
    /// Bitvavo sends an ARRAY of candles, each one an array of six mixed values:
    /// <c>[[timestamp_ms, "open", "high", "low", "close", "volume"]]</c> - the timestamp is a number,
    /// the rest are strings. There is no clean model for that, so it stays as raw json elements and
    /// <see cref="BitvavoCandle.From"/> does the reading in one place.
    /// </summary>
    [JsonPropertyName("candle")]
    public JsonElement[][] Candle { get; set; } = [];
}


/// <summary>
/// Answer to a subscribe or unsubscribe request: <c>{"event":"subscribed","subscriptions":{...}}</c>.
/// The contents of "subscriptions" are not read - the arrival of the message is the confirmation.
/// </summary>
internal class BitvavoSubscriptionResponse
{
    [JsonPropertyName("event")]
    public string Event { get; set; } = string.Empty;
}


/// <summary>
/// A rejected request: <c>{"action":"subscribe","errorCode":205,"error":"..."}</c>, without an event
/// field. Worth its own model because ONE invalid market rejects the WHOLE subscribe message, and
/// then the group receives nothing at all - previously the only symptom was the inactivity check
/// restarting it into the same rejection every few minutes.
/// </summary>
internal class BitvavoErrorResponse
{
    [JsonPropertyName("action")]
    public string? Action { get; set; }

    [JsonPropertyName("errorCode")]
    public int ErrorCode { get; set; }

    [JsonPropertyName("error")]
    public string? Error { get; set; }
}
