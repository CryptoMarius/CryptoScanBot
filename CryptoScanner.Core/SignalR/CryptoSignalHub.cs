using CryptoScanner.Core.Const;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Microsoft.AspNetCore.SignalR;

namespace CryptoScanner.Core.SignalR;

/// <summary>
/// SignalR hub that broadcasts generated crypto signals to connected clients.
/// Clients receive signals on the "ReceiveSignal" method.
/// Clients can invoke GetBarometerValues(quote) for the 1h/4h/1d summary of their own quote.
/// </summary>
public class CryptoSignalHub : Hub
{
    public override async Task OnConnectedAsync()
    {
        Core.GlobalData.AddTextToLogTab($"SignalR client connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        Core.GlobalData.AddTextToLogTab($"SignalR client disconnected: {Context.ConnectionId}");
        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Returns the barometer summary values (1h/4h/1d) for a given quote.
    /// The dashboard push carries the quote selected in the desktop app; a remote client that lets the
    /// user pick their own quote can call this to get the values for that quote instead.
    /// Call this on connect and whenever the client switches quote.
    /// </summary>
    public BarometerValuesDto GetBarometerValues(string quote)
    {
        return DashboardDataCollector.GetBarometerValues(quote);
    }
}
