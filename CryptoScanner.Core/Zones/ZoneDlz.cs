using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

using Dapper;

namespace CryptoScanner.Core.Zones;

// DLZ - Dominant Liquidity ZonesDlz

public class ZoneDlz
{

    public static void LoadAllZones()
    {
        foreach (var symbol in GlobalData.ActiveExchange!.SymbolListName.Values.ToList())
        {
            symbol.Data.ResetFvgData();
            symbol.Data.ResetDlzData();
            symbol.Data.ResetSmcData();
            symbol.Data.ResetTrendData();
        }

        using var database = new CryptoDatabase();
        string sql = "select * from zone where exchangeid=exchangeid and CloseTime is null order by OpenTime";
        foreach (CryptoZone zone in database.Connection.Query<CryptoZone>(sql, new { exchangeid = GlobalData.ActiveExchange!.Id }))
        {
            PutZoneInMemory(zone);
        }
    }


    public static void LoadZonesForSymbol(CryptoSymbol symbol)
    {
        CryptoSymbolData symbolData = symbol.Data;
        symbolData.ResetFvgData();
        symbolData.ResetDlzData();
        symbolData.ResetSmcData();
        symbolData.ResetTrendData();

        using var database = new CryptoDatabase();

        string sql = "select * from zone where SymbolId = @SymbolId order by OpenTime"; //and Kind=1
        foreach (CryptoZone zone in database.Connection.Query<CryptoZone>(sql, new { SymbolId = symbol.Id }))
        {
            PutZoneInMemory(zone);
        }
    }


    private static void PutZoneInMemory(CryptoZone zone)
    {
        if (GlobalData.ExchangeListId.TryGetValue(zone.ExchangeId, out Model.CryptoExchange? exchange))
        {
            zone.Exchange = exchange;
            if (exchange.SymbolListId.TryGetValue(zone.SymbolId, out CryptoSymbol? symbol))
            {
                zone.Symbol = symbol;
                CryptoSymbolData symbolData = symbol.Data;

                if (GlobalData.IntervalListId.TryGetValue(zone.IntervalId, out CryptoInterval? interval))
                {
                    zone.Interval = interval;
                    var symbolInterval = symbolData.Get(interval.IntervalPeriod);

                    if (zone.Kind == CryptoZoneKind.FairValueGap)
                    {
                        symbolInterval.FvgZones.Add(zone);
                    }
                    else if (zone.Kind == CryptoZoneKind.OrderBlock)
                    {
                        // SMC zones use a flat list; TouchCount/IsMitigated are recomputed by
                        // the next ZoneSmc.Detect (they are [Computed], not persisted).
                        symbolInterval.SmcZones.Add(zone);
                    }
                    else
                    {
                        symbolInterval.DlzZones.Add(zone);

                        // Creation date is the date of the last swing point (SH/SL)
                        // TODO: The last swing low and high are now extracted from the boundaries of the zone, that is not 100% correct
                        CandleTime timeLastSwingPoint = zone.OpenTime;
                        if (symbolInterval.DlzAdmin.TimeLastSwingPoint == null || timeLastSwingPoint > symbolInterval.DlzAdmin.TimeLastSwingPoint)
                        {
                            symbolInterval.DlzAdmin.TimeLastSwingPoint = timeLastSwingPoint;
                            if (symbolInterval.DlzAdmin.LastSwingLow == null || zone.Bottom > symbolInterval.DlzAdmin.LastSwingLow)
                                symbolInterval.DlzAdmin.LastSwingLow = zone.Bottom;
                            if (symbolInterval.DlzAdmin.LastSwingHigh == null || zone.Top > symbolInterval.DlzAdmin.LastSwingHigh)
                                symbolInterval.DlzAdmin.LastSwingHigh = zone.Top;
                        }
                    }
                }
            }
        }
    }


    private static void CreateZonesFromZigZag(CryptoSymbol symbol, CryptoInterval interval,
        List<ZigZagResult> zigZagList, List<CryptoZone> zones)
    {
        foreach (var zigZag in zigZagList)
        {
            if (zigZag.Dominant && !zigZag.Dummy) //  && zigZag.IsValid all newZones (also the closed ones)
            {
                CryptoZone zone = new()
                {
                    Kind = CryptoZoneKind.DominantLevel,
                    Strength = zigZag.Strength, // depending on the percentage of the zone "intro"
                    ExchangeId = symbol.Exchange.Id,
                    Exchange = symbol.Exchange,
                    SymbolId = symbol.Id,
                    Symbol = symbol,
                    IntervalId = interval.Id,
                    Interval = interval,
                    OpenTime = zigZag.Candle.OpenTime,
                    Top = zigZag.Top,
                    Bottom = zigZag.Bottom,
                    Side = zigZag.PointType == 'L' ? CryptoTradeSide.Long : CryptoTradeSide.Short,
                    IsValid = zigZag.IsValid,
                    CloseTime = zigZag.CloseDate,
                    Description = $"{interval.Name}: {zigZag.Percentage:N2}% {zigZag.NiceIntro}",
                };
                zones.Add(zone);
            }
        }
    }


    //private static void CombineAndSaveZones(CryptoSymbol symbol, CryptoInterval interval,
    //    List<ZigZagResult> zigZagList, DatabaseStatistics statistics)
    //{
    //}


    private static bool UnzoomedPercentageBelowMinimum(ZigZagResult zigZag)
    {
        if (GlobalData.Settings.Signal.ZonesDlz.ZonesApplyUnzoomed)
        {
            var value = GlobalData.Settings.Signal.ZonesDlz.MinimumUnZoomedPercentage;
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
        if (GlobalData.Settings.Signal.ZonesDlz.ZonesApplyUnzoomed)
        {
            var value = GlobalData.Settings.Signal.ZonesDlz.MaximumUnZoomedPercentage;
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
        var value = GlobalData.Settings.Signal.ZonesDlz.MinimumZoomedPercentage;
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
        var value = GlobalData.Settings.Signal.ZonesDlz.MaximumZoomedPercentage;
        if (value > 0 && zigZag.Percentage > value)
        {
            zigZag.Dominant = false;
            //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Zoomed box ignored {zigZag.Percentage:N2} > {value:N2} {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");
            return true;
        }
        return false;
    }


    public static async Task MakeDominantAndZoomInAsync(CryptoSymbol symbol, CryptoInterval interval,
        ZigZagResult zigZag, decimal top, decimal bottom,
        SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        zigZag.Top = top;
        zigZag.Bottom = bottom;
        zigZag.IsValid = true;
        zigZag.Dominant = true;
        zigZag.Percentage = (double)(100 * ((zigZag.Top - zigZag.Bottom) / zigZag.Bottom));
        //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot at {zigZag.Candle.DateLocal} {zigZag.PointType} top {zigZag.Top} bottom {zigZag.Bottom} perc={zigZag.Percentage:N2}");

        // Borrow the wick from the neighbour if higher or lower
        if (zigZag.PointType == 'L')
        {
            CryptoSymbolInterval symbolInterval = symbol!.GetSymbolInterval(interval!.IntervalPeriod);
            if (symbolInterval.CandleList.TryGetValue(zigZag.Candle.OpenTime + interval.Duration, out CryptoCandle candle))
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
            if (symbolInterval.CandleList.TryGetValue(zigZag.Candle.OpenTime + interval.Duration, out CryptoCandle candle))
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
            zigZag.Percentage = (double)(100 * ((zigZag.Top - zigZag.Bottom) / zigZag.Bottom));
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
        if (GlobalData.Settings.Signal.ZonesDlz.ZoomLowerTimeFrames && zigZag.Percentage >= GlobalData.Settings.Signal.ZonesDlz.MaximumZoomedPercentage)
        {
            CryptoIntervalPeriod zoom = interval!.IntervalPeriod;
            CandleTime unixStart = zigZag.Candle.OpenTime;
            CandleTime unixEinde = zigZag.Candle.OpenTime + interval.Duration;
            //DateTime unixStartDebug = CandleTools.GetUnixDate(unixStart);
            //DateTime unixEindeDebug = CandleTools.GetUnixDate(unixEinde);

            while (zigZag.Percentage >= GlobalData.Settings.Signal.ZonesDlz.MaximumZoomedPercentage && zoom > CryptoIntervalPeriod.interval1m)
            {
                zoom--;
                //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot zooming {zoom} {zigZag.Percentage:N2}%");

                // Is IntervalList supported by Exchange
                CryptoSymbolInterval zoomInterval = symbol!.GetSymbolInterval(zoom);
                //if (symbol.Exchange.IsIntervalSupported(zoomInterval.IntervalPeriod))
                {
                    //// Load candles from disk if needed
                    //if (!loadedCandlesInMemory.TryGetValue(zoomInterval.IntervalPeriod, out bool _))
                    //    await ZoneCandleEngine.ReadCandlesFromDiskAsync(symbol, zoomInterval.Interval);
                    //loadedCandlesInMemory.TryAdd(zoomInterval.IntervalPeriod, true); // in memory, alway's save

                    //// Load candles from the exchange if needed
                    //int count = interval.Duration / zoomInterval.Interval.Duration;
                    //if (await ZoneCandleEngine.FetchFrom(symbol, zoomInterval.Interval, unixStart, count))
                    //    loadedCandlesInMemory[zoomInterval.Interval.IntervalPeriod] = true; // in memory, alway's save

                    int count = (int)interval.Duration / (int)zoomInterval.Interval.Duration;
                    await ZoneCandleEngine.FetchFrom(loadedCandlesInMemory, symbol, zoomInterval.Interval, unixStart, count);

                    CandleTime loop = IntervalTools.StartOfIntervalCandle(unixStart, zoomInterval.Interval.Duration);
                    while (loop < unixEinde && zigZag.Percentage >= GlobalData.Settings.Signal.ZonesDlz.MaximumZoomedPercentage)
                    {
                        //DateTime loopDebug = CandleTools.GetUnixDate(loop);
                        if (loop >= zigZag.Candle.OpenTime) // really?
                        {
                            if (zoomInterval.CandleList.TryGetValue(loop, out CryptoCandle candle))
                            {
                                if (zigZag.PointType == 'L')
                                {
                                    decimal bodyTop = Math.Max(candle.Open, candle.Close);
                                    if (bodyTop < zigZag.Top)
                                    {
                                        double percentage = (double)(100 * ((bodyTop - zigZag.Bottom) / zigZag.Bottom));
                                        if (percentage >= GlobalData.Settings.Signal.ZonesDlz.MinimumUnZoomedPercentage)
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
                                        double percentage = (double)(100 * ((zigZag.Top - bodyBottom) / bodyBottom));
                                        if (percentage >= GlobalData.Settings.Signal.ZonesDlz.MinimumUnZoomedPercentage)
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

                if (zigZag.Percentage <= GlobalData.Settings.Signal.ZonesDlz.MaximumZoomedPercentage)
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

    public static async Task CalculateDlzAsync(AddTextEvent? sender,
        CryptoSymbol symbol, CryptoInterval interval, ZigZagIndicator indicator,
        SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        //GlobalData.AddTextToLogTab($"{data.Symbol.Name} Calculating newZones");

        ZigZagResult? previous = null;
        ZigZagResult? previous2 = null;
        foreach (var zigZag in indicator.ZigZagList)
        {
            if (previous != null && previous2 != null && !zigZag.Dummy)
            {
                sender?.Invoke($"Calculating dlz zones {symbol.Exchange.Name} {symbol.Name} {interval.Name} {zigZag.Candle.Date}");

                // Check: a dominant Low leading to a new Higher High
                if (zigZag.PointType == 'H' && previous.PointType == 'L' && previous2.PointType == 'H' && previous2.Value < zigZag.Value)
                    await MakeDominantAndZoomInAsync(symbol, interval, previous,
                        Math.Max(previous.Candle.Open, previous.Candle.Close), previous.Candle.Low, loadedCandlesInMemory);

                // Check: a dominant High leading to a new Lower Low
                if (zigZag.PointType == 'L' && previous.PointType == 'H' && previous2.PointType == 'L' && previous2.Value > zigZag.Value)
                    await MakeDominantAndZoomInAsync(symbol, interval, previous,
                        previous.Candle.High, Math.Min(previous.Candle.Open, previous.Candle.Close), loadedCandlesInMemory);
            }
            previous2 = previous;
            previous = zigZag;
        }
    }


    internal static void CalculateIntroZone(CryptoSymbol symbol, CryptoInterval interval, ZigZagIndicator indicator)
    {
        // Determine if a liq. box/zone has an interesting intro
        if (GlobalData.Settings.Signal.ZonesDlz.ZoneStartApply)
        {
            //var trendZigZagIndicator = data.IndicatorList[(session.TrendType, session.UseHighLow)];

            ZigZagResult? previous = null;
            foreach (var zigZag in indicator.ZigZagList)
            {
                if (previous != null)
                {
                    if (zigZag.Dominant && !zigZag.Dummy) //  && zigZag.IsValid all newZones (also the closed ones)
                    {
                        decimal boxLimit;
                        if (zigZag.PointType == 'L')
                            boxLimit = zigZag.Bottom;
                        else
                            boxLimit = zigZag.Top;
                        decimal price = boxLimit;

                        var symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
                        CandleTime max = zigZag.Candle.OpenTime;
                        CandleTime min = max - GlobalData.Settings.Signal.ZonesDlz.ZoneStartCandleCount * interval.Duration;
                        // Is it on the right of the last zigzag point?
                        if (min < previous.Candle.OpenTime)
                            min = previous.Candle.OpenTime;
                        while (min < max)
                        {
                            if (symbolInterval.CandleList.TryGetValue(min, out CryptoCandle candle))
                            {
                                if (zigZag.PointType == 'L')
                                {
                                    if (candle.High > price)
                                        price = candle.High;
                                }
                                else
                                {
                                    if (candle.Low < price)
                                        price = candle.Low;
                                }
                            }
                            min += interval.Duration;
                        }


                        double perc = (double)(100 * Math.Abs(boxLimit - price) / Math.Min(boxLimit, price));
                        //zigZag.NiceIntro = $"\n\r(intro {perc:N2}%)";
                        //zigZag.NiceIntro += $"\n\r{price}\n\r{boxLimit}";

                        if (perc <= GlobalData.Settings.Signal.ZonesDlz.ZoneStartPercentage)
                            zigZag.Strength = CryptoZoneStrength.Weak;
                        else
                            zigZag.Strength = CryptoZoneStrength.Strong;
                    }
                }
                previous = zigZag;
            }
        }
    }


    public static async Task<(CandleTime minDate, CandleTime maxDate)> LoadHistoricCandles(CryptoSymbol symbol, CryptoInterval interval,
        SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        // Determine the period (using the candlecount)
        int candleFetchCount = GlobalData.Settings.Signal.ZonesDlz.CandleCount;
        CandleTime maxDate = CandleTime.AlignFromDateTime(DateTime.UtcNow, interval.Duration);
        CandleTime minDate = maxDate - candleFetchCount * interval.Duration;
        await ZoneCandleEngine.FetchFrom(loadedCandlesInMemory, symbol, interval, minDate, candleFetchCount);

#if DEBUG
        var count = symbol.GetSymbolInterval(interval).CandleList.Count;
        GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} dlz from {minDate.ToLocalTime()} .. {maxDate.ToLocalTime()} candles = {count}");
#endif
        return (minDate, maxDate);
    }


    public static async Task CalculatePivots(CryptoSymbol symbol, CryptoInterval interval,
        CandleTime minDate, CandleTime maxDate, TrendZigZagIndicatorList trendZigZagIndicatorList)
    {
        await symbol.Data.CandleLock.WaitAsync();
        try
        {
            var candleList = symbol.GetSymbolInterval(interval.IntervalPeriod).CandleList;

            // Calculate "indicators"
            CandleTime loop = minDate;
            while (loop <= maxDate)
            {
                if (candleList.TryGetValue(loop, out CryptoCandle candle))
                {
                    foreach (var trendZigZagIndicator in trendZigZagIndicatorList.Values)
                    {
                        trendZigZagIndicator.Calculate(candle, true);
                    }
                }
                loop += interval.Duration;
            }


            // Remember the last swing point for the automatic zone calculation
            CryptoSymbolInterval symbolIntervalData = symbol.Data.Get(interval.IntervalPeriod);

            var trend = GlobalData.Settings.Signal.ZonesDlz.ZigZag;
            var indicator = trendZigZagIndicatorList[(trend.TrendType, trend.UseHighLow)];
            if (indicator.LastSwingPoint != null)
                symbolIntervalData.DlzAdmin.TimeLastSwingPoint = indicator.LastSwingPoint.Candle.OpenTime;
            if (indicator.LastSwingLow != null)
                symbolIntervalData.DlzAdmin.LastSwingLow = indicator.LastSwingLow.Value;
            if (indicator.LastSwingHigh != null)
                symbolIntervalData.DlzAdmin.LastSwingHigh = indicator.LastSwingHigh.Value;

            foreach (var indicatorX in trendZigZagIndicatorList.Values)
                indicatorX.FinishBatch();
        }
        finally
        {
            symbol.Data.CandleLock.Release();
        }

    }

    private static void CheckAndMarkBrokenZones(CryptoInterval interval,
        CryptoCandleList candleList, CryptoSymbolIntervalZones zones)
    {
        var oldest1 = zones.LongOpen.MinBy(z => z.OpenTime);
        var oldest2 = zones.ShortOpen.MinBy(z => z.OpenTime);

        CandleTime? startTime = null;
        if (oldest1 != null) startTime = oldest1.OpenTime;
        if (oldest2 != null && (startTime == null || oldest2.OpenTime < startTime))
            startTime = oldest2.OpenTime;

        if (startTime != null && candleList.Count > 0)
        {
            // Loosened invalidation: a wick into the zone counts as a test (TouchCount++),
            // only a body-close through the far side or reaching MaxTouches closes the zone.
            // See ZoneInvalidation for the theoretical background.
            int maxTouches = GlobalData.Settings.Signal.ZonesDlz.MaxTouches;

            CandleTime loop = startTime.Value;
            CandleTime loopEnd = candleList.Keys.Last();

            while (loop <= loopEnd)
            {
                if (candleList.TryGetValue(loop, out CryptoCandle candle))
                {
                    List<CryptoZone> closed = [];
                    //CheckBrokenZones(zones, candle, touched);

                    // LongOpen sorted descending by Top. Stop as soon as candle.Low >= zone.Top
                    if (zones.LongOpen.Count > 0) //&& candle.Low < zones.LongOpen[0].Top
                    {
                        foreach (var zone in zones.LongOpen)
                        {
                            if (candle.Low >= zone.Top)
                                break;
                            if (candle.OpenTime >= zone.OpenTime + zone.Interval.Duration)
                            {
                                //if (candle.High <= zone.Bottom)
                                //    touched.Add(zone);
                                //if (candle.Low < zone.Top)
                                //    touched.Add(zone);
                                if (ZoneInvalidation.ApplyToCandle(zone, candle, interval, maxTouches)
                                    && zone.CloseTime == candle.OpenTime + interval.Duration)
                                {
                                    closed.Add(zone);
                                }
                            }
                        }
                    }


                    // ShortOpen sorted ascending by Bottom. Stop as soon as candle.High <= zone.Bottom
                    if (zones.ShortOpen.Count > 0) //&& candle.High > zones.ShortOpen[0].Bottom
                    {
                        foreach (var zone in zones.ShortOpen)
                        {
                            if (candle.High <= zone.Bottom)
                                break;
                            if (candle.OpenTime >= zone.OpenTime + zone.Interval.Duration)
                            {
                                // Close old invalid zone without notifications..
                                //if (candle.Low >= zone.Top)
                                //    touched.Add(zone);
                                //if (candle.High > zone.Bottom)
                                //    touched.Add(zone);
                                if (ZoneInvalidation.ApplyToCandle(zone, candle, interval, maxTouches)
                                    && zone.CloseTime == candle.OpenTime + interval.Duration)
                                {
                                    closed.Add(zone);
                                }
                            }
                        }
                    }


                    // Move newly closed zones to the closed list; CloseTime was set by ApplyToCandle
                    // and is picked up by AddZonesToInternalLists for DB save.
                    foreach (var zone in closed)
                    {
                        if (zone.Side == CryptoTradeSide.Long)
                        {
                            zones.LongOpen.Remove(zone);
                            zones.LongClosed.Add(zone);
                        }
                        else
                        {
                            zones.ShortOpen.Remove(zone);
                            zones.ShortClosed.Add(zone);
                        }
                    }

                    // Early exit when all zones are closed
                    if (zones.LongOpen.Count + zones.ShortOpen.Count == 0)
                        break;
                }
                loop += interval.Duration;
            }
        }
    }


    public static async Task CalculateZonesAsync(AddTextEvent? sender,
        CryptoSymbol symbol, CryptoInterval interval,
        SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        try
        {
            var (minDate, maxDate) = await LoadHistoricCandles(symbol, interval, loadedCandlesInMemory);

            // Mark the dominant lows or highs
            //if (forceCalculation)
            {
                CryptoSymbolData symbolData = symbol.Data;
                var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);
                CryptoSymbolIntervalZones zones = symbolIntervalData.DlzZones;

                //if (symbol.Name == "1000PEPEUSDT")
                //    GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} " +
                //        $"{minDate.ToLocalTime():yyyy-MM-dd HH:mm} .. {maxDate.ToLocalTime():yyyy-MM-dd HH:mm} " +
                //        $"dlz zones long = {zones.LongOpen.Count} " +
                //        $"dlz zones short = {zones.ShortOpen.Count} ");

                var trend = GlobalData.Settings.Signal.ZonesDlz.ZigZag;
                TrendZigZagIndicatorList trendZigZagIndicatorList = [];
                trendZigZagIndicatorList.Add((trend.TrendType, trend.UseHighLow), new(trend.TrendType, trend.UseHighLow, 1.0m));
                var trendZigZagIndicator = trendZigZagIndicatorList[(trend.TrendType, trend.UseHighLow)];

                await CalculatePivots(symbol, interval, minDate, maxDate, trendZigZagIndicatorList);
                await CalculateDlzAsync(sender, symbol, interval, trendZigZagIndicator, loadedCandlesInMemory);
                CalculateIntroZone(symbol, interval, trendZigZagIndicator);

                // Index old zones for DB merge (must happen before zones are rebuilt)
                DatabaseStatistics statistics = new();
                SortedList<(CryptoTradeSide, CandleTime?, decimal, decimal), CryptoZone> oldZones = [];
                ZoneTools.CreateZoneIndex(zones.LongOpen, oldZones, statistics);
                ZoneTools.CreateZoneIndex(zones.ShortOpen, oldZones, statistics);
                ZoneTools.CreateZoneIndex(zones.LongClosed, oldZones, statistics);
                ZoneTools.CreateZoneIndex(zones.ShortClosed, oldZones, statistics);

                // Create new zones from the zigzag (local list, never visible to other threads)
                List<CryptoZone> newZones = [];
                CreateZonesFromZigZag(symbol, interval, trendZigZagIndicator.ZigZagList, newZones);

                // Sorted temp object for broken-zone detection — still local, not the live reference
                CryptoSymbolIntervalZones tempZones = new();
                foreach (var zone in newZones)
                    tempZones.Add(zone);

                // Check broken zones before DB comparison so CloseTime is set correctly on zone objects
                CheckAndMarkBrokenZones(interval, symbolIntervalData.CandleList, tempZones);

                // Merge with DB state into a fresh object, then atomically replace the live reference.
                // Other threads always see either the old complete object or the new complete one —
                // never a half-built list (which was the source of null holes in OrderedList).
                CryptoSymbolIntervalZones finalZones = new();
                ZoneTools.AddZonesToInternalLists(finalZones, oldZones, newZones, statistics);
                ZoneTools.DeleteRemainingZones(oldZones, statistics);
                symbolIntervalData.DlzZones = finalZones;

                if (statistics.Untouched != statistics.Total)
                {
                    var count = symbol.GetSymbolInterval(interval).CandleList.Count;
                    GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} " +
                        $"mindate = {minDate.ToLocalTime():yyyy-MM-dd HH:mm}, " +
                        $"maxdate = {maxDate.ToLocalTime():yyyy-MM-dd HH:mm} " +
                        $"Candles = {count}," +
                        $"Zones calculated ({trend.TrendType}, {trend.UseHighLow}), " +
                        $"inserted={statistics.Inserted} " +
                        $"modified={statistics.Modified} deleted={statistics.Deleted} " +
                        $"untouched={statistics.Untouched} total={statistics.Total}");
                }
            }


            //GlobalData.AddTextToLogTab($"{data.Symbol.Name} points={data.Indicator.PivotList.Count} fib.points={data.IndicatorFib.PivotList.Count}");
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Info($"ERROR {error}");
            GlobalData.AddTextToLogTab($"ERROR {error}");
        }
    }

}
