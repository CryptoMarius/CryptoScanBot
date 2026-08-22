using CryptoExchange.Net.Clients;
using CryptoExchange.Net.Objects.Options;

using Microsoft.Extensions.Logging;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot.Socket;

/// <summary>
/// The outer client, the equivalent of BinanceSocketClient or BitMartSocketClient. It owns
/// <see cref="SpotApi"/> and exists mainly so Bitvavo fits the same shape as every other exchange:
/// <see cref="SubscriptionBundle.SocketClient"/> holds a <see cref="BaseSocketClient"/>, and the
/// bundle disposes it. Without this wrapper Bitvavo would still be the exception in the layout, which
/// is exactly what this whole change is meant to end.
/// </summary>
public class BitvavoSocketClient : BaseSocketClient
{
    /// <summary>
    /// Named SpotApi to match the other exchanges, even though Bitvavo has only a spot market.
    /// </summary>
    public BitvavoSocketClientSpotApi SpotApi { get; }

    public BitvavoSocketClient(ILoggerFactory? loggerFactory = null) : base(loggerFactory, "Bitvavo")
    {
        var options = new SocketExchangeOptions<BitvavoEnvironment>
        {
            Environment = BitvavoEnvironment.Live,

            // Same as the other exchanges use. SocketNoDataTimeout is deliberately left at its default
            // of zero: on a candle feed that only pushes when something is traded it measures inactivity
            // instead of trouble, and the keep alive of the library (10 seconds, and it aborts when the
            // answer stays away) is the one that really tells whether the connection is alive.
            ReconnectInterval = TimeSpan.FromSeconds(10),
            RequestTimeout = TimeSpan.FromSeconds(40),
        };

        Initialize(options);
        SpotApi = AddApiClient(new BitvavoSocketClientSpotApi(loggerFactory, options));
    }
}
