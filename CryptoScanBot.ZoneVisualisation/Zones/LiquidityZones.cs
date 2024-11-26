using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Trend;

using System.Text;

namespace CryptoScanBot.ZoneVisualisation.Zones;

public class LiquidityZones
{
    public static async Task CalculateZonesForSymbolAsync(AddTextEvent? sender, CryptoZoneSession session, CryptoZoneData data, SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        //long startTime = Stopwatch.GetTimestamp();
        //ScannerLog.Logger.Info("CalculateZonesForSymbolAsync.Start");

        //Text = $"{data.Exchange.Name}.{session.SymbolBase}{session.SymbolQuote} {session.IntervalName}";
        StringBuilder log = new();
        try
        {
            // Determine dates
            long unixStartUp = CandleTools.GetUnixTime(CandleEngine.StartupTime, 0); // todo Emulator date?
            long fetchFrom = IntervalTools.StartOfIntervalCandle(unixStartUp, data.SymbolInterval.Interval.Duration);
            fetchFrom -= GlobalData.Settings.Signal.Zones.CandleCount * data.SymbolInterval.Interval.Duration;

            // Load candles from disk
            if (!loadedCandlesInMemory.TryGetValue(data.Interval.IntervalPeriod, out bool _))
                CandleEngine.LoadCandleDataFromDisk(data.Symbol, data.Interval, data.SymbolInterval.CandleList);
            loadedCandlesInMemory.TryAdd(data.Interval.IntervalPeriod, false); // in memory, nothing changed

            // Load candles from the exchange
            if (await CandleEngine.FetchFrom(data.Symbol, data.Interval, data.SymbolInterval.CandleList, log, fetchFrom, GlobalData.Settings.Signal.Zones.CandleCount))
                loadedCandlesInMemory[data.Interval.IntervalPeriod] = true;
            if (data.SymbolInterval.CandleList.Count == 0)
                return;


            // Set Batch mode and add all the candles to the indicators
            if (session.UseBatchProcess)
            {
                data.Indicator.StartBatch();
                if (session.ShowFib || session.ShowFibZigZag)
                    data.IndicatorFib.StartBatch();
            }
            // Calculate indicators
            foreach (var candle in data.SymbolInterval.CandleList.Values)
            {
                if (candle.OpenTime >= session.MinDate && candle.OpenTime <= session.MaxDate)
                {
                    data.Indicator.Calculate(candle);
                    if (session.ShowFib || session.ShowFibZigZag)
                        data.IndicatorFib.Calculate(candle);
                }
            }
            if (session.UseBatchProcess)
            {
                data.Indicator.FinishBatch();
                if (session.ShowFib || session.ShowFibZigZag)
                    data.IndicatorFib.FinishBatch();
            }



            // Mark the dominant lows or highs
            if (session.ShowLiqBoxes && session.ForceCalculation)
            {
                await CryptoCalculation.CalculateLiqBoxesAsync(sender, data, session.ZoomLiqBoxes, log, loadedCandlesInMemory);
                CryptoCalculation.CalculateBrokenBoxes(data);
            }


            // Create the zones and save them
            if (session.ForceCalculation)
                ZoneTools.SaveZonesForSymbol(data.Symbol, data.Interval, data.Indicator.ZigZagList);

            CandleEngine.SaveCandleDataToDisk(data.Symbol, log, loadedCandlesInMemory);
        }
        catch (Exception error)
        {
            log.AppendLine(error.ToString());
            ScannerLog.Logger.Info($"ERROR {error}");
            GlobalData.AddTextToLogTab($"ERROR {error}");
            CandleEngine.SaveCandleDataToDisk(data.Symbol, log, loadedCandlesInMemory);
        }

        if (sender == null)
            _ = CandleEngine.CleanLoadedCandlesAsync(data.Symbol);
        //ScannerLog.Logger.Info("CalculateZonesForSymbolAsync.Stop " + Stopwatch.GetElapsedTime(startTime).TotalSeconds.ToString());
    }


    public static async Task CalculateZonesForAllSymbolsAsync(AddTextEvent? sender)
    {
        if (GlobalData.Settings.General.Exchange != null)
        {
            foreach (var symbol in GlobalData.Settings.General.Exchange.SymbolListName.Values)
            {
                if (symbol.QuoteData!.FetchCandles && symbol.Status == 1 && !symbol.IsBarometerSymbol())
                {
                    //if (!(symbol.Base == "XRP" || symbol.Base == "BTC" || symbol.Base == "DOGE" || symbol.Base == "SOL"))
                    //if (!(symbol.Base == "BTC"))
                    //    continue;


                    if (symbol.QuoteData.MinimalVolume == 0 || symbol.Volume >= symbol.QuoteData.MinimalVolume)
                    {
                        var symbolInterval = symbol.GetSymbolInterval(GlobalData.Settings.Signal.Zones.Interval);

                        CryptoZoneSession session = new()
                        {
                            SymbolBase = symbol.Base,
                            SymbolQuote = symbol.Quote,
                            IntervalName = symbolInterval.Interval.Name,
                            ActiveInterval = symbolInterval.Interval.IntervalPeriod,
                            ShowLiqBoxes = true,
                            ZoomLiqBoxes = GlobalData.Settings.Signal.Zones.ZoomLowerTimeFrames,
                            ShowLiqZigZag = false,
                            ShowFib = false,
                            ShowFibZigZag = false,
                            ForceCalculation = true,
                            UseBatchProcess = true,
                        };

                        CryptoZoneData data = new()
                        {
                            Account = GlobalData.ActiveAccount!,
                            Exchange = symbol.Exchange,
                            Symbol = symbol,
                            Interval = symbolInterval.Interval,
                            SymbolInterval = symbolInterval,
                            Indicator = new(symbolInterval.CandleList, GlobalData.Settings.Signal.Zones.UseHighLow, session.Deviation, symbolInterval.Interval.Duration),
                            IndicatorFib = new(symbolInterval.CandleList, true, session.Deviation, symbolInterval.Interval.Duration),
                            //AccountSymbolIntervalData = GlobalData.ActiveAccount!.Data.GetSymbolTrendData(symbol.Name, symbolInterval.Interval.IntervalPeriod),
                        };

                        session.MaxDate = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
                        session.MaxDate = IntervalTools.StartOfIntervalCandle(session.MaxDate, data.Interval.Duration);
                        session.MinDate = session.MaxDate - GlobalData.Settings.Signal.Zones.CandleCount * data.Interval.Duration;

                        // avoid candles being removed...
                        data.Symbol.CalculatingZones = true;
                        try
                        {
                            SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory = [];
                            await CalculateZonesForSymbolAsync(sender, session, data, loadedCandlesInMemory);
                        }
                        finally
                        {
                            data.Symbol.CalculatingZones = false;
                        }
                    }
                }
            }
        }
    }

}
