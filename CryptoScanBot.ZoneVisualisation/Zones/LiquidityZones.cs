using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Trend;
using CryptoScanBot.Core.Zones;

using System.Text;

namespace CryptoScanBot.ZoneVisualisation.Zones;

public class LiquidityZones
{
    public static async Task CalculateZonesForSymbolAsync(AddTextEvent? sender, CryptoZoneSession session, CryptoZoneData data)
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

            // Load candles from disk and exchange
            CandleEngine.LoadDataFromDisk(data.Symbol, data.Interval, data.SymbolInterval.CandleList);
            if (await CandleEngine.FetchFrom(data.Symbol, data.Interval, data.SymbolInterval.CandleList, log, fetchFrom, GlobalData.Settings.Signal.Zones.CandleCount))
                CandleEngine.LoadedCandlesInMemory[data.Interval.IntervalPeriod] = true;
            if (data.SymbolInterval.CandleList.Count == 0)
                return;


            // Calculate indicators
            foreach (var candle in data.SymbolInterval.CandleList.Values)
            {
                if (candle.OpenTime >= session.MinUnix && candle.OpenTime <= session.MaxUnix)
                {
                    data.Indicator.Calculate(candle);
                    data.IndicatorFib.Calculate(candle);
                }
            }
            data.Indicator.FinishJob();
            data.IndicatorFib.FinishJob();
            CryptoTrendIndicator trend = TrendInterval.InterpretZigZagPoints(data.Indicator, null);


            // Mark the dominant lows or highs
            if (session.ShowLiqBoxes)
            {
                await CryptoCalculation.CalculateLiqBoxesAsync(sender, data, session.ZoomLiqBoxes, log);
                CryptoCalculation.CalculateBrokenBoxes(data);
            }



            CryptoCalculation.SaveToZoneTable(data);

            //plotView.Model = plotModel;
            //plotView.Model.InvalidatePlot(true);
            //plotView.Model.MouseDown += OnChartClick;
            //ScannerLog.Logger.Info($"Done plotting data");
            CandleEngine.SaveAddedCandleData(data.Symbol, log);
        }
        catch (Exception e)
        {
            log.AppendLine(e.ToString());
            ScannerLog.Logger.Info($"ERROR {e}");
            CandleEngine.SaveAddedCandleData(data.Symbol, log);
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
                    //if (symbol.Base == "ADA" || symbol.Base == "BTC" || symbol.Base == "DOGE") // test

                    if (symbol.QuoteData.MinimalVolume == 0 || symbol.Volume >= symbol.QuoteData.MinimalVolume)
                    {
                        CryptoZoneSession session = new()
                        {
                            SymbolBase = symbol.Base,
                            SymbolQuote = symbol.Quote,
                            IntervalName = "1h", // overridden later
                            ShowLiqBoxes = true,
                            ZoomLiqBoxes = GlobalData.Settings.Signal.Zones.ZoomLowerTimeFrames,
                            ShowLiqZigZag = false,
                            ShowFib = false,
                            ShowFibZigZag = false,
                        };

                        var symbolInterval = symbol.GetSymbolInterval(GlobalData.Settings.Signal.Zones.Interval);
                        session.IntervalName = symbolInterval.Interval.Name; // fix

                        CryptoZoneData data = new()
                        {
                            Exchange = symbol.Exchange,
                            Symbol = symbol,
                            Interval = symbolInterval.Interval,
                            SymbolInterval = symbolInterval,
                            Indicator = new(symbolInterval.CandleList, GlobalData.Settings.Signal.Zones.UseHighLow, session.Deviation),
                            IndicatorFib = new(symbolInterval.CandleList, true, session.Deviation),
                            //AccountSymbolIntervalData = GlobalData.ActiveAccount!.Data.GetSymbolTrendData(symbol.Name, symbolInterval.Interval.IntervalPeriod),
                        };

                        // avoid candles being removed...
                        data.Symbol.CalculatingZones = true;
                        try
                        {
                            await CalculateZonesForSymbolAsync(sender, session, data);
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
