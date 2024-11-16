using System.Text;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;
using CryptoScanBot.ZoneVisualisation.Zones;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Zones;


public class CryptoCalculation
{

    private static bool UnzoomedPercentageBelowMinimum(ZigZagResult zigZag, CryptoSymbol symbol, CryptoInterval interval)
    {
        var value = GlobalData.Settings.Signal.Zones.MinimumUnZoomedPercentage;
        if (value > 0 && zigZag.Percentage < value)
        {
            zigZag.Dominant = false;
            ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Unzoomed box ignored {zigZag.Percentage:N2} < {value:N2} {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
            return true;
        }
        return false;
    }


    private static bool UnzoomedPercentageAboveMaximum(ZigZagResult zigZag, CryptoSymbol symbol, CryptoInterval interval)
    {
        var value = GlobalData.Settings.Signal.Zones.MaximumUnZoomedPercentage;
        if (value > 0 && zigZag.Percentage > value)
        {
            zigZag.Dominant = false;
            ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Unzoomed box ignored {zigZag.Percentage:N2} > {value:N2} {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
            return true;
        }

        return false;
    }

 
    private static bool ZoomedPercentageBelowMinimum(ZigZagResult zigZag, CryptoSymbol symbol, CryptoInterval interval)
    {
        var value = GlobalData.Settings.Signal.Zones.MinimumZoomedPercentage;
        if (value > 0 && zigZag.Percentage < value)
        {
            zigZag.Dominant = false;
            ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Zoomed box ignored {zigZag.Percentage:N2} < {value:N2} {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
            return true;
        }
        return false;
    }

    private static bool ZoomedPercentageAboveMaximum(ZigZagResult zigZag, CryptoSymbol symbol, CryptoInterval interval)
    {
        var value = GlobalData.Settings.Signal.Zones.MaximumZoomedPercentage;
        if (value > 0 && zigZag.Percentage > value)
        {
            zigZag.Dominant = false;
            ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Zoomed box ignored {zigZag.Percentage:N2} > {value:N2} {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
            return true;
        }
        return false;
    }


    public static async Task MakeDominantAndZoomInAsync(CryptoSymbol symbol, CryptoInterval interval,
        ZigZagResult zigZag, decimal top, decimal bottom, bool zoomFurther, StringBuilder log)
    {
        zigZag.Top = top;
        zigZag.Bottom = bottom;
        zigZag.Dominant = true;
        zigZag.Percentage = 100 * ((zigZag.Top - zigZag.Bottom) / zigZag.Bottom);
        ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot at {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");

        // Borrow the wick from the neighbour if higher/lower
        if (zigZag.PointType == 'L')
        {
            CryptoSymbolInterval symbolInterval = symbol!.GetSymbolInterval(interval!.IntervalPeriod);
            if (symbolInterval.CandleList.TryGetValue(zigZag.Candle.OpenTime + interval.Duration, out CryptoCandle? candle))
            {
                if (candle.Low < zigZag.Bottom)
                {
                    //zigZag.Candle = candle; // dont
                    zigZag.Top = Math.Max(candle.Close, candle.Open);
                    zigZag.Bottom = candle.Low;
                    zigZag.Value = candle.GetLowValue(GlobalData.Settings.Signal.Zones.UseHighLow);
                }
            }
            if (symbolInterval.CandleList.TryGetValue(zigZag.Candle.OpenTime - interval.Duration, out candle))
            {
                if (candle.Low < zigZag.Bottom)
                {
                    //zigZag.Candle = candle; // dont
                    zigZag.Top = Math.Max(candle.Close, candle.Open);
                    zigZag.Bottom = candle.Low;
                    zigZag.Value = candle.GetLowValue(GlobalData.Settings.Signal.Zones.UseHighLow);
                }
            }
        }
        else if (zigZag.PointType == 'H')
        {
            CryptoSymbolInterval symbolInterval = symbol!.GetSymbolInterval(interval!.IntervalPeriod);
            if (symbolInterval.CandleList.TryGetValue(zigZag.Candle.OpenTime + interval.Duration, out CryptoCandle? candle))
            {
                if (candle.High > zigZag.Top)
                {
                    //zigZag.Candle = candle;
                    zigZag.Top = candle.High;
                    zigZag.Bottom = Math.Min(candle.Close, candle.Open);
                    zigZag.Value = candle.GetHighValue(GlobalData.Settings.Signal.Zones.UseHighLow);
                }

            }
            if (symbolInterval.CandleList.TryGetValue(zigZag.Candle.OpenTime - interval.Duration, out candle))
            {
                if (candle.High > zigZag.Top)
                {
                    //zigZag.Candle = candle;
                    zigZag.Top = candle.High;
                    zigZag.Bottom = Math.Min(candle.Close, candle.Open);
                    zigZag.Value = candle.GetHighValue(GlobalData.Settings.Signal.Zones.UseHighLow);
                }
            }
        }
        if (zigZag.Top != top || zigZag.Bottom != bottom)
        {
            zigZag.Percentage = 100 * ((zigZag.Top - zigZag.Bottom) / zigZag.Bottom);
            ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot corrected {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
        }

        // Is the (unzoomed) percentage between the configured limits? 
        // Or is the percentage alread below the zoom-limit? (saves time)
        if (UnzoomedPercentageBelowMinimum(zigZag, symbol, interval) 
            || UnzoomedPercentageAboveMaximum(zigZag, symbol, interval)
            || ZoomedPercentageBelowMinimum(zigZag, symbol, interval))
            return; // (mark the point as not dominant + exit)



        // If the found percentage is obove 0.7% zoom in on the lower intervals (withing the boundaries of the current candle)
        if (zigZag.Percentage >= GlobalData.Settings.Signal.Zones.MaximumZoomedPercentage && zoomFurther)
        {
            CryptoIntervalPeriod zoom = interval!.IntervalPeriod;
            long unixStart = zigZag.Candle.OpenTime;
            long unixEinde = zigZag.Candle.OpenTime + interval.Duration;
            //DateTime unixStartDebug = CandleTools.GetUnixDate(unixStart);
            //DateTime unixEindeDebug = CandleTools.GetUnixDate(unixEinde);
            int durationForThisCandle = (int)(unixEinde - unixStart);

            while (zigZag.Percentage >= GlobalData.Settings.Signal.Zones.MaximumZoomedPercentage && zoom > CryptoIntervalPeriod.interval1m)
            {
                zoom--;
                ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot zooming {zoom} {zigZag.Percentage:N2}%");

                // Is Interval supported by Exchange
                CryptoSymbolInterval zoomInterval = symbol!.GetSymbolInterval(zoom);
                if (symbol.Exchange.IsIntervalSupported(zoomInterval.IntervalPeriod))
                {
                    int count = durationForThisCandle / zoomInterval.Interval.Duration;
                    CandleEngine.LoadDataFromDisk(symbol, zoomInterval.Interval, zoomInterval.CandleList);
                    if (await CandleEngine.FetchFrom(symbol, zoomInterval.Interval, zoomInterval.CandleList, log, unixStart, count))
                        CandleEngine.LoadedCandlesInMemory[zoomInterval.Interval.IntervalPeriod] = true;

                    long loop = IntervalTools.StartOfIntervalCandle(unixStart, zoomInterval.Interval.Duration);
                    while (loop < unixEinde && zigZag.Percentage >= GlobalData.Settings.Signal.Zones.MaximumZoomedPercentage)
                    {
                        //DateTime loopDebug = CandleTools.GetUnixDate(loop);
                        if (loop >= zigZag.Candle.OpenTime) //really?
                        {
                            if (zoomInterval.CandleList.TryGetValue(loop, out CryptoCandle? candle))
                            {
                                if (zigZag.PointType == 'L')
                                {
                                    decimal bodyTop = Math.Max(candle.Open, candle.Close);
                                    if (bodyTop < zigZag.Top)
                                    {
                                        decimal percentage = 100 * ((bodyTop - zigZag.Bottom) / zigZag.Bottom);
                                        if (percentage >= GlobalData.Settings.Signal.Zones.MinimumUnZoomedPercentage)
                                        {
                                            zigZag.Top = bodyTop;
                                            zigZag.Percentage = percentage;
                                            ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot zoomed {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2} {zoomInterval.Interval.Name}");
                                        }
                                    }
                                }
                                else // High
                                {
                                    decimal bodyBottom = Math.Min(candle.Open, candle.Close);
                                    if (bodyBottom > zigZag.Bottom)
                                    {
                                        decimal percentage = 100 * ((zigZag.Top - bodyBottom) / bodyBottom);
                                        if (percentage >= GlobalData.Settings.Signal.Zones.MinimumUnZoomedPercentage)
                                        {
                                            zigZag.Bottom = bodyBottom;
                                            zigZag.Percentage = percentage;
                                            ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot zoomed {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2} {zoomInterval.Interval.Name}");
                                        }
                                    }
                                }
                            }
                        }
                        loop += zoomInterval.Interval.Duration;
                    }
                }

                if (zigZag.Percentage <= GlobalData.Settings.Signal.Zones.MaximumZoomedPercentage)
                    break;
            }
        }

        // Is the zoomed percentage between the configured limits?
        if (ZoomedPercentageBelowMinimum(zigZag, symbol, interval) || ZoomedPercentageAboveMaximum(zigZag, symbol, interval))
            return; // (mark the point as not dominant + exit)
    }

    public static async Task CalculateLiqBoxesAsync(AddTextEvent? sender, CryptoZoneData data, CryptoZoneSession session, StringBuilder log)
    {
        //long startTime = Stopwatch.GetTimestamp();
        //ScannerLog.Logger.Info("CalculateLiqBoxesAsync.Start");
        GlobalData.AddTextToLogTab($"{data.Symbol.Name} Calculating dominant pivots and zones");
        ZigZagResult? previous = null;
        ZigZagResult? previous2 = null;
        foreach (var zigZag in data.Indicator.ZigZagList)
        {
            if (previous != null && previous2 != null && !zigZag.Dummy)
            {
                //if (zigZag.Candle.DateLocal >= new DateTime(2024, 10, 19, 11, 0, 0, DateTimeKind.Local))
                //    previous = previous;
                sender?.Invoke($"Calculating Bybit.Spot.{session.SymbolQuote} {zigZag.Candle.Date}");

                // Check: a dominant Low leading to a new Higher High
                if (zigZag.PointType == 'H' && previous.PointType == 'L' && previous2.PointType == 'H' && previous2.Value < zigZag.Value)
                    await MakeDominantAndZoomInAsync(data.Symbol, data.SymbolInterval.Interval, previous,
                        Math.Max(previous.Candle.Open, previous.Candle.Close), previous.Candle.Low, session.ZoomLiqBoxes, log);

                // Check: a dominant High leading to a new Lower Low
                if (zigZag.PointType == 'L' && previous.PointType == 'H' && previous2.PointType == 'L' && previous2.Value > zigZag.Value)
                    await MakeDominantAndZoomInAsync(data.Symbol, data.SymbolInterval.Interval, previous,
                        previous.Candle.High, Math.Min(previous.Candle.Open, previous.Candle.Close), session.ZoomLiqBoxes, log);
            }
            previous2 = previous;
            previous = zigZag;
        }
        //ScannerLog.Logger.Info("CalculateLiqBoxesAsync.Stop " + Stopwatch.GetElapsedTime(startTime).TotalSeconds.ToString());
    }


    public static void CalculateBrokenBoxes(CryptoZoneData data)
    {
        GlobalData.AddTextToLogTab($"{data.Symbol.Name} Calculating dominant pivots and zones");
        // Determine if level is broken (not accurate! right now just for display purposes)
        // TODO: Its only a dominant point when the BOS has occurred!
        // from there we need to invalidate the liq.box (the 3 candles below is just plain wrong!)
        ZigZagResult? prevZigZag = null;
        //ScannerLog.Logger.Info($"{data.Symbol.Name} Start marking broken zones");
        //GlobalData.AddTextToLogTab($"{data.Symbol.Name} Start marking broken zones");
        foreach (var zigzag in data.Indicator.ZigZagList)
        {
            if (prevZigZag == null)
                zigzag.InvalidOn = zigzag.Candle; //Just to show it..
            else
            {
                //if (zigzag.Candle!.DateLocal >= new DateTime(2024, 10, 10, 18, 0, 0, DateTimeKind.Local))
                //    zigzag.Candle = zigzag.Candle; // debug 

                // skip a couple of candles
                bool brokenBos = false;
                long key = zigzag.Candle.OpenTime;
                long checkUpTo = CandleTools.GetUnixTime(CandleEngine.StartupTime, data.SymbolInterval.Interval.Duration);
                while (zigzag.Dominant && key <= checkUpTo)
                {
                    key += data.SymbolInterval.Interval.Duration;
                    if (data.SymbolInterval.CandleList.TryGetValue(key, out CryptoCandle? candle))
                    {
                        // We need a BOS before we can invalidate the liq.box
                        if (!brokenBos)
                        {
                            if (zigzag.PointType == 'H' && candle.GetLowValue(GlobalData.Settings.Signal.Zones.UseHighLow) <= prevZigZag.Value)
                                brokenBos = true;
                            if (zigzag.PointType == 'L' && candle.GetHighValue(GlobalData.Settings.Signal.Zones.UseHighLow) >= prevZigZag.Value)
                                brokenBos = true;
                        }
                        else
                        {
                            //if (zigzag.PointType == 'H' && (candle.High >= zigzag.Top || candle.GetHighValue(GlobalData.Settings.Signal.Zones.UseHighLow) >= zigzag.Bottom))
                            if (zigzag.PointType == 'H' && (candle.High >= zigzag.Top || Math.Max(candle.Open, candle.Close) >= zigzag.Bottom))
                            {
                                zigzag.InvalidOn = candle;
                                break;
                            }
                            //if (zigzag.PointType == 'L' && (candle.Low <= zigzag.Bottom || candle.GetLowValue(GlobalData.Settings.Signal.Zones.UseHighLow) <= zigzag.Top))
                            if (zigzag.PointType == 'L' && (candle.Low <= zigzag.Bottom || Math.Min(candle.Open, candle.Close) <= zigzag.Top))
                            {
                                zigzag.InvalidOn = candle;
                                break;
                            }
                        }
                    }
                }
            }
            prevZigZag = zigzag;
        }
    }


    public static void SaveToZoneTable(CryptoZoneData data)
    {
        using CryptoDatabase databaseThread = new();
        databaseThread.Connection.Open();

        using var transaction = databaseThread.BeginTransaction();
        databaseThread.Connection.Execute($"delete from zone where symbolId={data.Symbol.Id}", transaction);
        transaction.Commit();

        var x = GlobalData.ActiveAccount!.Data.GetSymbolData(data.Symbol.Name);
        x.ResetZoneData();

        foreach (var zigZag in data.Indicator.ZigZagList)
        {
            if (zigZag.Dominant && zigZag.InvalidOn == null) //zigZag.Bottom > 0 && 
            {
                CryptoZone zone = new()
                {
                    AccountId = GlobalData.ActiveAccount.Id,
                    Account = GlobalData.ActiveAccount,
                    ExchangeId = data.Exchange.Id,
                    Exchange = data.Exchange,
                    SymbolId = data.Symbol.Id,
                    Symbol = data.Symbol,
                    Top = zigZag.Top,
                    Bottom = zigZag.Bottom,
                    Side = CryptoTradeSide.Long,
                    Strategy = CryptoSignalStrategy.DominantLevel,
                };

                if (zigZag.PointType == 'L')
                {
                    zone.Side = CryptoTradeSide.Long;
                    zone.AlarmPrice = zone.Top * (100 + GlobalData.Settings.Signal.Zones.WarnPercentage) / 100;
                    x.ZoneListLong.Add(zone);
                }
                else
                {
                    zone.Side = CryptoTradeSide.Short;
                    zone.AlarmPrice = zone.Bottom * (100 - GlobalData.Settings.Signal.Zones.WarnPercentage) / 100;
                    x.ZoneListShort.Add(zone);
                }

                databaseThread.Connection.Insert(zone);

            }
        }
    }
}
