using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

using Dapper;

namespace CryptoScanBot.Core.Zones;

// DLZ - Dominant Liquidity ZonesDlz 

public class ZoneDlz
{

    public static void LoadAllZones()
    {
        foreach (var symbol in GlobalData.ActiveExchange!.SymbolListName.Values.ToList())
        {
            symbol.Data.ResetFvgData();
            symbol.Data.ResetDlzData();
            symbol.Data.ResetTrendData();
        }

        using var database = new CryptoDatabase();
        string sql = "select * from zone where exchangeid=exchangeid and CloseTime is null order by CreateTime";
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
        symbolData.ResetTrendData();

        using var database = new CryptoDatabase();

        string sql = "select * from zone where SymbolId = @SymbolId order by CreateTime"; //and Kind=1 
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
                    else
                    {
                        symbolInterval.DlzZones.Add(zone);

                        // Creation date is the date of the last swing point (SH/SL)
                        // TODO: The last swing low and high are now extracted from the boundaries of the zone, that is not 100% correct
                        long timeLastSwingPoint = CandleTools.GetUnixTime(zone.CreateTime, 0);
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


    private static void CreateZonesFromZigZag(ZoneConfig data, List<ZigZagResult> zigZagList, List<CryptoZone> zones)
    {
        foreach (var zigZag in zigZagList)
        {
            if (zigZag.Dominant && !zigZag.Dummy) //  && zigZag.IsValid all newCreatedZones (also the closed ones)
            {
                CryptoZone zone = new()
                {
                    Kind = CryptoZoneKind.DominantLevel,
                    Strength = zigZag.Strength, // depending on the percentage of the zone "intro"
                    CreateTime = zigZag.Candle.Date,
                    ExchangeId = data.Symbol.Exchange.Id,
                    Exchange = data.Symbol.Exchange,
                    SymbolId = data.Symbol.Id,
                    Symbol = data.Symbol,
                    IntervalId = data.Interval.Id,
                    Interval = data.Interval,
                    OpenTime = zigZag.Candle.OpenTime,
                    Top = zigZag.Top,
                    Bottom = zigZag.Bottom,
                    Side = zigZag.PointType == 'L' ? CryptoTradeSide.Long : CryptoTradeSide.Short,
                    IsValid = zigZag.IsValid,
                    CloseTime = zigZag.CloseDate,
                    Description = $"{data.Interval.Name}: {zigZag.Percentage:N2}% {zigZag.NiceIntro}",
                };
                zones.Add(zone);
            }
        }
    }


    public static void SaveZonesForSymbol(ZoneConfig data, List<ZigZagResult> zigZagList, DatabaseStatistics dbStats)
    {
        // We are going to rebuild all the dlz lists
        var symbolData = data.Symbol.Data;
        var symbolIntervalData = symbolData.Get(data.Interval.IntervalPeriod);

        // Collect old zones
        SortedList<(CryptoTradeSide, long?, decimal, decimal), CryptoZone> zonesFromDatabase = [];
        ZoneTools.CreateZoneIndex(zonesFromDatabase, symbolIntervalData.DlzZones.LongOpen, dbStats);
        ZoneTools.CreateZoneIndex(zonesFromDatabase, symbolIntervalData.DlzZones.ShortOpen, dbStats);
        ZoneTools.CreateZoneIndex(zonesFromDatabase, symbolIntervalData.DlzZones.LongClosed, dbStats);
        ZoneTools.CreateZoneIndex(zonesFromDatabase, symbolIntervalData.DlzZones.ShortClosed, dbStats);
        symbolIntervalData.DlzZones.Reset();

        // Create new zones
        List<CryptoZone> newCreatedZones = [];
        CreateZonesFromZigZag(data, zigZagList, newCreatedZones);

        // Rebuild
        ZoneTools.AddZonesToInternalLists(symbolIntervalData.DlzZones, zonesFromDatabase, newCreatedZones, dbStats);
        ZoneTools.DeleteRemainingZones(zonesFromDatabase, dbStats);
    }


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
            long unixStart = zigZag.Candle.OpenTime;
            long unixEinde = zigZag.Candle.OpenTime + interval.Duration;
            //DateTime unixStartDebug = CandleTools.GetUnixDate(unixStart);
            //DateTime unixEindeDebug = CandleTools.GetUnixDate(unixEinde);

            while (zigZag.Percentage >= GlobalData.Settings.Signal.ZonesDlz.MaximumZoomedPercentage && zoom > CryptoIntervalPeriod.interval1m)
            {
                zoom--;
                //ScannerLog.Logger.Trace($"{symbol.Name} {interval.Name} Dominant pivot zooming {zoom} {zigZag.Percentage:N2}%");

                // Is IntervalList supported by Exchange
                CryptoSymbolInterval zoomInterval = symbol!.GetSymbolInterval(zoom);
                if (symbol.Exchange.IsIntervalSupported(zoomInterval.IntervalPeriod))
                {
                    // Load candles from disk if needed
                    if (!loadedCandlesInMemory.TryGetValue(zoomInterval.IntervalPeriod, out bool _))
                        await ZoneCandleEngine.LoadCandleDataFromDiskAsync(symbol, zoomInterval.Interval);
                    loadedCandlesInMemory.TryAdd(zoomInterval.IntervalPeriod, true); // in memory, alway's save

                    // Load candles from the exchange if needed
                    int count = interval.Duration / zoomInterval.Interval.Duration;
                    if (await ZoneCandleEngine.FetchFrom(symbol, zoomInterval.Interval, unixStart, count))
                        loadedCandlesInMemory[zoomInterval.Interval.IntervalPeriod] = true; // in memory, alway's save

                    long loop = IntervalTools.StartOfIntervalCandle(unixStart, zoomInterval.Interval.Duration);
                    while (loop < unixEinde && zigZag.Percentage >= GlobalData.Settings.Signal.ZonesDlz.MaximumZoomedPercentage)
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

    public static async Task CalculateDlzZonesAsync(AddTextEvent? sender, ZoneConfig data, ZigZagIndicator indicator, 
        SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        //GlobalData.AddTextToLogTab($"{data.Symbol.Name} Calculating newCreatedZones");

        ZigZagResult? previous = null;
        ZigZagResult? previous2 = null;
        foreach (var zigZag in indicator.ZigZagList)
        {
            if (previous != null && previous2 != null && !zigZag.Dummy)
            {
                sender?.Invoke($"Calculating dlz zones {data.Symbol.Exchange.Name} {data.Symbol.Name} {data.Interval.Name} {zigZag.Candle.Date}");

                // Check: a dominant Low leading to a new Higher High
                if (zigZag.PointType == 'H' && previous.PointType == 'L' && previous2.PointType == 'H' && previous2.Value < zigZag.Value)
                    await MakeDominantAndZoomInAsync(data.Symbol, data.SymbolInterval.Interval, previous,
                        Math.Max(previous.Candle.Open, previous.Candle.Close), previous.Candle.Low, loadedCandlesInMemory);

                // Check: a dominant High leading to a new Lower Low
                if (zigZag.PointType == 'L' && previous.PointType == 'H' && previous2.PointType == 'L' && previous2.Value > zigZag.Value)
                    await MakeDominantAndZoomInAsync(data.Symbol, data.SymbolInterval.Interval, previous,
                        previous.Candle.High, Math.Min(previous.Candle.Open, previous.Candle.Close), loadedCandlesInMemory);
            }
            previous2 = previous;
            previous = zigZag;
        }
    }


    public static void CheckZones(ZoneConfig data, ref long key, long checkUpTo, long delay, List<ZigZagResult> zonesLong, List<ZigZagResult> zonesShort)
    {
        while (key <= checkUpTo)
        {
            if (data.SymbolInterval.CandleList.TryGetValue(key, out CryptoCandle? candle))
            {
                // Note: A candle can break multiple long or short boxes

                foreach (var zigZag in zonesLong)
                {
                    if (key > zigZag.Candle.OpenTime + delay && candle.Low < zigZag.Top)
                    {
                        zigZag.CloseDate = candle.OpenTime + data.SymbolInterval.Interval.Duration;
                        zonesLong.Remove(zigZag);
                        break;
                    }
                }
                foreach (var zigZag in zonesShort)
                {
                    if (key > zigZag.Candle.OpenTime + delay && candle.High > zigZag.Bottom)
                    {
                        zigZag.CloseDate = candle.OpenTime + data.SymbolInterval.Interval.Duration;
                        zonesShort.Remove(zigZag);
                        break;
                    }
                }
            }
            key += data.SymbolInterval.Interval.Duration;
        }
    }

    
    public static void CalculateBrokenBoxes(ZoneConfig data, ZigZagIndicator indicator)
    {
        List<ZigZagResult> zonesLong = [];
        List<ZigZagResult> zonesShort = [];

        long delay = 4 * data.SymbolInterval.Interval.Duration; // TODO, not correct!
        long maxTime = CandleTools.GetUnixTime(DateTime.UtcNow, 60);

        if (indicator.ZigZagList.Count > 0)
        {
            // brute force, this is going to take a lot of iterations..
            int last = indicator.ZigZagList.Count - 1;
            long key = indicator.ZigZagList.First().Candle.OpenTime + delay;

            for (int i = 0; i <= last; i++)
            {
                var zigZag = indicator.ZigZagList[i];

                if (zigZag.Dominant && !zigZag.Dummy) // all newCreatedZones (also the closed ones) //  && zigZag.IsValid
                {
                    // The newCreatedZones are growing as we iterate, broken newCreatedZones will be removed to keep the list small
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
                    zigZag.CloseDate = zigZag.Candle.OpenTime + data.SymbolInterval.Interval.Duration;
                }
            }
            CheckZones(data, ref key, maxTime, delay, zonesLong, zonesShort);
        }
    }

    internal static void CalculateIntroZone(ZoneConfig data, ZigZagIndicator indicator)
    {
        // Determine if a liq. box/zone has an interesting intro
        if (GlobalData.Settings.Signal.ZonesDlz.ZoneStartApply)
        {
            //var indicator = data.IndicatorList[(session.TrendType, session.UseHighLow)];

            ZigZagResult? previous = null;
            foreach (var zigZag in indicator.ZigZagList)
            {
                if (previous != null)
                {
                    if (zigZag.Dominant && !zigZag.Dummy) //  && zigZag.IsValid all newCreatedZones (also the closed ones)
                    {
                        decimal boxLimit;
                        if (zigZag.PointType == 'L')
                            boxLimit = zigZag.Bottom;
                        else
                            boxLimit = zigZag.Top;
                        decimal price = boxLimit;

                        long max = zigZag.Candle.OpenTime;
                        long min = max - GlobalData.Settings.Signal.ZonesDlz.ZoneStartCandleCount * data.Interval.Duration;
                        // Is it on the right of the last zigzag point?
                        if (min < previous.Candle.OpenTime)
                            min = previous.Candle.OpenTime;
                        while (min < max)
                        {
                            if (data.SymbolInterval.CandleList.TryGetValue(min, out CryptoCandle? candle))
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
                            min += data.SymbolInterval.Interval.Duration;
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


    public static async Task CalculateDlzBoxesAsync(AddTextEvent? sender, ZoneSession session,
        ZoneConfig data, SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        try
        {
            // Determine dates
            long unixStartUp = CandleTools.GetUnixTime(DateTime.UtcNow, 0); // todo Emulator date?
            long fetchFrom = IntervalTools.StartOfIntervalCandle(unixStartUp, data.SymbolInterval.Interval.Duration);
            fetchFrom -= GlobalData.Settings.Signal.ZonesDlz.CandleCount * data.SymbolInterval.Interval.Duration;
            // Load candles from disk
            if (!loadedCandlesInMemory.TryGetValue(data.Interval.IntervalPeriod, out bool _))
                await ZoneCandleEngine.LoadCandleDataFromDiskAsync(data.Symbol, data.Interval);
            loadedCandlesInMemory.TryAdd(data.Interval.IntervalPeriod, true); // in memory, nothing zoneExistsInDatabase (save alway's)
                                                                              // Load candles from the exchange
            if (await ZoneCandleEngine.FetchFrom(data.Symbol, data.Interval, fetchFrom, GlobalData.Settings.Signal.ZonesDlz.CandleCount))
                loadedCandlesInMemory[data.Interval.IntervalPeriod] = true;
            if (data.SymbolInterval.CandleList.Count == 0)
                return;


            await data.Symbol.Data.CandleLock.WaitAsync();
            try
            {
                // Calculate indicators
                foreach (var candle in data.SymbolInterval.CandleList.Values)
                {
                    if (candle.OpenTime >= session.MinDate && candle.OpenTime <= session.MaxDate)
                    {
                        foreach (var indicatorX in data.IndicatorList.Values)
                        {
                            indicatorX.Calculate(candle, session.UseBatchProcess);
                        }
                    }
                }

                // Remember the last swing point for the automatic zone calculation
                CryptoSymbolData symbolData = data.Symbol.Data;
                CryptoSymbolInterval symbolIntervalData = symbolData.Get(data.Interval.IntervalPeriod);
                
                // TODO: This is kind of weird, WHAT indicator do we need for main!
                //foreach (var indicatorX in data.IndicatorList.Values)
                {
                    //var trend = session.TrendType == TrendType.Primary ? GlobalData.Settings.Trend.Primary : GlobalData.Settings.Trend.Secondary;
                    var trend = GlobalData.Settings.Signal.ZonesDlz.ZigZag;
                    var indicator = data.IndicatorList[(trend.TrendType, trend.UseHighLow)];
                    if (indicator.LastSwingPoint != null)
                        symbolIntervalData.DlzAdmin.TimeLastSwingPoint = indicator.LastSwingPoint.Candle.OpenTime;
                    if (indicator.LastSwingLow != null)
                        symbolIntervalData.DlzAdmin.LastSwingLow = indicator.LastSwingLow.Value;
                    if (indicator.LastSwingHigh != null)
                        symbolIntervalData.DlzAdmin.LastSwingHigh = indicator.LastSwingHigh.Value;
                }

                if (session.UseBatchProcess)
                {
                    foreach (var indicatorX in data.IndicatorList.Values)
                        indicatorX.FinishBatch();
                    //data.Indicator.FinishBatch();
                }
            }
            finally
            {
                data.Symbol.Data.CandleLock.Release();
            }


            // Mark the dominant lows or highs
            if (session.ForceCalculation)
            {
                var trend = GlobalData.Settings.Signal.ZonesDlz.ZigZag;
                //var trend = session.TrendType == TrendType.Primary? GlobalData.Settings.Trend.Primary : GlobalData.Settings.Trend.Secondary;
                var indicator = data.IndicatorList[(trend.TrendType, trend.UseHighLow)];
                await CalculateDlzZonesAsync(sender, data, indicator, loadedCandlesInMemory);
                CalculateIntroZone(data, indicator);
                CalculateBrokenBoxes(data, indicator);

                DatabaseStatistics dbStats = new();
                SaveZonesForSymbol(data, indicator.ZigZagList, dbStats);
                GlobalData.AddTextToLogTab($"{data.Symbol.Name} {data.Interval.Name} Zones calculated ({trend.TrendType}, {trend.UseHighLow}), inserted={dbStats.Inserted} " +
                    $"modified={dbStats.Modified} deleted={dbStats.Deleted} " +
                    $"untouched={dbStats.Untouched} total={dbStats.Total}");
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
