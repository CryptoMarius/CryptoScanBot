using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using System.Collections.Concurrent;

namespace CryptoScanner.Core.Zones;

public class ZoneThreadCalculate
{

    private readonly BlockingCollection<(CryptoSymbol, CryptoInterval)> Queue = [];
    private readonly CancellationTokenSource cancellationToken = new();


    public void Stop()
    {
        cancellationToken.Cancel();
        //GlobalData.AddTextToLogTab(string.Format("Stop calculating zones"));
    }


    public void AddToQueue(CryptoSymbol symbol, CryptoInterval interval)
    {
        Queue.Add((symbol, interval));
    }


    private static async Task CalculateZones(CryptoSymbol symbol, CryptoInterval interval)
    {
        if (symbol.QuoteData!.FetchCandles && symbol.Status == 1 && !symbol.IsBarometerSymbol())
        {
            if (symbol.QuoteData.MinimalVolume == 0 || symbol.Volume >= symbol.QuoteData.MinimalVolume)
            {
                //GlobalData.AddTextToLogTab($"Calculation zones for {symbol.Name} {interval.Name}");

                var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                var symbolDataInterval = symbol.Data.Get(interval.IntervalPeriod);

                //var trend = GlobalData.Settings.Signal.ZonesDlz.ZigZag;
                //TrendZigZagIndicatorList trendZigZagIndicatorList = [];
                //trendZigZagIndicatorList.Add((trend.TrendType, trend.UseHighLow),
                //    new(trend.TrendType, trend.UseHighLow, 1.0m));

                SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory = [];

                // Set BEFORE LoadZonesForSymbol: ScanForNew also writes to FvgZones.LongOpen/ShortOpen.
                // Concurrent access to the non-thread-safe OrderedList corrupts its internal List<T>,
                // producing null holes that later cause ArgumentNullException in BinarySearch.
                symbol.Data.CalculatingZones = true;
                try
                {
                    ZoneDlz.LoadZonesForSymbol(symbol);

                    int candleFetchCount = GlobalData.Settings.Signal.ZonesDlz.CandleCount;
                    CandleTime maxDate = CandleTime.AlignFromDateTime(DateTime.UtcNow, interval.Duration);
                    CandleTime minDate = maxDate - candleFetchCount * interval.Duration;
                    //await ZoneDlz.LoadHistoricCandles(symbol, interval, loadedCandlesInMemory);

                    await ZoneDlz.CalculateZonesAsync(null, symbol, interval, loadedCandlesInMemory);
                    await ZoneFvg.CalculateZonesAsync(null, symbol, interval, loadedCandlesInMemory);
                }
                finally
                {
                    await ZoneCandleEngine.SaveCandleDataToDiskAsync(symbol, loadedCandlesInMemory);
                    loadedCandlesInMemory.Clear();
                    _ = ZoneCandleEngine.CleanLoadedCandlesAsync(symbol);
                    symbol.Data.CalculatingZones = false;
                }
            }
        }
    }

    public async Task ExecuteAsync()
    {
        //GlobalData.AddTextToLogTab("Starting task for calculating zones");
        try
        {
            foreach ((CryptoSymbol symbol, CryptoInterval interval) in Queue.GetConsumingEnumerable(cancellationToken.Token))
            {
                try
                {
                    await CalculateZones(symbol, interval);
                }
                catch (OperationCanceledException)
                {
                    throw; // exit..
                }
                catch (Exception error)
                {
                    ScannerLog.Logger.Error(error, "");
                    GlobalData.AddTextToLogTab($"ThreadZoneCalculate ERROR {error.Message}");
                }
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



    public static void CalculateZonesForAllSymbolsAsync()
    {
        if (GlobalData.ActiveExchange != null)
        {
            foreach (var symbol in GlobalData.ActiveExchange.SymbolListName.Values)
            {
                foreach (var intervalName in GlobalData.Settings.Signal.ZonesFvg.IntervalList.ToList())
                {
                    if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                    {
                        GlobalData.ThreadZoneCalculate?.AddToQueue(symbol, interval);
                    }
                }
            }
        }
    }

}
