using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trader;

using System.Collections.Concurrent;

namespace CryptoScanBot.Core.Core;

public class ThreadMonitorOrder
{
    private readonly CancellationTokenSource cancellationToken = new();
    private readonly BlockingCollection<(CryptoSymbol symbol, CryptoOrderStatus status, CryptoOrder order)> Queue = [];


    public void Stop()
    {
        cancellationToken.Cancel();
        GlobalData.AddTextToLogTab("Stop order monitor");
    }


    public void AddToQueue((CryptoSymbol symbol, CryptoOrderStatus orderStatus, CryptoOrder order) data)
    {
        Queue.Add(data);
    }


    public async Task ExecuteAsync()
    {
        GlobalData.AddTextToLogTab("Task order monitor started");
        foreach ((CryptoSymbol symbol, CryptoOrderStatus orderStatus, CryptoOrder order) in Queue.GetConsumingEnumerable(cancellationToken.Token))
        {
            try
            {
                await TradeHandler.HandleTradeAsync(symbol, orderStatus, order);
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"{symbol.Name} ERROR order monitor thread {error.Message}");
            }
        }
        GlobalData.AddTextToLogTab("Task order monitor stopped");
    }
}
