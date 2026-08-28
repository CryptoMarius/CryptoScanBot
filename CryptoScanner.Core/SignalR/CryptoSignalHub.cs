using CryptoScanner.Core.Const;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Microsoft.AspNetCore.SignalR;

namespace CryptoScanner.Core.SignalR;

/// <summary>
/// SignalR hub that broadcasts generated crypto signals to connected clients.
/// Clients receive signals on the "ReceiveSignal" method.
/// Clients can invoke GetBarometerGraph(quote, interval) for initial/switch data.
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
    /// Returns the barometer graph history for a given quote and interval.
    /// Call this on connect and when switching between 1h/4h/1d.
    /// </summary>
    public BarometerGraphDto GetBarometerGraph(string quote, string interval)
    {
        var result = new BarometerGraphDto
        {
            Quote = quote,
            Interval = interval,
            // Report the scanner's load state so the UI shows the loading skeleton and only draws the
            // graph once candles are in. Set on every return path.
            Ready = Core.GlobalData.ApplicationStatus == CryptoApplicationStatus.Running,
            Progress = Core.GlobalData.CandleProgressText,
        };

        if (Core.GlobalData.ActiveExchange == null)
            return result;

        if (!Core.GlobalData.IntervalListPeriodName.TryGetValue(interval, out CryptoInterval? cryptoInterval))
            return result;

        string barometerSymbolName = Constants.SymbolNameBarometerPrice + quote;
        if (!Core.GlobalData.ActiveExchange.TryGetSymbolByPair(barometerSymbolName, out CryptoSymbol? symbol))
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
