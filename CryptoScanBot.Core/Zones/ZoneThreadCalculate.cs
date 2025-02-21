using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

using System.Collections.Concurrent;

namespace CryptoScanBot.Core.Zones;

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
                var symbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);
                var symbolDataInterval = symbolData.Get(interval.IntervalPeriod);


                ZoneSession session = new()
                {
                    SymbolBase = symbol.Base,
                    SymbolQuote = symbol.Quote,
                    IntervalName = interval.Name,
                    ActiveInterval = interval.IntervalPeriod,
                    ShowLiqBoxes = true,
                    ZoomLiqBoxes = GlobalData.Settings.Signal.ZonesDlz.ZoomLowerTimeFrames,
                    ShowLiqZigZag = false,
                    ShowFib = false,
                    ShowFibZigZag = false,
                    ForceCalculation = true,
                    UseBatchProcess = true,
                    TrendType = GlobalData.Settings.Signal.ZonesDlz.TrendType,
                    UseHighLow = GlobalData.Settings.Signal.ZonesDlz.UseHighLow,
                    UseOptimizing = false,
                    Deviation = 1.0m,
                };
                

                ZoneData data = new()
                {
                    Account = GlobalData.ActiveAccount!,
                    Exchange = symbol.Exchange,
                    Symbol = symbol,
                    Interval = interval,
                    SymbolInterval = symbolInterval,
                };
                data.IndicatorList.Add((session.TrendType, session.UseHighLow), new(session.TrendType, session.UseHighLow, session.Deviation));



                ZoneDlz.LoadZonesForSymbol(symbol);

                SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory = [];

                // avoid candles being removed...
                symbol.CalculatingZones = true;
                try
                {
                    session.MaxDate = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
                    session.MaxDate = IntervalTools.StartOfIntervalCandle(session.MaxDate, interval.Duration);
                    session.MinDate = session.MaxDate - GlobalData.Settings.Signal.ZonesDlz.CandleCount * interval.Duration;
                    await ZoneDlz.CalculateDlzZonesAsync(null, session, data, loadedCandlesInMemory);
                    await ZoneFvg.CalculateFvgZonesAsync(null, data.Account, data.Symbol, interval, loadedCandlesInMemory);
                }
                finally
                {
                    await ZoneCandleEngine.SaveCandleDataToDiskAsync(symbol, loadedCandlesInMemory);
                    loadedCandlesInMemory.Clear();
                    _ = ZoneCandleEngine.CleanLoadedCandlesAsync(symbol);
                    symbol.CalculatingZones = false;
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
        if (GlobalData.Settings.General.Exchange != null)
        {
            foreach (var symbol in GlobalData.Settings.General.Exchange.SymbolListName.Values)
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
