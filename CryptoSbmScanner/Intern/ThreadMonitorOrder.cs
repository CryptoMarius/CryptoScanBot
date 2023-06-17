using System.Collections.Concurrent;
using Binance.Net.Objects.Models.Spot.Socket;
using CryptoSbmScanner.Model;


namespace CryptoSbmScanner.Intern;

public class ThreadMonitorOrder
{
    private readonly CancellationTokenSource cancellationToken = new();
    private readonly BlockingCollection<BinanceStreamOrderUpdate> Queue = new();

    public ThreadMonitorOrder()
    {
    }

    public void Stop()
    {
        cancellationToken.Cancel();

        GlobalData.AddTextToLogTab(string.Format("Stop order handler"));
    }

    public void AddToQueue(BinanceStreamOrderUpdate data)
    {
        Queue.Add(data);
    }

    public async Task ExecuteAsync()
    {
        // Een aparte queue die orders afhandeld (met als achterliggende reden de juiste afhandel volgorde)
        foreach (BinanceStreamOrderUpdate data in Queue.GetConsumingEnumerable(cancellationToken.Token))
        {
            try
            {
                // Nieuwe thread opstarten en de data meegeven zodat er een sell wordt gedaan of administratie wordt bijgewerkt.
                // Het triggeren van een stoploss of een DCA zal op een andere manier gedaan moeten worden (maar hoe en waar?)
                if (GlobalData.ExchangeListName.TryGetValue("Binance", out Model.CryptoExchange exchange))
                {
                    if (exchange.SymbolListName.TryGetValue(data.Symbol, out CryptoSymbol symbol))
                    {
                        // Puzzel, welk trading account hoort erbij (als je meerdere hebt)
                        await PositionMonitor.HandleTradeAsync(GlobalData.BinanceRealTradeAccount, symbol, data);
                    }
                }

            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab("\r\n" + "\r\n" + data.Symbol + " error order handler thread\r\n" + error.ToString());
            }
        }
        GlobalData.AddTextToLogTab("\r\n" + "\r\n MONITOR order THREAD EXIT");
    }
}

