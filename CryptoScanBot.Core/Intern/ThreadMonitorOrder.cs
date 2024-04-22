using System.Collections.Concurrent;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trader;

namespace CryptoScanBot.Core.Intern;

#if TRADEBOT
public class ThreadMonitorOrder
{
    private readonly CancellationTokenSource cancellationToken = new();
    private readonly BlockingCollection<(CryptoSymbol symbol, CryptoOrderType ordertype, CryptoOrderSide side, CryptoOrderStatus status, CryptoOrder order)> Queue = [];


    public void Stop()
    {
        cancellationToken.Cancel();
        GlobalData.AddTextToLogTab("Stop order monitor");
    }


    public void AddToQueue((CryptoSymbol symbol, CryptoOrderType orderType, CryptoOrderSide orderSide, CryptoOrderStatus orderStatus, CryptoOrder order) data)
    {
        Queue.Add(data);
    }


    public async Task ExecuteAsync()
    {
        GlobalData.AddTextToLogTab("Task order monitor started");
        foreach ((CryptoSymbol symbol, CryptoOrderType orderType, CryptoOrderSide orderSide, CryptoOrderStatus orderStatus, CryptoOrder order) in Queue.GetConsumingEnumerable(cancellationToken.Token))
        {
            try
            {
                await TradeHandler.HandleTradeAsync(symbol, orderType, orderSide, orderStatus, order);
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
#endif
