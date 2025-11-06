using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

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
                var symbolData = symbol.Data;
                var symbolDataInterval = symbolData.Get(interval.IntervalPeriod);


                ZoneSession session = new()
                {
                    SymbolBase = symbol.Base,
                    SymbolQuote = symbol.Quote,
                    IntervalName = interval.Name,
                    ActiveInterval = interval.IntervalPeriod,
                    DlzShowBoxes = true,
                    FibShowRetracement = false,
                    FibShowZigZag = false,
                    ForceCalculation = true,
                    UseOptimizing = false,
                    Deviation = 1.0m,
                    TrendType = TrendType.Primary,
                };
                

                ZoneConfig data = new()
                {
                    Exchange = symbol.Exchange,
                    Symbol = symbol,
                    Interval = interval,
                    SymbolInterval = symbolInterval,
                };
                var trend = GlobalData.Settings.Signal.ZonesDlz.ZigZag;
                data.IndicatorList.Add((trend.TrendType, trend.UseHighLow), 
                    new(trend.TrendType, trend.UseHighLow, 1.0m));



                ZoneDlz.LoadZonesForSymbol(symbol);

                SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory = [];

                // avoid candles being removed...
                symbol.Data.CalculatingZones = true;
                try
                {
                    session.MaxDate = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
                    session.MaxDate = IntervalTools.StartOfIntervalCandle(session.MaxDate, interval.Duration);
                    session.MinDate = session.MaxDate - GlobalData.Settings.Signal.ZonesDlz.CandleCount * interval.Duration;
                    await ZoneDlz.CalculateDlzBoxesAsync(null, session, data, loadedCandlesInMemory);
                    await ZoneFvg.CalculateFvgZonesAsync(null, data.Symbol, interval, loadedCandlesInMemory);
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
