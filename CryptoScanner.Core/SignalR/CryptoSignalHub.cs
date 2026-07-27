using CryptoScanner.Core.Const;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Microsoft.AspNetCore.SignalR;

namespace CryptoScanner.Core.SignalR;

/// <summary>
/// SignalR hub that broadcasts generated crypto signals to connected clients.
/// Clients receive signals on the "ReceiveSignal" method.
/// Clients can invoke GetBarometerGraph(quote, interval) for initial/switch data.
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
    /// Returns the barometer graph history for a given quote and interval.
    /// Call this on connect and when switching between 1h/4h/1d.
    /// </summary>
    public BarometerGraphDto GetBarometerGraph(string quote, string interval)
    {
        var result = new BarometerGraphDto { Quote = quote, Interval = interval };

        if (Core.GlobalData.ActiveExchange == null)
            return result;

        if (!Core.GlobalData.IntervalListPeriodName.TryGetValue(interval, out CryptoInterval? cryptoInterval))
            return result;

        string barometerSymbolName = Constants.SymbolNameBarometerPrice + quote;
        if (!Core.GlobalData.ActiveExchange.SymbolListName.TryGetValue(barometerSymbolName, out CryptoSymbol? symbol))
            return result;

        var symbolInterval = symbol.GetSymbolInterval(cryptoInterval.IntervalPeriod);
        int maxPoints = Constants.BarometerGraphHours * 60;

        var candles = symbolInterval.CandleList.GetLastNValues(maxPoints, 1);
        foreach (var candle in candles)
        {
            result.Points.Add(new BarometerPointDto
            {
                Time = candle.OpenTime.ToDateTime(),
                Value = candle.Close,
            });
        }

        return result;
    }
}
