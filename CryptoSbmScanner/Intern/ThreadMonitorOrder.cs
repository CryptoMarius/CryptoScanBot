using System.Collections.Concurrent;

using CryptoSbmScanner.Enums;
using CryptoSbmScanner.Model;

namespace CryptoSbmScanner.Intern;

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

        GlobalData.AddTextToLogTab(string.Format("Stop order handler"));
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
                // Dit is reeds gecontroleerd, overbodig
                // Wij zijn slechts geinteresseerd in een paar statussen (de andere zijn niet interessant voor de afhandeling van de order) 
                //if (data.orderStatus == CryptoOrderStatus.Filled || 
                //    data.orderStatus == CryptoOrderStatus.PartiallyFilled || 
                //    data.orderStatus == CryptoOrderStatus.Canceled)
                //{
                await PositionMonitor.HandleTradeAsync(data.symbol, data.orderType, data.orderSide, data.orderStatus, data.trade);
                //}
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab("\r\n" + "\r\n" + data.symbol.Name + " error order handler thread\r\n" + error.ToString());
            }
        }
        GlobalData.AddTextToLogTab("\r\n" + "\r\n MONITOR order THREAD EXIT");
    }
}

