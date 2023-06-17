using CryptoSbmScanner.Model;

using System.Collections.Concurrent;

namespace CryptoSbmScanner.Intern;

public class ThreadMonitorCandle
{
    private readonly SemaphoreSlim Semaphore = new(3); // X threads tegelijk
    private readonly BlockingCollection<(CryptoSymbol symbol, CryptoCandle candle)> Queue = new();
    private readonly CancellationTokenSource cancellationToken = new();

    public ThreadMonitorCandle()
    {
    }

    public void Stop()
    {
        cancellationToken.Cancel();

        GlobalData.AddTextToLogTab(string.Format("Stop create signals"));
    }

    public void AddToQueue(CryptoSymbol symbol, CryptoCandle candle)
    {
        Queue.Add((symbol, candle));
    }


    public void Execute()
    {
        foreach ((CryptoSymbol symbol, CryptoCandle candle) in Queue.GetConsumingEnumerable(cancellationToken.Token))
        {
            new Thread(async () =>
            {
                // Er is een 1m candle gearriveerd, acties adhv deze candle..

                await Semaphore.WaitAsync();
                try
                {
                    PositionMonitor positionMonitor = new(symbol, candle);
                    await positionMonitor.NewCandleArrived();
                }
                finally
                {
                    Semaphore.Release();
                }
            }
            ).Start();
        }

        GlobalData.AddTextToLogTab("\r\n" + "\r\n MONITOR candle THREAD EXIT");
    }
}
