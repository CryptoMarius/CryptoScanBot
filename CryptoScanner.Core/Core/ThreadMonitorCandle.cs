using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using System.Collections.Concurrent;

namespace CryptoScanner.Core.Core;

public class ThreadMonitorCandle
{
    private readonly SemaphoreSlim Semaphore = new(4); // X threads tegelijk
    private readonly BlockingCollection<(CryptoSymbol symbol, CryptoCandle candle)> Queue = [];
    private readonly CancellationTokenSource cancellationToken = new();


    public void Stop()
    {
        cancellationToken.Cancel();
        //GlobalData.AddTextToLogTab(string.Format("Stop monitor candle"));
    }


    public void AddToQueue(CryptoSymbol symbol, CryptoCandle candle)
    {
        if (!GlobalData.IsEmulatorMode && GlobalData.ApplicationStatus == CryptoApplicationStatus.Running)
            Queue.Add((symbol, candle));
    }


    public void Execute()
    {
        //GlobalData.AddTextToLogTab("Starting task for creating signals");
        try
        {
            foreach ((CryptoSymbol symbol, CryptoCandle candle) in Queue.GetConsumingEnumerable(cancellationToken.Token))
            {
                Task.Run(async () =>
                {
                    await Semaphore.WaitAsync();
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
                ).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // niets..
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"ThreadMonitorCandle ERROR {error.Message}");
        }

        GlobalData.AddTextToLogTab("ThreadMonitorCandle candle thread exit");
    }
}
