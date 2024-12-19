using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;

using System.Collections.Concurrent;

namespace CryptoScanBot.Core.Zones;

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
                //await MarketTrend.CalculateMarketTrendAsync(GlobalData.ActiveAccount!, symbol, 0, 0);
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
