using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Zones;

public class LiquidityZones
{
    public static async Task CalculateZonesForSymbolAsync(AddTextEvent? sender, CryptoZoneSession session,
        CryptoZoneData data, SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        try
        {
            try
            {
                // Determine dates
                long unixStartUp = CandleTools.GetUnixTime(DateTime.UtcNow, 0); // todo Emulator date?
                long fetchFrom = IntervalTools.StartOfIntervalCandle(unixStartUp, data.SymbolInterval.Interval.Duration);
                fetchFrom -= GlobalData.Settings.Signal.Zones.CandleCount * data.SymbolInterval.Interval.Duration;

                // Load candles from disk
                if (!loadedCandlesInMemory.TryGetValue(data.Interval.IntervalPeriod, out bool _))
                    await CandleEngine.LoadCandleDataFromDiskAsync(data.Symbol, data.Interval);
                loadedCandlesInMemory.TryAdd(data.Interval.IntervalPeriod, true); // in memory, nothing changed (save alway's)

                // Load candles from the exchange
                if (await CandleEngine.FetchFrom(data.Symbol, data.Interval, fetchFrom, GlobalData.Settings.Signal.Zones.CandleCount))
                    loadedCandlesInMemory[data.Interval.IntervalPeriod] = true;
                if (data.SymbolInterval.CandleList.Count == 0)
                    return;

                await data.Symbol.CandleLock.WaitAsync();
                try
                {
                    // Calculate indicators
                    foreach (var candle in data.SymbolInterval.CandleList.Values)
                    {
                        if (candle.OpenTime >= session.MinDate && candle.OpenTime <= session.MaxDate)
                        {
                            data.Indicator.Calculate(candle, session.UseBatchProcess);
#if !DEBUGZIGZAG
                            data.IndicatorFib.Calculate(candle, session.UseBatchProcess);
#endif
                        }
                    }

                    // Remember the last swing point for the automatic zone calculation
                    AccountSymbolData symbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(data.Symbol.Name);
                    AccountSymbolIntervalData symbolIntervalData = symbolData.GetAccountSymbolInterval(data.Interval.IntervalPeriod);
                    if (data.Indicator.LastSwingPoint != null)
                        symbolIntervalData.TimeLastSwingPoint = data.Indicator.LastSwingPoint.Candle.OpenTime;
                    if (data.Indicator.LastSwingLow != null)
                        symbolIntervalData.LastSwingLow = data.Indicator.LastSwingLow.Value;
                    if (data.Indicator.LastSwingHigh != null)
                        symbolIntervalData.LastSwingHigh = data.Indicator.LastSwingHigh.Value;

                    if (session.UseBatchProcess)
                    {
                        data.Indicator.FinishBatch();
#if !DEBUGZIGZAG
                        data.IndicatorFib.FinishBatch();
#endif
                    }
                }
                finally
                {
                    data.Symbol.CandleLock.Release();
                }



                // Mark the dominant lows or highs
                if (session.ForceCalculation)
                {
                    await CryptoCalculation.CalculateLiqBoxesAsync(sender, data, session.ZoomLiqBoxes, loadedCandlesInMemory);
                    CryptoCalculation.CalculateIntroZone(data);
                    CryptoCalculation.CalculateBrokenBoxes(data);
                }


                // Create the zones and save them
                if (session.ForceCalculation)
                    ZoneTools.SaveZonesForSymbol(data, data.Indicator.ZigZagList);
                await CandleEngine.SaveCandleDataToDiskAsync(data.Symbol, loadedCandlesInMemory);

                //GlobalData.AddTextToLogTab($"{data.Symbol.Name} points={data.Indicator.PivotList.Count} fib.points={data.IndicatorFib.PivotList.Count}");
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Info($"ERROR {error}");
                GlobalData.AddTextToLogTab($"ERROR {error}");
                await CandleEngine.SaveCandleDataToDiskAsync(data.Symbol, loadedCandlesInMemory);
            }
        }
        finally
        {
            if (sender == null)
                _ = CandleEngine.CleanLoadedCandlesAsync(data.Symbol);
        }
    }


    public static async Task CalculateZones(AddTextEvent? sender, CryptoSymbol symbol)
    {
        if (symbol.QuoteData!.FetchCandles && symbol.Status == 1 && !symbol.IsBarometerSymbol())
        {
            //if (!(symbol.Base == "XRP" || symbol.Base == "BTC" || symbol.Base == "DOGE" || symbol.Base == "SOL"))
            //if (!(symbol.Base == "BTC"))
            //    continue;


            if (symbol.QuoteData.MinimalVolume == 0 || symbol.Volume >= symbol.QuoteData.MinimalVolume)
            {
                //GlobalData.AddTextToLogTab($"Calculation zones for {symbol.Name}");

                CryptoInterval interval = GlobalData.IntervalListPeriod[GlobalData.Settings.Signal.Zones.Interval];
                CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

                // Would be nice if we got rid of this one
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
                    UseOptimizing = true,
                    ShowSecondary = false,
                    Deviation = 1.0m,
                };


                CryptoZoneData data = new()
                {
                    Account = GlobalData.ActiveAccount!,
                    Exchange = symbol.Exchange,
                    Symbol = symbol,
                    Interval = interval,
                    SymbolInterval = symbolInterval,
                    Indicator = new(false, session.Deviation),
                    IndicatorFib = new(true, session.Deviation),
                };

                session.MaxDate = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
                session.MaxDate = IntervalTools.StartOfIntervalCandle(session.MaxDate, interval.Duration);
                session.MinDate = session.MaxDate - GlobalData.Settings.Signal.Zones.CandleCount * interval.Duration;

                // avoid candles being removed...
                data.Symbol.CalculatingZones = true;
                try
                {
                    ZoneTools.LoadZonesForSymbol(data.Symbol.Id, data);
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

    public static void CalculateZonesForAllSymbolsAsync(AddTextEvent? sender)
    {
        if (GlobalData.Settings.General.Exchange != null)
        {
            foreach (var symbol in GlobalData.Settings.General.Exchange.SymbolListName.Values)
            {
                GlobalData.ThreadZoneCalculate?.AddToQueue(symbol);
            }
        }
    }

}
