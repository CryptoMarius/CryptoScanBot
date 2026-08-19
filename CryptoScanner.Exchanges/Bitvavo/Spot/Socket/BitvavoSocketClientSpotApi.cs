using CryptoExchange.Net.Clients;
using CryptoExchange.Net.Converters.MessageParsing.DynamicConverters;
using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Converters.SystemTextJson;
using CryptoExchange.Net.Interfaces;
using CryptoExchange.Net.Objects.Options;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.SharedApis;

using Microsoft.Extensions.Logging;

using System.Net.WebSockets;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot.Socket;

/// <summary>
/// The Bitvavo websocket api client, built on CryptoExchange.Net instead of on a hand written
/// ClientWebSocket.
/// <para>
/// WHY, because it is a fair question when a working socket gets replaced: everything the other
/// nineteen markets get for free was missing here. A keep alive that also NOTICES a missing answer
/// (ClientWebSocketOptions.KeepAliveTimeout does not exist before .NET 9, but the library brings its
/// own), reconnecting with a policy, resubscribing after a reconnect, and the events
/// ConnectionLost / ConnectionRestored / ResubscribingFailed that Exchange/Subscription.cs already
/// handles for every other exchange. Without them a half open Bitvavo socket was only noticed by our
/// own silence check, five minutes later, and every repair to the shared path had to be redone here
/// by hand.
/// </para>
/// <para>
/// Deliberately built on <see cref="SocketApiClient{TEnvironment}"/>, the variant WITHOUT credentials
/// and authentication provider: the scanner only reads public candles from Bitvavo. That leaves two
/// members to implement instead of a full exchange library.
/// </para>
/// </summary>
public class BitvavoSocketClientSpotApi : SocketApiClient<BitvavoEnvironment>
{
    public BitvavoSocketClientSpotApi(ILoggerFactory? loggerFactory, SocketExchangeOptions<BitvavoEnvironment> options)
        : base(loggerFactory, "Bitvavo", options.Environment.SocketAddress, options, new SocketApiOptions())
    {
    }

    /// <summary>
    /// Convenience constructor with the defaults this scanner uses. The keep alive is left to the
    /// library (10 seconds, and it aborts the socket when the answer does not arrive), and
    /// SocketNoDataTimeout is deliberately NOT set: on a candle feed that only pushes when something
    /// is traded it measures silence instead of trouble - see the remarks in the other Api.cs files.
    /// </summary>
    public BitvavoSocketClientSpotApi() : this(null, new SocketExchangeOptions<BitvavoEnvironment>
    {
        Environment = BitvavoEnvironment.Live,
        ReconnectInterval = TimeSpan.FromSeconds(10),
    })
    {
    }

    /// <summary>
    /// Bitvavo names a market as BASE-QUOTE ("BTC-EUR"), which is why the scanner keeps an exchange
    /// name per symbol next to its own name.
    /// </summary>
    public override string FormatSymbol(string baseAsset, string quoteAsset, TradingMode tradingMode, DateTime? deliverDate = null)
        => $"{baseAsset.ToUpperInvariant()}-{quoteAsset.ToUpperInvariant()}";

    public override ISocketMessageHandler CreateMessageConverter(WebSocketMessageType messageType)
        => new BitvavoSocketMessageConverter();

    /// <summary>
    /// Same options as the message converter uses, so what is read and what is written agree.
    /// </summary>
    protected override IMessageSerializer CreateSerializer()
        => new SystemTextJsonMessageSerializer(new System.Text.Json.JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
        });

    /// <summary>
    /// Subscribe to the candles of a group of markets. The market names are the EXCHANGE names
    /// ("BTC-EUR"), not the scanner names - the caller translates.
    /// </summary>
    internal Task<WebSocketResult<UpdateSubscription>> SubscribeToKlineUpdatesAsync(
        IEnumerable<string> markets, string interval,
        Action<DataEvent<BitvavoCandleUpdate>> onMessage, CancellationToken ct = default)
    {
        var subscription = new BitvavoCandleSubscription(_logger, interval, markets.ToArray(), onMessage);
        return SubscribeAsync(BaseAddress, subscription, ct);
    }
}
