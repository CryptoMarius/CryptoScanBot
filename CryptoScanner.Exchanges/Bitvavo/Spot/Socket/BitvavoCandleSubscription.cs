using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Objects.Sockets;
using CryptoExchange.Net.Sockets;
using CryptoExchange.Net.Sockets.Default;
using CryptoExchange.Net.Sockets.Default.Routing;

using Microsoft.Extensions.Logging;
// De scanner heeft zelf een Exchange.Subscription, en dit bestand staat in een namespace daaronder,
// dus zonder alias erft deze klasse van de verkeerde. Dat kost een compileerfout over Subscribe(),
// wat lastig te plaatsen is als je de oorzaak niet kent - vandaar deze regel.
using LibrarySubscription = CryptoExchange.Net.Sockets.Default.Subscription;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot.Socket;

/// <summary>
/// One subscription to the Bitvavo candle channel, for a group of markets on one interval.
/// <para>
/// The library owns the lifecycle from here on: it sends the subscribe message through
/// <see cref="GetSubQuery"/>, sends it AGAIN by itself after a reconnect, and reports through the
/// events on UpdateSubscription that the rest of the scanner already listens to
/// (see Exchange/Subscription.cs). That is what the hand written socket did not have.
/// </para>
/// </summary>
internal class BitvavoCandleSubscription : LibrarySubscription
{
    private readonly string _interval;
    private readonly string[] _markets;
    private readonly Action<DataEvent<BitvavoCandleUpdate>> _handler;

    public BitvavoCandleSubscription(ILogger logger, string interval, string[] markets,
        Action<DataEvent<BitvavoCandleUpdate>> handler) : base(logger, false)
    {
        _interval = interval;
        _markets = markets;
        _handler = handler;

        // One subscription serves several markets, and the library counts them so its own limits and
        // its state overview match what is really on the connection.
        IndividualSubscriptionCount = markets.Length;

        // Route on the event name AND the market: a candle for a market that belongs to another group
        // on the same connection must not end up here.
        MessageRouter = MessageRouter.CreateForEvent<BitvavoCandleUpdate>("candle", markets, DoHandleMessage);
    }

    protected override Query? GetSubQuery(SocketConnection connection) => BuildQuery("subscribe", "subscribed");

    protected override Query? GetUnsubQuery(SocketConnection connection) => BuildQuery("unsubscribe", "unsubscribed");

    /// <summary>
    /// The message Bitvavo expects:
    /// <c>{"action":"subscribe","channels":[{"name":"candles","interval":["1m"],"markets":["BTC-EUR"]}]}</c>
    /// </summary>
    private BitvavoSubscribeQuery BuildQuery(string action, string expectedEvent)
    {
        var request = new
        {
            action,
            channels = new[]
            {
                new { name = "candles", interval = new[] { _interval }, markets = _markets }
            }
        };
        return new BitvavoSubscribeQuery(request, expectedEvent);
    }

    public CallResult DoHandleMessage(SocketConnection connection, DateTime receiveTime,
        string? originalData, BitvavoCandleUpdate message)
    {
        _handler(new DataEvent<BitvavoCandleUpdate>(connection.ApiClient.Exchange, message, receiveTime, originalData)
            .WithSymbol(message.Market));
        return CallResult.Ok();
    }
}
