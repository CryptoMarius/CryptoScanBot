using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

namespace CryptoScanBot.Core.Zones;


public class CryptoCalculation
{

    private static bool UnzoomedPercentageBelowMinimum(ZigZagResult zigZag) //, CryptoSymbol symbol, CryptoInterval interval)
    {
        if (GlobalData.Settings.Signal.Zones.ZonesApplyUnzoomed)
        {
            var value = GlobalData.Settings.Signal.Zones.MinimumUnZoomedPercentage;
            if (value > 0 && zigZag.Percentage < value)
            {
                zigZag.Dominant = false;
                //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Unzoomed box ignored {zigZag.Percentage:N2} < {value:N2} {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
                return true;
            }
        }
        return false;
    }


    private static bool UnzoomedPercentageAboveMaximum(ZigZagResult zigZag) //, CryptoSymbol symbol, CryptoInterval interval)
    {
        if (GlobalData.Settings.Signal.Zones.ZonesApplyUnzoomed)
        {
            var value = GlobalData.Settings.Signal.Zones.MaximumUnZoomedPercentage;
            if (value > 0 && zigZag.Percentage > value)
            {
                zigZag.Dominant = false;
                //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Unzoomed box ignored {zigZag.Percentage:N2} > {value:N2} {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
                return true;
            }
        }
        return false;
    }


    private static bool ZoomedPercentageBelowMinimum(ZigZagResult zigZag) //, CryptoSymbol symbol, CryptoInterval interval)
    {
        var value = GlobalData.Settings.Signal.Zones.MinimumZoomedPercentage;
        if (value > 0 && zigZag.Percentage < value)
        {
            zigZag.Dominant = false;
            //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Zoomed box ignored {zigZag.Percentage:N2} < {value:N2} {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
            return true;
        }
        return false;
    }

    private static bool ZoomedPercentageAboveMaximum(ZigZagResult zigZag) //, CryptoSymbol symbol, CryptoInterval interval)
    {
        var value = GlobalData.Settings.Signal.Zones.MaximumZoomedPercentage;
        if (value > 0 && zigZag.Percentage > value)
        {
            zigZag.Dominant = false;
            //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Zoomed box ignored {zigZag.Percentage:N2} > {value:N2} {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
            return true;
        }
        return false;
    }


    public static async Task MakeDominantAndZoomInAsync(CryptoSymbol symbol, CryptoInterval interval,
        ZigZagResult zigZag, decimal top, decimal bottom, bool zoomFurther, 
        SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        zigZag.Top = top;
        zigZag.Bottom = bottom;
        zigZag.IsValid = true;
        zigZag.Dominant = true;
        zigZag.Percentage = 100 * ((zigZag.Top - zigZag.Bottom) / zigZag.Bottom);
        //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot at {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");

        // Borrow the wick from the neighbour if higher or lower
        if (zigZag.PointType == 'L')
        {
            CryptoSymbolInterval symbolInterval = symbol!.GetSymbolInterval(interval!.IntervalPeriod);
            if (symbolInterval.CandleList.TryGetValue(zigZag.Candle.OpenTime + interval.Duration, out CryptoCandle? candle))
            {
                if (candle.Low < zigZag.Bottom)
                {
                    zigZag.Top = Math.Max(candle.Close, candle.Open);
                    zigZag.Bottom = candle.Low;
                }
            }
            if (symbolInterval.CandleList.TryGetValue(zigZag.Candle.OpenTime - interval.Duration, out candle))
            {
                if (candle.Low < zigZag.Bottom)
                {
                    zigZag.Top = Math.Max(candle.Close, candle.Open);
                    zigZag.Bottom = candle.Low;
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
                    zigZag.Top = candle.High;
                    zigZag.Bottom = Math.Min(candle.Close, candle.Open);
                }
            }
            if (symbolInterval.CandleList.TryGetValue(zigZag.Candle.OpenTime - interval.Duration, out candle))
            {
                if (candle.High > zigZag.Top)
                {
                    zigZag.Top = candle.High;
                    zigZag.Bottom = Math.Min(candle.Close, candle.Open);
                }
            }
        }
        if (zigZag.Top != top || zigZag.Bottom != bottom)
        {
            zigZag.Percentage = 100 * ((zigZag.Top - zigZag.Bottom) / zigZag.Bottom);
            //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot corrected {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
        }

        // Is the (unzoomed) percentage between the configured limits? 
        // Or is the percentage alread below the zoom-limit? (saves time)
        if (UnzoomedPercentageBelowMinimum(zigZag)
            || UnzoomedPercentageAboveMaximum(zigZag)
            || ZoomedPercentageBelowMinimum(zigZag))
        {
            zigZag.IsValid = false;
            return; // (mark the point as not dominant + exit)
        }



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
                //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot zooming {zoom} {zigZag.Percentage:N2}%");

                // Is Interval supported by Exchange
                CryptoSymbolInterval zoomInterval = symbol!.GetSymbolInterval(zoom);
                if (symbol.Exchange.IsIntervalSupported(zoomInterval.IntervalPeriod))
                {
                    // Load candles from disk
                    if (!loadedCandlesInMemory.TryGetValue(zoomInterval.IntervalPeriod, out bool _))
                        await CandleEngine.LoadCandleDataFromDiskAsync(symbol, zoomInterval.Interval, zoomInterval.CandleList);
                    loadedCandlesInMemory.TryAdd(zoomInterval.IntervalPeriod, false); // in memory, nothing changed

                    // Load candles from the exchange
                    int count = durationForThisCandle / zoomInterval.Interval.Duration;
                    if (await CandleEngine.FetchFrom(symbol, zoomInterval.Interval, zoomInterval.CandleList, unixStart, count))
                        loadedCandlesInMemory[zoomInterval.Interval.IntervalPeriod] = true;

                    long loop = IntervalTools.StartOfIntervalCandle(unixStart, zoomInterval.Interval.Duration);
                    while (loop < unixEinde && zigZag.Percentage >= GlobalData.Settings.Signal.Zones.MaximumZoomedPercentage)
                    {
                        //DateTime loopDebug = CandleTools.GetUnixDate(loop);
                        if (loop >= zigZag.Candle.OpenTime) // really?
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
                                            //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot zoomed {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2} {zoomInterval.Interval.Name}");
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
                                            //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot zoomed {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2} {zoomInterval.Interval.Name}");
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
        if (ZoomedPercentageBelowMinimum(zigZag) || ZoomedPercentageAboveMaximum(zigZag))
        {
            zigZag.IsValid = false;
            return; // (mark the point as not dominant + exit)
        }
    }

    public static async Task CalculateLiqBoxesAsync(AddTextEvent? sender, CryptoZoneData data, bool zoomLiqBoxes, SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        GlobalData.AddTextToLogTab($"{data.Symbol.Name} Calculating zones");
        ZigZagResult? previous = null;
        ZigZagResult? previous2 = null;
        foreach (var zigZag in data.Indicator.ZigZagList)
        {
            if (previous != null && previous2 != null && !zigZag.Dummy)
            {
                sender?.Invoke($"Calculating Bybit.Spot.{data.Symbol.Name} {zigZag.Candle.Date}");

                // Check: a dominant Low leading to a new Higher High
                if (zigZag.PointType == 'H' && previous.PointType == 'L' && previous2.PointType == 'H' && previous2.Value < zigZag.Value)
                    await MakeDominantAndZoomInAsync(data.Symbol, data.SymbolInterval.Interval, previous,
                        Math.Max(previous.Candle.Open, previous.Candle.Close), previous.Candle.Low, zoomLiqBoxes, loadedCandlesInMemory);

                // Check: a dominant High leading to a new Lower Low
                if (zigZag.PointType == 'L' && previous.PointType == 'H' && previous2.PointType == 'L' && previous2.Value > zigZag.Value)
                    await MakeDominantAndZoomInAsync(data.Symbol, data.SymbolInterval.Interval, previous,
                        previous.Candle.High, Math.Min(previous.Candle.Open, previous.Candle.Close), zoomLiqBoxes, loadedCandlesInMemory);
            }
            previous2 = previous;
            previous = zigZag;
        }
    }


    //public static void CalculateBrokenBoxes(CryptoZoneData data)
    //{
    //    // Determine if a liq. box/zone has been broken
    //    ZigZagResult? prevZigZag = null;
    //    //ScannerLog.Logger.Info($"{data.Symbol.Name} Start marking broken zones");
    //    //GlobalData.AddTextToLogTab($"{data.Symbol.Name} Start marking broken zones");
    //    foreach (var zigZag in data.Indicator.ZigZagList)
    //    {
    //        if (prevZigZag == null)
    //            zigZag.CloseDate = zigZag.Candle.OpenTime; //Just to show it..
    //        else
    //        {
    //            if (zigZag.Dominant && !zigZag.Dummy && zigZag.IsValid) // all zones (also the closed ones)
    //            {
    //                bool brokenBos = false;
    //                long key = zigZag.Candle.OpenTime;
    //                long checkUpTo = CandleTools.GetUnixTime(DateTime.UtcNow, data.SymbolInterval.Interval.Duration);
    //                while (key <= checkUpTo)
    //                {
    //                    key += data.SymbolInterval.Interval.Duration;
    //                    if (data.SymbolInterval.CandleList.TryGetValue(key, out CryptoCandle? candle))
    //                    {
    //                        // We need a BOS before we can invalidate the liq.box
    //                        if (!brokenBos)
    //                        {
    //                            if (zigZag.PointType == 'H' && candle.GetLowValue(false) <= prevZigZag.Value)
    //                                brokenBos = true;
    //                            if (zigZag.PointType == 'L' && candle.GetHighValue(false) >= prevZigZag.Value)
    //                                brokenBos = true;
    //                        }
    //                        else
    //                        {
    //                            if (zigZag.PointType == 'H' && candle.High > zigZag.Bottom)
    //                            {
    //                                zigZag.CloseDate = candle.OpenTime;
    //                                break;
    //                            }
    //                            if (zigZag.PointType == 'L' && candle.Low < zigZag.Top)
    //                            {
    //                                zigZag.CloseDate = candle.OpenTime;
    //                                break;
    //                            }
    //                        }
    //                    }
    //                }
    //            }
    //        }
    //        prevZigZag = zigZag;
    //    }
    //}

    public static void CheckZones(CryptoZoneData data, ref long key, long checkUpTo, long delay, List<ZigZagResult> zonesLong, List<ZigZagResult> zonesShort)
    {
        // public because of testing (yeah, q&d)

        while (key <= checkUpTo)
        {
            if (data.SymbolInterval.CandleList.TryGetValue(key, out CryptoCandle? candle))
            {
                // Note: A candle can break multiple long or short boxes

                foreach (var x in zonesLong)
                {
                    if (key > x.Candle.OpenTime + delay && candle.Low < x.Top)
                    {
                        x.CloseDate = candle.OpenTime;
                        zonesLong.Remove(x);
                        break;
                    }
                }
                foreach (var x in zonesShort)
                {
                    if (key > x.Candle.OpenTime + delay && candle.High > x.Bottom)
                    {
                        x.CloseDate = candle.OpenTime;
                        zonesShort.Remove(x);
                        break;
                    }
                }
            }
            key += data.SymbolInterval.Interval.Duration;
        }
    }

    public static void CalculateBrokenBoxes(CryptoZoneData data)
    {
        List<ZigZagResult> zonesLong = [];
        List<ZigZagResult> zonesShort = [];

        long delay = 6 * data.SymbolInterval.Interval.Duration;
        long maxTime = CandleTools.GetUnixTime(DateTime.UtcNow, 60);

        if (data.Indicator.ZigZagList.Count > 0)
        {
            // brute force, this is going to take a lot of iterations..
            int last = data.Indicator.ZigZagList.Count - 1;
            long key = data.Indicator.ZigZagList.First().Candle.OpenTime + delay;

            for (int i = 0; i <= last; i++)
            {
                var zigZag = data.Indicator.ZigZagList[i];

                if (zigZag.Dominant && !zigZag.Dummy) // all zones (also the closed ones) //  && zigZag.IsValid
                {
                    // The zones are growing as we iterate, broken zones will be removed to keep the list small
                    if (zigZag.PointType == 'L')
                        zonesLong.Add(zigZag);
                    else
                        zonesShort.Add(zigZag);

                    long checkUpTo;
                    if (i < last)
                        checkUpTo = zigZag.Candle.OpenTime;
                    else
                        checkUpTo = maxTime;

                    CheckZones(data, ref key, checkUpTo, delay, zonesLong, zonesShort);
                }
                else
                {
                    // Close it just to be sure..
                    zigZag.CloseDate = zigZag.Candle.OpenTime;
                }
            }
            CheckZones(data, ref key, maxTime, delay, zonesLong, zonesShort);
        }
    }

    internal static void CalculateIntroZone(CryptoZoneData data)
    {
        // Determine if a liq. box/zone has an interesting intro
        if (GlobalData.Settings.Signal.Zones.ZoneStartApply)
        {
            foreach (var zigZag in data.Indicator.ZigZagList)
            {
                if (zigZag.Dominant && !zigZag.Dummy) //  && zigZag.IsValid all zones (also the closed ones)
                {
                    decimal minPrice = decimal.MaxValue;
                    decimal maxPrice = decimal.MinValue;
                    long max = zigZag.Candle.OpenTime;
                    long min = max - GlobalData.Settings.Signal.Zones.ZoneStartCandleCount * data.Interval.Duration;
                    while (min <= max)
                    {
                        if (data.SymbolInterval.CandleList.TryGetValue(min, out CryptoCandle? candle))
                        {
                            if (candle.Low < minPrice)
                                minPrice = candle.Low;
                            if (candle.High > maxPrice)
                                maxPrice = candle.High;
                        }
                        min += data.SymbolInterval.Interval.Duration;
                    }

                    if (minPrice != decimal.MaxValue)
                    {
                        decimal diff = maxPrice - minPrice;
                        decimal perc = 100 * diff / minPrice;
                        if (perc >= GlobalData.Settings.Signal.Zones.ZoneStartPercentage)
                        {
                            zigZag.NiceIntro = $"{perc:N2}";
                        }
                        else
                            zigZag.NiceIntro = "";
                    }
                }
            }
        }   
    }
}
