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
