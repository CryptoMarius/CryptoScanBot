using System.Collections.Concurrent;

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;
using CryptoSbmScanner.Trader;

namespace CryptoSbmScanner.Intern;

#if TRADEBOT
public class ThreadMonitorOrder
{
    private readonly CancellationTokenSource cancellationToken = new();
    private readonly BlockingCollection<(CryptoSymbol symbol, CryptoOrderType ordertype, CryptoOrderSide side, CryptoOrderStatus status, CryptoTrade trade)> Queue = new();

     

    public ThreadMonitorOrder()
    {
    }

    public void Stop()
    {
        cancellationToken.Cancel();

        GlobalData.AddTextToLogTab("Stop order handler");
    }

    public void AddToQueue((CryptoSymbol symbol, CryptoOrderType orderType, CryptoOrderSide orderSide, CryptoOrderStatus orderStatus, CryptoTrade trade) data)
    {
        Queue.Add(data);
    }

    public async Task ExecuteAsync()
    {
        // Een aparte queue die orders afhandeld (met als achterliggende reden de juiste afhandel volgorde)
        foreach ((CryptoSymbol symbol, CryptoOrderType orderType, CryptoOrderSide orderSide, CryptoOrderStatus orderStatus, CryptoTrade trade) data in Queue.GetConsumingEnumerable(cancellationToken.Token))
        {
            try
            {
                await TradeHandler.HandleTradeAsync(data.symbol, data.orderType, data.orderSide, data.orderStatus, data.trade);
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab($"{data.symbol.Name} ERROR order handler thread {error.Message}");
            }
        }
        GlobalData.AddTextToLogTab("\r\n" + "\r\n MONITOR order THREAD EXIT");
    }
}
#endif
