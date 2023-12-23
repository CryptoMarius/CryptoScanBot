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
                // Het gaat te snel, de trade is blijkbaar nog niet in het systeem verwerkt! (ByBit, 31-10-2023)
                // De optie herberekenen krijgt de order wel binnen
                Thread.Sleep(2500);

                //string text = JsonSerializer.Serialize(data, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = false }).Trim();
                //GlobalData.AddTextToLogTab(string.Format("{0} ThreadMonitorOrder#1 TradeId={1} {2} quantity={3} price={4} text={5}", data.symbol, 
                //    data.trade.OrderId, data.trade.Side, data.trade.Quantity, data.trade.Price, text));
                await TradeHandler.HandleTradeAsync(data.symbol, data.orderType, data.orderSide, data.orderStatus, data.trade);
            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error, "");
                GlobalData.AddTextToLogTab($"{data.symbol.Name} ERROR order handler thread {error.Message}");
            }
        }
        GlobalData.AddTextToLogTab("\r\n" + "\r\n MONITOR order THREAD EXIT");
    }
}
#endif
