using CryptoSbmScanner.Model;

using System.Collections.Concurrent;

namespace CryptoSbmScanner.Intern;

public class ThreadMonitorCandle
{
    //public Thread Thread;

    private readonly SemaphoreSlim Semaphore = new(3); // X threads tegelijk
    private readonly BlockingCollection<(CryptoSymbol symbol, CryptoCandle candle)> Queue = new();
    private readonly CancellationTokenSource cancellationToken = new();

    public ThreadMonitorCandle()
    {
    }
    //GlobalData.ThreadProcessCandle.Thread.Start();

    //public void Start()
    //{
    //    Thread = new Thread(Execute);
    //    Thread.Name = "ThreadMonitorCandle";
    //    Thread.IsBackground = true;

    //    GlobalData.AddTextToLogTab(string.Format("Start create signals"));
    //}

    public void Stop()
    {
        cancellationToken.Cancel();

        GlobalData.AddTextToLogTab(string.Format("Stop create signals"));
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
                    // Er is een 1m candle gearriveerd, acties adhv deze candle..

                    //await Semaphore.WaitAsync();
                    try
                    {
                        PositionMonitor positionMonitor = new(symbol, candle);
                        await positionMonitor.NewCandleArrivedAsync();
                    }
                    finally
                    {
                        Semaphore.Release();
                    }
                }
                ).Start();

                //await Semaphore.WaitAsync();
                //_ = Task.Run(async () =>
                //{
                //    // Er is een 1m candle gearriveerd, acties adhv deze candle..

                //    //await Semaphore.WaitAsync();
                //    try
                //    {
                //        GlobalData.Logger.Info("Monitor:" + candle.OhlcText(symbol, GlobalData.IntervalList[0], symbol.PriceDisplayFormat, true, false, true));
                //        PositionMonitor positionMonitor = new(symbol, candle);
                //        await positionMonitor.NewCandleArrivedAsync();
                //    }
                //    finally
                //    {
                //        Semaphore.Release();
                //    }
                //});

                //GlobalData.Logger.Info("Monitor:" + candle.OhlcText(symbol, GlobalData.IntervalList[0], symbol.PriceDisplayFormat, true, false, true));
                //PositionMonitor positionMonitor = new(symbol, candle);
                //await positionMonitor.NewCandleArrivedAsync();

            }
        }
        // Die cancel moet er even uit
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
