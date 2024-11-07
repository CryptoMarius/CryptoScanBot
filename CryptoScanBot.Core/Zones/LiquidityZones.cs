using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Trend;

using System.Text;

namespace CryptoScanBot.Core.Experimental;

public class LiquidityZones
{
    public static async Task CalculateSymbolAsync(object? sender, CryptoZoneSession session, CryptoZoneData data)
    {
        CryptoCandles.LoadedCandlesFrom = ""; // force
        CryptoCandles.LoadedCandlesInMemory.Clear(); // force

        //Text = $"{data.Exchange.Name}.{session.SymbolBase}{session.SymbolQuote} {session.IntervalName}";
        StringBuilder log = new();
        try
        {
            //// Load candles from the CryptoBaseScanner
            //CryptoCandles.StartupTime = DateTime.UtcNow;
            //log.AppendLine($"loading available candles");
            //string baseStorageFolder = GlobalData.GetBaseDir();
            //string exchangeStorageFolder = baseStorageFolder + data.Exchange.Name.ToLower() + @"\";
            ////DataStore.LoadCandleForSymbol(exchangeStorageFolder, data.Symbol!);

            //// Load candles from the Exchange
            //if (CryptoCandles.LoadedCandlesFrom != data.Symbol.Name)
            //{
            //    CryptoCandles.LoadedCandlesInMemory = [];
            //    CryptoCandles.LoadedCandlesFrom = data.Symbol.Name;
            //    DataStore.LoadCandleForSymbol(exchangeStorageFolder, data.Symbol);
            //}

            // Load candles from the Exchange
            long unixStartUp = CandleTools.GetUnixTime(CryptoCandles.StartupTime, 0);
            long fetchFrom = CandleTools.GetUnixTime(CryptoCandles.StartupTime, data.SymbolInterval.Interval.Duration);
            if (fetchFrom + data.SymbolInterval.Interval.Duration > unixStartUp)
                fetchFrom -= data.SymbolInterval.Interval.Duration; // candle not yet completed

            fetchFrom -= GlobalData.Settings.Signal.Zones.CandleCount * data.SymbolInterval.Interval.Duration;
            await CryptoCandles.GetCandleData(data.Symbol, data.SymbolInterval, log, fetchFrom, true, GlobalData.Settings.Signal.Zones.CandleCount);
            CryptoCandles.SaveAddedCandleData(data.Symbol, log);
            if (data.SymbolInterval.CandleList.Count == 0)
                return;



            // Calculate the Indicator
            ScannerLog.Logger.Info($"Creating zigzag indicator");
            data.Indicator = new(data.SymbolInterval.CandleList, GlobalData.Settings.Signal.Zones.UseHighLow, session.OptimizeZigZag);
            data.Indicator.PostponeFinish = true;
            foreach (var candle in data.SymbolInterval.CandleList.Values)
            {
                if (candle.OpenTime >= fetchFrom)
                    data.Indicator.Calculate(candle, data.Interval.Duration);
            }
            data.Indicator.FinishJob();
            CryptoTrendIndicator trend = TrendInterval.InterpretZigZagPoints(data.Indicator, null);
            ScannerLog.Logger.Info($"Done adding zigzag candles {trend}");



            // Mark the dominant lows or highs
            if (session.ShowLiqBoxes)
                await CryptoCalculation.CalculateLiqBoxesAsync(sender, data, session, log);



            // start from unix date
            ScannerLog.Logger.Info($"Start plotting data");
            long unix = 0;
            if (data.SymbolInterval.CandleList.Count > GlobalData.Settings.Signal.Zones.CandleCount)
            {
                var candle = data.SymbolInterval.CandleList.Values[data.SymbolInterval.CandleList.Count - GlobalData.Settings.Signal.Zones.CandleCount];
                unix = candle.OpenTime;
            }
            var lastCandle = data.SymbolInterval.CandleList.Values.Last();


            CryptoCalculation.SaveToZoneTable(data, session);

            //plotView.Model = plotModel;
            //plotView.Model.InvalidatePlot(true);
            //plotView.Model.MouseDown += OnChartClick;
            //ScannerLog.Logger.Info($"Done plotting data");
            CryptoCandles.SaveAddedCandleData(data.Symbol, log);
        }
        catch (Exception e)
        {
            log.AppendLine(e.ToString());
            ScannerLog.Logger.Info($"ERROR {e}");
            CryptoCandles.SaveAddedCandleData(data.Symbol, log);
        }

        if (sender == null)
            CryptoCandles.CleanLoadedCandles(data.Symbol);
    }

    public static async Task CalculateAllSymbolsAsync(object? sender)
    {
        if (GlobalData.Settings.General.Exchange != null)
        {
            foreach (var symbol in GlobalData.Settings.General.Exchange.SymbolListName.Values)
            {
                if (symbol.QuoteData!.FetchCandles && symbol.Status == 1 && !symbol.IsBarometerSymbol())
                {
                    //if (symbol.Base == "ADA" || symbol.Base == "BTC" || symbol.Base == "DOGE") // test
                    //{

                    if (symbol.QuoteData.MinimalVolume == 0 || symbol.Volume >= symbol.QuoteData.MinimalVolume)
                    {
                        CryptoZoneSession session = new()
                        {
                            SymbolBase = symbol.Base,
                            SymbolQuote = symbol.Quote,
                            IntervalName = "1h",
                            ShowLiqBoxes = true,
                            ZoomLiqBoxes = true,
                            ShowZigZag = false
                        };

                        var symbolInterval = symbol.GetSymbolInterval(GlobalData.Settings.Signal.Zones.Interval);
                        session.IntervalName = symbolInterval.Interval.Name; // fix

                        CryptoZoneData data = new()
                        {
                            Exchange = symbol.Exchange,
                            Symbol = symbol,
                            Interval = symbolInterval.Interval,
                            SymbolInterval = symbolInterval,
                            Indicator = new(symbolInterval.CandleList, GlobalData.Settings.Signal.Zones.UseHighLow, session.OptimizeZigZag),
                        };

                        // avoid candles being removed...
                        data.Symbol.CalculatingZones = true;
                        try
                        {
                            await CalculateSymbolAsync(sender, session, data);
                        }
                        finally
                        {
                            data.Symbol.CalculatingZones = false;
                        }
                    }
                }
                //}
            }
        }
    }

}
