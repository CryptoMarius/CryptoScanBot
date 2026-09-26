using CryptoExchange.Net.Clients;

namespace CryptoScanner.Core.Exchange;

/// <summary>
/// One socket client with the subscriptions that run over it. The exchange library manages the actual
/// websocket connection(s) underneath, so unsubscribing one subscription leaves the others untouched.
/// </summary>
public class SubscriptionBundle : IDisposable
{
    // Iedere client bedient maximaal 10 subscriptions
    // Iedere subscription bedient een aantal symbols
    // dat is zo'n 1..200 en afhankelijk van de exchange..
    public BaseSocketClient? SocketClient; // made public for ExchangeTest project
    public List<Subscription> SubscriptionList { get; set; } = [];

    private readonly object _socketClientLock = new();

    /// <summary>
    /// The socket client of this bundle, created with <paramref name="factory"/> the first time it
    /// is asked for.
    /// <para>
    /// Every subscription used to do <c>SocketClient ??= new ...</c> itself. Two subscriptions of
    /// the same bundle start 32 to 250 milliseconds apart in their own tasks (see
    /// SubscriptionManager.StartStaggeredAsync), and on a saturated threadpool both can read null
    /// and both create a client. The bundle keeps the last one, the first is never disposed and its
    /// connection stays open until its own subscriptions close. Unsubscribing still worked, because
    /// UnsubscribeAsync goes through the UpdateSubscription itself and not through the client it is
    /// called on - which is exactly why nobody noticed. Alpaca already did this behind a lock.
    /// </para>
    /// </summary>
    public T GetOrCreateSocketClient<T>(Func<T> factory) where T : BaseSocketClient
    {
        lock (_socketClientLock)
        {
            SocketClient ??= factory();
            return (T)SocketClient;
        }
    }


    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            if (SocketClient != null)
            {
                SocketClient.Dispose();
                SocketClient = null;
            }
        }
    }
}
