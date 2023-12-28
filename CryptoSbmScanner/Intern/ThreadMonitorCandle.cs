using CryptoSbmScanner.Model;

using System.Collections.Concurrent;

namespace CryptoSbmScanner.Intern;

public class ThreadMonitorCandle
{
    private readonly SemaphoreSlim Semaphore = new(5); // X threads tegelijk
    private readonly BlockingCollection<(CryptoSymbol symbol, CryptoCandle candle)> Queue = new();
    private readonly CancellationTokenSource cancellationToken = new();


    public ThreadMonitorCandle()
    {
    }


    public void Stop()
    {
        cancellationToken.Cancel();

        GlobalData.AddTextToLogTab(string.Format("Stop monitor candle"));
    }


    public void AddToQueue(CryptoSymbol symbol, CryptoCandle candle)
    {
        Queue.Add((symbol, candle));
    }


    public async void Execute()
    {
        try
        {
            foreach ((CryptoSymbol symbol, CryptoCandle candle) in Queue.GetConsumingEnumerable(cancellationToken.Token))
            {
                await Semaphore.WaitAsync();

                new Thread(async () =>
                {
                    try
                    {
                        // Er is een 1m candle gearriveerd, acties adhv deze candle..
                        PositionMonitor positionMonitor = new(symbol, candle);
                        await positionMonitor.NewCandleArrivedAsync();
                    }
                    finally
                    {
                        Semaphore.Release();
                    }
                }
                ).Start();
            }
        }
        catch (OperationCanceledException)
        {
            // niets..
        }
        catch (Exception error) 
        {
            GlobalData.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"ERROR monitor candle {error.Message}");
        }

        GlobalData.AddTextToLogTab("\r\n" + "\r\n MONITOR candle THREAD EXIT");
    }
}
