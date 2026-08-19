using CryptoExchange.Net.Converters.MessageParsing.DynamicConverters;
using CryptoExchange.Net.Converters.SystemTextJson.MessageHandlers;

using System.Net.WebSockets;
using System.Text.Json;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot.Socket;

/// <summary>
/// Decides what an incoming Bitvavo message IS, so the library can route it to the right subscription
/// or query. This is the piece that makes the rest work, and the piece to look at first when nothing
/// arrives: get the identifier wrong and every message lands in the void, which shows up as a group
/// that delivers no candles at all.
/// <para>
/// Bitvavo has three shapes and they are told apart by which field is present:
/// <list type="bullet">
/// <item><c>{"event":"candle", "market":"BTC-EUR", ...}</c> - identifier "candle", topic the market</item>
/// <item><c>{"event":"subscribed", "subscriptions":{...}}</c> - identifier "subscribed", the answer the subscribe query waits for</item>
/// <item><c>{"action":"subscribe", "errorCode":205, "error":"..."}</c> - no event field at all, identifier "error"</item>
/// </list>
/// </para>
/// </summary>
internal class BitvavoSocketMessageConverter : JsonSocketMessageHandler
{
    /// <summary>
    /// Bitvavo names its fields in lower case ("event", "market"), the models use the usual C#
    /// casing, and the JsonPropertyName attributes bridge that. Case insensitive on top of it so a
    /// change in their casing cannot silently produce empty fields.
    /// </summary>
    public override JsonSerializerOptions Options { get; } = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    /// <summary>
    /// Evaluated in order, first match wins. The error shape has to come FIRST: a rejection carries no
    /// event field, so leaving it to the event rule below would leave it unidentified and unlogged,
    /// and that is exactly the failure that used to be invisible.
    /// <para>
    /// Assigned as a property initializer on purpose - those run before the base constructor, which is
    /// where the evaluators are picked up.
    /// </para>
    /// </summary>
    protected override MessageTypeDefinition[] TypeEvaluators { get; } =
    [
        new MessageTypeDefinition
        {
            Fields = [new PropertyFieldReference("errorCode")],
            TypeIdentifierCallback = (SearchResult _) => "error",
        },
        new MessageTypeDefinition
        {
            Fields = [new PropertyFieldReference("event")],
            TypeIdentifierCallback = (SearchResult x) => x.FieldValue("event") ?? string.Empty,
        },
    ];

    public BitvavoSocketMessageConverter()
    {
        // Which subscription a candle belongs to is decided by the market, because one connection
        // carries the candles of every market in its group.
        AddTopicMapping<BitvavoCandleUpdate>(x => x.Market);
    }

    /// <summary>
    /// Bitvavo sends nothing that is not json (no binary frames, no bare "pong"), so there is nothing
    /// to identify here.
    /// </summary>
    protected override string? GetTypeIdentifierNonJson(ReadOnlySpan<byte> data, WebSocketMessageType? webSocketMessageType) => null;
}
