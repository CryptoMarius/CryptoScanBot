using CryptoScanner.Core.Core;

using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace CryptoScanner.Core.Exchange.Altrady;

/// <summary>
/// Hands a deep link to the running Altrady desktop application over its local WebSocket.
/// <para>
/// The desktop application listens on 127.0.0.1:6850 (help.altrady.com, "Technical integration",
/// deep links). The protocol is two JSON messages: a <c>ping</c> that must be answered with a
/// <c>pong</c> naming the application (so a stranger on the same port is never handed anything),
/// then a <c>deepLink</c> carrying a route such as <c>#/trade/BINA_USDT_BTC?resolution=60</c>.
/// </para>
/// <para>
/// This replaces the detour the scanner used to need: loading the https address in an invisible
/// browser window so its redirect to the altrady:// protocol would bring the application to the
/// front. That window still exists in both hosts as the fallback for when the socket does not
/// answer, for instance because Altrady is not running or an older version has no socket yet.
/// </para>
/// </summary>
public static class AltradyDeepLink
{
    public const string Host = "127.0.0.1";
    public const int Port = 6850;

    /// <summary>
    /// A connection to the loopback interface either succeeds at once or there is nobody listening,
    /// so there is no reason to keep the click waiting for long before the fallback takes over.
    /// </summary>
    public static readonly TimeSpan Timeout = TimeSpan.FromSeconds(3);

    private const string WebHost = "app.altrady.com";
    private const string ProtocolPrefix = "altrady://";

    /// <summary>
    /// Open the address in Altrady. The socket is tried first, on a worker thread so the click that
    /// caused it is not held up; when the socket does not take the link the <paramref name="fallback"/>
    /// gets the original address (the hidden browser in the hosts). An address that is not an
    /// Altrady deep link goes straight to the fallback.
    /// </summary>
    public static void Open(string url, Action<string> fallback)
    {
        string? route = RouteOf(url);
        if (route == null)
        {
            fallback(url);
            return;
        }

        _ = Task.Run(async () =>
        {
            try
            {
                if (await TrySendAsync(route))
                {
                    GlobalData.AddTextToLogTab($"Altrady: opened {route} via the local WebSocket");
                    return;
                }
            }
            catch (Exception error)
            {
                GlobalData.AddTextToLogTab($"Altrady: the local WebSocket failed for {route}: {error.Message}");
            }

            GlobalData.AddTextToLogTab($"Altrady: the local WebSocket did not take {route}, using the hidden browser instead");
            try
            {
                fallback(url);
            }
            catch (Exception error)
            {
                GlobalData.AddErrorToLogTab($"Altrady: the fallback for {url} failed: {error.Message}");
            }
        });
    }

    /// <summary>
    /// Send the route to the application and tell whether it was accepted. Any failure (nobody
    /// listening, something else on the port, a refused route, a timeout) answers false; the reason
    /// is written to the log tab.
    /// </summary>
    public static async Task<bool> TrySendAsync(string route, int port = Port, CancellationToken cancellationToken = default)
    {
        using ClientWebSocket socket = new();
        using CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(Timeout);

        try
        {
            await socket.ConnectAsync(new Uri($"ws://{Host}:{port}"), cts.Token);

            // Always ping first: the port is a convention, not a guarantee that Altrady owns it
            await SendAsync(socket, "{\"command\":\"ping\"}", cts.Token);
            string pong = await ReceiveAsync(socket, cts.Token);
            if (!IsPong(pong))
            {
                GlobalData.AddTextToLogTab($"Altrady: no pong from {Host}:{port}, got {Shorten(pong)}");
                return false;
            }

            string message = JsonSerializer.Serialize(new { command = "deepLink", deepLink = route });
            await SendAsync(socket, message, cts.Token);
            string answer = await ReceiveAsync(socket, cts.Token);
            bool accepted = IsDeepLinkAccepted(answer);
            if (!accepted)
                GlobalData.AddTextToLogTab($"Altrady: the deep link {route} was not accepted, got {Shorten(answer)}");

            try
            {
                await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cts.Token);
            }
            catch
            {
                // The link has been delivered by now; how the socket goes away does not matter
            }
            return accepted;
        }
        catch (OperationCanceledException)
        {
            GlobalData.AddTextToLogTab($"Altrady: no answer from {Host}:{port} within {Timeout.TotalSeconds:0} seconds");
            return false;
        }
        catch (WebSocketException error)
        {
            // The usual case: Altrady is not running, so nothing listens on the port
            GlobalData.AddTextToLogTab($"Altrady: no WebSocket on {Host}:{port} ({error.Message})");
            return false;
        }
    }

    /// <summary>
    /// The route the socket wants, derived from any of the three link forms Altrady documents, or
    /// null when the address is not an Altrady deep link at all.
    /// <list type="bullet">
    /// <item><c>https://app.altrady.com/d/BINA_USDT_BTC:60</c> (the form in our link templates) becomes <c>#/trade/BINA_USDT_BTC?resolution=60</c></item>
    /// <item><c>https://app.altrady.com/dashboard#/d/BINA_USDT_BTC?resolution=60</c> becomes the same</item>
    /// <item><c>https://app.altrady.com/dashboard#/o/bots</c> becomes <c>#/bots</c></item>
    /// <item><c>altrady://trade/BINA_USDT_BTC?resolution=60</c> and <c>altrady://open/#/trade/...</c> become <c>#/trade/...</c></item>
    /// </list>
    /// </summary>
    public static string? RouteOf(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return null;
        url = url.Trim();

        if (url.StartsWith(ProtocolPrefix, StringComparison.OrdinalIgnoreCase))
        {
            string rest = url[ProtocolPrefix.Length..];
            // altrady://open/#/<route>: everything after the # is the route as-is
            int hash = rest.IndexOf('#');
            if (rest.StartsWith("open/", StringComparison.OrdinalIgnoreCase) && hash >= 0)
                return NormalizeRoute(rest[hash..]);
            return NormalizeRoute("#/" + rest.TrimStart('/'));
        }

        if (!Uri.TryCreate(url, UriKind.Absolute, out Uri? uri))
            return null;
        if (!uri.Host.Equals(WebHost, StringComparison.OrdinalIgnoreCase))
            return null;

        // The fragment keeps its query string, so "#/d/X?resolution=5" arrives in one piece
        string fragment = Uri.UnescapeDataString(uri.Fragment);
        if (fragment.StartsWith("#/d/", StringComparison.OrdinalIgnoreCase))
            return TradeRouteOf(fragment[4..]);
        if (fragment.StartsWith("#/o/", StringComparison.OrdinalIgnoreCase))
            return NormalizeRoute("#/" + fragment[4..]);
        if (fragment.StartsWith("#/", StringComparison.Ordinal))
            return NormalizeRoute(fragment);

        // The older short form without a fragment: /d/<symbol>:<minutes>
        string path = Uri.UnescapeDataString(uri.AbsolutePath);
        if (path.StartsWith("/d/", StringComparison.OrdinalIgnoreCase))
            return TradeRouteOf(path[3..] + uri.Query);

        return null;
    }

    /// <summary>
    /// A market with an optional interval, "BINA_USDT_BTC:60" or "BINA_USDT_BTC?resolution=60" or
    /// just the market, as a trading terminal route.
    /// </summary>
    private static string? TradeRouteOf(string market)
    {
        market = market.Trim('/');
        if (market.Length == 0)
            return null;

        int colon = market.IndexOf(':');
        if (colon > 0)
        {
            string symbol = market[..colon];
            string minutes = market[(colon + 1)..];
            if (minutes.Length == 0)
                return $"#/trade/{symbol}";
            return $"#/trade/{symbol}?resolution={ResolutionOf(minutes)}";
        }
        return $"#/trade/{market}";
    }

    /// <summary>
    /// Our link templates write the interval as a number of minutes. Within a day Altrady reads
    /// those as TradingView resolutions unchanged; a day and longer have letter codes.
    /// </summary>
    internal static string ResolutionOf(string minutes)
    {
        return minutes switch
        {
            "1440" => "D",
            "2880" => "2D",
            "10080" => "W",
            "20160" => "2W",
            "43200" => "1M",
            _ => minutes,
        };
    }

    private static string? NormalizeRoute(string route)
    {
        if (route == "#" || route == "#/")
            return null;
        return route;
    }

    /// <summary>The reply to a ping, when it says Altrady is the one listening.</summary>
    public static bool IsPong(string json)
    {
        return HasField(json, "type", "pong") && HasField(json, "application", "altrady");
    }

    /// <summary>The reply to a deepLink, when the application took it.</summary>
    public static bool IsDeepLinkAccepted(string json)
    {
        return HasField(json, "type", "deepLink") && HasField(json, "result", "ok");
    }

    private static bool HasField(string json, string name, string expected)
    {
        if (string.IsNullOrWhiteSpace(json))
            return false;
        try
        {
            using JsonDocument document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return false;
            if (!document.RootElement.TryGetProperty(name, out JsonElement value))
                return false;
            return value.ValueKind == JsonValueKind.String
                && string.Equals(value.GetString(), expected, StringComparison.OrdinalIgnoreCase);
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static async Task SendAsync(ClientWebSocket socket, string message, CancellationToken cancellationToken)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(message);
        await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken);
    }

    private static async Task<string> ReceiveAsync(ClientWebSocket socket, CancellationToken cancellationToken)
    {
        byte[] buffer = new byte[4096];
        using MemoryStream stream = new();
        while (true)
        {
            WebSocketReceiveResult result = await socket.ReceiveAsync(buffer, cancellationToken);
            if (result.MessageType == WebSocketMessageType.Close)
                return "";
            stream.Write(buffer, 0, result.Count);
            if (result.EndOfMessage)
                break;
        }
        return Encoding.UTF8.GetString(stream.ToArray());
    }

    private static string Shorten(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "(nothing)";
        return text.Length <= 120 ? text : text[..120] + "...";
    }
}
