using CryptoExchange.Net.Objects;

namespace CryptoScanner.Core.Exchange.Bitvavo.Spot.Socket;

/// <summary>
/// Where the Bitvavo websocket lives. CryptoExchange.Net wants an environment per exchange so the
/// address can be swapped for a test net; Bitvavo has no test net, so there is one.
/// </summary>
public class BitvavoEnvironment : TradeEnvironment
{
    public string SocketAddress { get; }

    internal BitvavoEnvironment(string name, string socketAddress) : base(name)
    {
        SocketAddress = socketAddress;
    }

    public static BitvavoEnvironment Live { get; } = new("Live", "wss://ws.bitvavo.com/v2/");
}
