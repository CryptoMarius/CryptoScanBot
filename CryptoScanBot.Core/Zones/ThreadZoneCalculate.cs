using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using System.Collections.Concurrent;

namespace CryptoScanBot.Core.Zones;

// The database Sqlite is not the best when working with heavy multithreaded applications.
// There is basicly only one 1 write transaction allowed, that is not sufficient.
// (in effect (&currently) this goes only wrong for inserting signals in parrallel)
// In effect this is just a stupid workaround the database limitations
// Simple and effective but still kind of stupid..
// Extendable to other objects who dont require an immediatie id

public class ThreadZoneCalculate
{

    private readonly BlockingCollection<CryptoSymbol> Queue = [];
    private readonly CancellationTokenSource cancellationToken = new();


    public void Stop()
    {
        cancellationToken.Cancel();
        //GlobalData.AddTextToLogTab(string.Format("Stop calculating zones"));
    }


    public void AddToQueue(CryptoSymbol symbol)
    {
        Queue.Add(symbol);
    }


    public async Task ExecuteAsync()
    {
        //GlobalData.AddTextToLogTab("Starting task for calculating zones");
        try
        {
            foreach (CryptoSymbol symbol in Queue.GetConsumingEnumerable(cancellationToken.Token))
            {
                await LiquidityZones.CalculateZones(null, symbol);
            }
        }
        catch (OperationCanceledException)
        {
            // niets..
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab($"ThreadZoneCalculate ERROR {error.Message}");
        }

        GlobalData.AddTextToLogTab("ThreadZoneCalculate thread exit");
    }
}
