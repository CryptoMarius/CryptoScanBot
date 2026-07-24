using Microsoft.AspNetCore.SignalR;

namespace CryptoScanner.Core.SignalR;

/// <summary>
/// SignalR hub that broadcasts generated crypto signals to connected clients.
/// Clients receive signals on the "ReceiveSignal" method.
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
}
