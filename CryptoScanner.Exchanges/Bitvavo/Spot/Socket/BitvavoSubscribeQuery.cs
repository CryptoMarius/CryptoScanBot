using CryptoExchange.Net.Objects;
using CryptoExchange.Net.Sockets;
using CryptoExchange.Net.Sockets.Default;
using CryptoExchange.Net.Sockets.Default.Routing;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot.Socket;

/// <summary>
/// Sends one subscribe or unsubscribe message and waits for the answer. Bitvavo confirms with
/// <c>{"event":"subscribed"}</c> (and the same for unsubscribed), so that is what this waits on.
/// <para>
/// Having it as a Query rather than a fire-and-forget send is the whole point of the exercise: the
/// library now knows whether the subscription actually landed, retries it after a reconnect, and can
/// report failure through ResubscribingFailed instead of the group silently receiving nothing.
/// </para>
/// </summary>
internal class BitvavoSubscribeQuery : Query<BitvavoSubscriptionResponse>
{
    public BitvavoSubscribeQuery(object request, string expectedEvent) : base(request, false, 1)
    {
        MessageRouter = MessageRouter.CreateForQuery<BitvavoSubscriptionResponse>(expectedEvent, HandleMessage);
    }

    public CallResult<BitvavoSubscriptionResponse> HandleMessage(SocketConnection connection,
        DateTime receiveTime, string? originalData, BitvavoSubscriptionResponse message)
    {
        return CallResult<BitvavoSubscriptionResponse>.Ok(message, originalData);
    }
}
