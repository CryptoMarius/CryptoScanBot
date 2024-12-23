using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Exchange;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

using Dapper;

namespace CryptoScanBot.Core.Zones;

public class LiquidityZones
{
    public static void LoadAllZones()
    {
        //GlobalData.AddTextToLogTab("Reading open zones");

        foreach (var x in GlobalData.ActiveAccount!.Data.SymbolDataList.Values.ToList())
        {
            x.ResetFvgData();
            x.ResetZoneData();
        }


        SortedList<string, AccountSymbolData> todoSorting = [];
        using var database = new CryptoDatabase();
        string sql = "select * from zone where CloseTime is null";
        foreach (CryptoZone zone in database.Connection.Query<CryptoZone>(sql))
        {
            if (GlobalData.TradeAccountList.TryGetValue(zone.AccountId, out CryptoAccount? tradeAccount))
            {
                zone.Account = tradeAccount;
                if (GlobalData.ExchangeListId.TryGetValue(zone.ExchangeId, out Model.CryptoExchange? exchange))
                {
                    zone.Exchange = exchange;
                    if (exchange.SymbolListId.TryGetValue(zone.SymbolId, out CryptoSymbol? symbol))
                    {
                        zone.Symbol = symbol;
                        AccountSymbolData symbolData = tradeAccount.Data.GetSymbolData(symbol.Name);
                        if (zone.Kind == CryptoZoneKind.FairValueGap)
                        {
                            if (zone.Side == CryptoTradeSide.Long)
                                symbolData.FvgListLong.Add(zone);
                            else
                                symbolData.FvgListShort.Add(zone);
                        }
                        else
                        {
                            if (zone.Side == CryptoTradeSide.Long)
                                symbolData.ZoneListLong.Add(zone);
                            else
                                symbolData.ZoneListShort.Add(zone);

                            // Creation date is the date of the last swing point (SH/SL)
                            long timeLastSwingPoint = CandleTools.GetUnixTime(zone.CreateTime, 0);
                            CryptoInterval interval = GlobalData.IntervalListPeriod[GlobalData.Settings.Signal.Zones.Interval];
                            AccountSymbolIntervalData symbolIntervalData = symbolData.GetAccountSymbolInterval(interval.IntervalPeriod);

                            if (symbolIntervalData.TimeLastSwingPoint == null || timeLastSwingPoint > symbolIntervalData.TimeLastSwingPoint)
                            {
                                symbolIntervalData.TimeLastSwingPoint = timeLastSwingPoint;

                                if (symbolIntervalData.LastSwingLow == null || zone.Bottom > symbolIntervalData.LastSwingLow)
                                    symbolIntervalData.LastSwingLow = zone.Bottom;
                                if (symbolIntervalData.LastSwingHigh == null || zone.Top > symbolIntervalData.LastSwingHigh)
                                    symbolIntervalData.LastSwingHigh = zone.Top;
                            }
                        }
                        todoSorting.TryAdd(symbol.Name, symbolData);
                    }
                }
            }
        }

        // do some sorting
        foreach (AccountSymbolData symbolData in todoSorting.Values)
            symbolData.SortZones();
    }


    public static void LoadZonesForSymbol(int symbolId, CryptoZoneData data)
    {
        using var database = new CryptoDatabase();
        string sql = "select * from zone where SymbolId = @SymbolId";
        foreach (CryptoZone zone in database.Connection.Query<CryptoZone>(sql, new { SymbolId = symbolId }))
        {
            if (GlobalData.TradeAccountList.TryGetValue(zone.AccountId, out CryptoAccount? tradeAccount))
            {
                zone.Account = tradeAccount;
                if (GlobalData.ExchangeListId.TryGetValue(zone.ExchangeId, out Model.CryptoExchange? exchange))
                {
                    zone.Exchange = exchange;
                    if (exchange.SymbolListId.TryGetValue(zone.SymbolId, out CryptoSymbol? symbol))
                    {
                        zone.Symbol = symbol;
                        if (zone.Side == CryptoTradeSide.Long)
                            data.ZoneListLong.Add(zone);
                        else
                            data.ZoneListShort.Add(zone);
                    }
                }
            }
        }
    }


    public static void SaveZonesForSymbol(CryptoZoneData data, List<ZigZagResult> zigZagList)
    {
        var symbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(data.Symbol.Name);

        // Oops, there are duplicate zones (strange, didn't expect this!)
        // We remove the duplicates with this code which is fine for now.
        // (still curious why this is happening..... ENAUSDT 14 Aug 23:00 UTC)
        SortedList<(decimal, decimal, CryptoTradeSide), CryptoZone> zoneIndex = [];
        foreach (var zone in data.ZoneListLong)
            if (zone.Kind == CryptoZoneKind.DominantLevel)
                zoneIndex.TryAdd((zone.Bottom, zone.Top, zone.Side), zone);
        foreach (var zone in data.ZoneListShort)
            if (zone.Kind == CryptoZoneKind.DominantLevel)
                zoneIndex.TryAdd((zone.Bottom, zone.Top, zone.Side), zone);

        // We rebuild all the lists
        data.ZoneListLong.Clear();
        data.ZoneListShort.Clear();
        symbolData.ResetZoneData();

        int inserted = 0;
        int deleted = 0;
        int modified = 0;
        int untouched = 0;
        foreach (var zigZag in zigZagList)
        {
            if (zigZag.Dominant && !zigZag.Dummy) //  && zigZag.IsValid all zones (also the closed ones)
            {
                bool changed = false;
                CryptoTradeSide side = zigZag.PointType == 'L' ? CryptoTradeSide.Long : CryptoTradeSide.Short;

                // Try to reuse the previous zones.
                if (!zoneIndex.TryGetValue((zigZag.Bottom, zigZag.Top, side), out CryptoZone? zone))
                {
                    zone = new()
                    {
                        Kind = CryptoZoneKind.DominantLevel,
                        CreateTime = zigZag.Candle.Date,
                        AccountId = GlobalData.ActiveAccount.Id,
                        Account = GlobalData.ActiveAccount,
                        ExchangeId = data.Symbol.Exchange.Id,
                        Exchange = data.Symbol.Exchange,
                        SymbolId = data.Symbol.Id,
                        Symbol = data.Symbol,
                        OpenTime = zigZag.Candle.OpenTime,
                        Top = zigZag.Top,
                        Bottom = zigZag.Bottom,
                        Side = side,
                        IsValid = false,
                    };
                    changed = true;
                }

                // mark the zone as interesting because of a large move into the zone
                string description = $"{data.Interval.Name}: {zigZag.Percentage:N2}% {zigZag.NiceIntro}";
                if (!zigZag.IsValid)
                    description += " not valid";
                if (description != zone.Description)
                {
                    changed = true;
                    zone.Description = description;
                }


                if (zone.CloseTime == null && zigZag.CloseDate != null)
                {
                    changed = true;
                    zone.CloseTime = zigZag.CloseDate;
                }
                else if (zone.CloseTime != null && zigZag.CloseDate == null)
                {
                    // rat race?
                    changed = true;
                    zone.CloseTime = null;
                }


                if (zigZag.IsValid != zone.IsValid)
                {
                    changed = true;
                    zone.IsValid = zigZag.IsValid;
                }

                // All the zones (including the invalidated zones)
                if (side == CryptoTradeSide.Long)
                    data.ZoneListLong.Add(zone);
                else
                    data.ZoneListShort.Add(zone);

                // The not closed zones (much less)
                if (zone.CloseTime == null)
                {
                    if (side == CryptoTradeSide.Long)
                        symbolData.ZoneListLong.Add(zone);
                    else
                        symbolData.ZoneListShort.Add(zone);
                }

                if (changed)
                {
                    if (zone.Id > 0)
                    {
                        modified++;
                        //databaseThread.Connection.Update(zone);
                        GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                    }
                    else
                    {
                        inserted++;
                        //databaseThread.Connection.Insert(zone);
                        GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                    }
                }
                else untouched++;

                zoneIndex.Remove((zigZag.Bottom, zigZag.Top, side));
            }
        }

        // delete the remaining zones
        foreach (var zone in zoneIndex.Values)
        {
            deleted++;
            zone.Id *= -1;
            GlobalData.ThreadSaveObjects!.AddToQueue(zone);
        }
        int total = data.ZoneListLong.Count + data.ZoneListShort.Count;
        GlobalData.AddTextToLogTab($"{data.Symbol.Name} Zones calculated, inserted={inserted} modified={modified} deleted={deleted} " +
            $"untouched={untouched} total={total} ({symbolData.ZoneListLong.Count}/{symbolData.ZoneListShort.Count})");

        symbolData.SortZones();
    }


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
                        await CandleEngine.LoadCandleDataFromDiskAsync(symbol, zoomInterval.Interval);
                    loadedCandlesInMemory.TryAdd(zoomInterval.IntervalPeriod, false); // in memory, nothing changed

                    // Load candles from the exchange
                    int count = durationForThisCandle / zoomInterval.Interval.Duration;
                    if (await CandleEngine.FetchFrom(symbol, zoomInterval.Interval, unixStart, count))
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
                                        double percentage = (double)(100 * ((bodyTop - zigZag.Bottom) / zigZag.Bottom));
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
                                        double percentage = (double)(100 * ((zigZag.Top - bodyBottom) / bodyBottom));
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
        //GlobalData.AddTextToLogTab($"{data.Symbol.Name} Calculating zones");
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
            ZigZagResult? previous = null;
            foreach (var zigZag in data.Indicator.ZigZagList)
            {
                if (previous != null)
                {
                    if (zigZag.Dominant && !zigZag.Dummy) //  && zigZag.IsValid all zones (also the closed ones)
                    {
                        decimal? price = null;
                        long max = zigZag.Candle.OpenTime;
                        long min = max - GlobalData.Settings.Signal.Zones.ZoneStartCandleCount * data.Interval.Duration;
                        if (min < previous.Candle.OpenTime)
                            min = previous.Candle.OpenTime;
                        while (min < max)
                        {
                            if (data.SymbolInterval.CandleList.TryGetValue(min, out CryptoCandle? candle))
                            {
                                if (zigZag.PointType == 'L')
                                {
                                    if (candle.High > zigZag.Value)
                                    {
                                        if (price == null || candle.High > price)
                                            price = candle.High;
                                    }
                                }
                                else
                                {
                                    if (candle.Low < zigZag.Value)
                                    {
                                        if (price == null || candle.Low < price)
                                            price = candle.Low;
                                    }
                                }
                            }
                            min += data.SymbolInterval.Interval.Duration;
                        }

                        if (price != null)
                        {
                            double diff = (double)Math.Abs(zigZag.Value - price.Value);
                            double perc = 100 * diff / (double)zigZag.Value;
                            if (perc >= GlobalData.Settings.Signal.Zones.ZoneStartPercentage)
                            {
                                zigZag.NiceIntro = $"{perc:N2} !!!";
                            }
                            else
                                zigZag.NiceIntro = $"{perc:N2}";
                        }
                    }
                }
                previous = zigZag;
            }
        }
    }

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
                    await CalculateLiqBoxesAsync(sender, data, session.ZoomLiqBoxes, loadedCandlesInMemory);
                    CalculateIntroZone(data);
                    CalculateBrokenBoxes(data);
                }


                // Create the zones and save them
                if (session.ForceCalculation)
                    SaveZonesForSymbol(data, data.Indicator.ZigZagList);
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
                    LoadZonesForSymbol(data.Symbol.Id, data);
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
