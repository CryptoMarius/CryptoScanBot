using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.Core.Zones;

// FVG - Fair Value Gaps

public class ZoneFvg
{
    private static CryptoZone? ScanForLongFvg(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle prev2, CryptoCandle prev, CryptoCandle candle)
    {
        // 3 green candles in a row (only the strong fvg)..
        if (candle.Close > candle.Open && prev.Close > prev.Open && prev2.Close > prev2.Open)
        {
            // with specific boundaries
            if (candle.Low > prev2.High && prev.Close > prev2.High)
            {
                double perc = 100 * (double)((candle.Low - prev2.High) / prev2.High);
                if (perc > GlobalData.Settings.Signal.ZonesFvg.MinimumPercentage)
                {
                    CryptoZone zone = new()
                    {
                        Kind = CryptoZoneKind.FairValueGap,
                        Strength = CryptoZoneStrength.None,
                        ExchangeId = symbol.Exchange.Id,
                        Exchange = symbol.Exchange,
                        SymbolId = symbol.Id,
                        Symbol = symbol,
                        IntervalId = interval.Id,
                        Interval = interval,
                        OpenTime = candle.OpenTime + interval.Duration,
                        Top = candle.Low,
                        Bottom = prev2.High,
                        Side = CryptoTradeSide.Long,
                        IsValid = false,
                        Description = $"{interval.Name} {perc:N2}%",
                    };
                    return zone;
                }
            }
        }
        return null;
    }

    private static CryptoZone? ScanForShortFvg(CryptoSymbol symbol, CryptoInterval interval, CryptoCandle prev2, CryptoCandle prev, CryptoCandle candle)
    {
        // 3 red candles in a row (only the strong fvg)..
        if (candle.Open > candle.Close && prev.Open > prev.Close && prev2.Open > prev2.Close)
        {
            // with specific boundaries
            if (candle.High < prev2.Low && prev.Close < prev2.Low)
            {
                double perc = 100 * (double)((prev2.Low - candle.High) / candle.High);
                if (perc > GlobalData.Settings.Signal.ZonesFvg.MinimumPercentage)
                {
                    CryptoZone zone = new()
                    {
                        Kind = CryptoZoneKind.FairValueGap,
                        Strength = CryptoZoneStrength.None,
                        ExchangeId = symbol.Exchange.Id,
                        Exchange = symbol.Exchange,
                        SymbolId = symbol.Id,
                        Symbol = symbol,
                        IntervalId = interval.Id,
                        Interval = interval,
                        OpenTime = candle.OpenTime + interval.Duration,
                        Top = prev2.Low,
                        Bottom = candle.High,
                        Side = CryptoTradeSide.Short,
                        IsValid = false,
                        Description = $"{interval.Name} {perc:N2}%",
                    };
                    return zone;
                }
            }
        }
        return null;
    }


    // FVG (just a quick approach)
    public static void Detect(CryptoSymbol symbol, CryptoInterval interval, CandleTime lastCandle1mCloseTime)
    {
        // Non-blocking: skip if a full recalculation currently holds ZoneLock.
        // This prevents concurrent writes to the non-thread-safe OrderedList.
        if (!symbol.Data.ZoneLock.Wait(0))
            return;
        try
        {
            // We need the last 3 candles
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

            if (!symbolInterval.CandleList.TryGetValue(lastCandle1mCloseTime - 1 * interval.Duration, out CryptoCandle candle))
                return;
            if (!symbolInterval.CandleList.TryGetValue(lastCandle1mCloseTime - 2 * interval.Duration, out CryptoCandle prev))
                return;
            if (!symbolInterval.CandleList.TryGetValue(lastCandle1mCloseTime - 3 * interval.Duration, out CryptoCandle prev2))
                return;

            // scan voor long FVG
            //if (side == CryptoTradeSide.Long)
            {
                var zone = ScanForLongFvg(symbol, interval, prev2, prev, candle);
                if (zone != null)
                {
                    //GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} {CryptoTradeSide.Long} FVG {prev2.High}..{candle.Low} {zone.Description}");
                    var symbolDataInterval = symbol.Data.Get(interval.IntervalPeriod);
                    symbolDataInterval.Fvg.Zones.LongOpen.Add(zone);
                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                }
            }

            // scan voor short FVG
            //if (side == CryptoTradeSide.Short)
            {
                var zone = ScanForShortFvg(symbol, interval, prev2, prev!, candle);
                if (zone != null)
                {
                    //GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} {CryptoTradeSide.Short} FVG {candle.Low}..{prev2.High} {zone.Description}");
                    var symbolDataInterval = symbol.Data.Get(interval.IntervalPeriod);
                    symbolDataInterval.Fvg.Zones.ShortOpen.Add(zone);
                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                }
            }

            // Realtime invalidation: apply the just-closed candle to all open zones so that
            // TouchCount, IsMitigated and CloseTime stay current between full recalc cycles.
            // Without this the live LongOpen/ShortOpen lists would keep showing zones that
            // have already been wicked or even body-broken, producing stale entry signals.
            var symbolDataIntervalForInvalidate = symbol.Data.Get(interval.IntervalPeriod);
            int maxTouches = GlobalData.Settings.Signal.ZonesFvg.MaxTouches;

            InvalidateRealtime(symbolDataIntervalForInvalidate.Fvg.Zones.LongOpen,
                symbolDataIntervalForInvalidate.Fvg.Zones.LongClosed,
                candle, interval, maxTouches);
            InvalidateRealtime(symbolDataIntervalForInvalidate.Fvg.Zones.ShortOpen,
                symbolDataIntervalForInvalidate.Fvg.Zones.ShortClosed,
                candle, interval, maxTouches);

            // Keep CalculateZonesAsync's incremental cursor in sync with what this realtime tick
            // just covered. Without this, the periodic catch-up (triggered by DLZ's queue, often
            // drained within the same tick in the emulator — see ZoneThreadCalculate.DrainQueueAsync)
            // would re-scan and re-invalidate this exact candle a second time: duplicate zone inserts
            // (ScanForLongFvg/ScanForShortFvg would match the same 3-candle pattern again) and
            // double-counted TouchCount (ZoneInvalidation.ApplyToCandle is not idempotent — it always
            // increments on a wick, regardless of whether it already saw this candle).
            // Only advance if the cursor is already set: null means the first full historical scan
            // (CalculateZonesAsync's "first run" branch) hasn't happened yet, and must not be skipped.
            if (symbolDataIntervalForInvalidate.Fvg.ProcessedCandleMarker != null)
                symbolDataIntervalForInvalidate.Fvg.ProcessedCandleMarker = candle.OpenTime;
        }
        finally
        {
            symbol.Data.ZoneLock.Release();
        }
    }


    /// <summary>
    /// Apply <see cref="ZoneInvalidation.ApplyToCandle"/> to every open zone for a single
    /// just-closed candle. Zones that close are moved from <paramref name="openZones"/> to
    /// <paramref name="closedZones"/> and queued for DB persistence.
    /// </summary>
    private static void InvalidateRealtime(OrderedList<CryptoZone> openZones,
        OrderedList<CryptoZone> closedZones, CryptoCandle candle, CryptoInterval interval, int maxTouches)
    {
        if (openZones.Count == 0)
            return;

        // Iterate from the back so removals don't shift the items we still have to visit.
        for (int i = openZones.Count - 1; i >= 0; i--)
        {
            var zone = openZones[i];
            bool wasOpen = zone.CloseTime == null;
            ZoneInvalidation.ApplyToCandle(zone, candle, interval, maxTouches);

            if (wasOpen && zone.CloseTime != null)
            {
                openZones.RemoveAt(i);
                closedZones.Add(zone);
                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
            }
            // TouchCount/IsMitigated are in-memory only (Computed in the DB schema). They are
            // rebuilt deterministically by CalculateZonesAsync from candle history, so no DB
            // persistence is needed for them between recalc cycles.
        }
    }


    private static void InvalidateLongZones(CryptoSymbolInterval symbolIntervalData,
        OrderedList<CryptoZone> zoneList, CryptoCandle candle)
    {
        int count = zoneList.Count;
        if (count == 0)
            return;

        int maxTouches = GlobalData.Settings.Signal.ZonesFvg.MaxTouches;
        int index = 0;
        //if (count > 10)
        //{
        //    var s = new CryptoZone()
        //    {
        //        Kind = CryptoZoneKind.ZoneFvg,
        //        CreateTime = candle.Time,
        //        AccountId = GlobalData.ActiveAccount!.Id,
        //        Account = GlobalData.ActiveAccount,
        //        ExchangeId = symbol.Exchange.Id,
        //        Exchange = symbol.Exchange,
        //        SymbolId = symbol.Id,
        //        Symbol = symbol,
        //        OpenTime = candle.OpenTime,
        //        Top = candle.High,
        //        Bottom = candle.Low,
        //        Side = CryptoTradeSide.Long,
        //        IsValid = false,
        //    };

        //    oldZones = zoneList.BinarySearch(s);
        //    if (oldZones < 0)
        //        oldZones = 0;
        //    else if (oldZones > zoneList.Count)
        //        oldZones = count - 1;
        //}
        //if (count > 20)
        //    oldZones = oldZones; // debug

        while (index < zoneList.Count) // sorted on Zone.Top descending
        {
            var zone = zoneList[index];

            // situation (A candle way above the zone) The list is sorted on top value and if there are no more reachable oldZones break (save some looping time)
            if (candle.Low > zone.Top)
                break;

            // Loosened invalidation: only a body-close through the floor truly breaks a zone.
            // Wicks into the zone are TESTS (TouchCount++) until MaxTouches exhausts it.
            // See ZoneInvalidation for the rationale (supply/demand & ICT theory).
            ZoneInvalidation.ApplyToCandle(zone, candle, symbolIntervalData.Interval, maxTouches);

            //if (zone.CloseTime != null) // remove all closed oldZones
            //{
            //    //zoneList.RemoveAt(index);
            //    //GlobalData.ThreadSaveObjects!.AddToQueue(zone);
            //    //symbolIntervalData.Fvg.Zones.LongClosed.Add(zone);
            //    //GlobalData.AddTextToLogTab($"{symbol.Name} Removed fvg zone #{zone.Id} {zone.Side} {zone.Description}");
            //}
            //else index++;
            index++;
        }
    }


    private static void InvalidateShortZones(CryptoSymbolInterval symbolIntervalData,
        OrderedList<CryptoZone> zoneList, CryptoCandle candle)
    {
        int count = zoneList.Count;
        if (count == 0)
            return;

        int maxTouches = GlobalData.Settings.Signal.ZonesFvg.MaxTouches;
        int index = 0;
        //if (count > 10)
        //{
        //    var s = new CryptoZone()
        //    {
        //        Kind = CryptoZoneKind.ZoneFvg,
        //        CreateTime = candle.Time,
        //        AccountId = GlobalData.ActiveAccount!.Id,
        //        Account = GlobalData.ActiveAccount,
        //        ExchangeId = symbol.Exchange.Id,
        //        Exchange = symbol.Exchange,
        //        SymbolId = symbol.Id,
        //        Symbol = symbol,
        //        OpenTime = candle.OpenTime,
        //        Top = candle.High,
        //        Bottom = candle.Low,
        //        Side = CryptoTradeSide.Short,
        //        IsValid = false,
        //    };

        //    oldZones = zoneList.BinarySearch(s); - this works a bit different than expected, need more time for this
        //    if (oldZones < 0)
        //        oldZones = 0;
        //    else if (oldZones > zoneList.Count)
        //        oldZones = count - 1;
        //}
        //if (count > 20)
        //    oldZones = oldZones; // debug

        while (index < zoneList.Count) // sorted on Zone.Bottom asscending
        {
            var zone = zoneList[index];

            // situation (A candle way below the zone) The list is sorted on bottom value and if there are no more reachable oldZones break (save some looping time)
            if (candle.High < zone.Bottom)
                break;

            // Loosened invalidation: only a body-close through the ceiling truly breaks a zone.
            // Wicks into the zone are TESTS (TouchCount++) until MaxTouches exhausts it.
            ZoneInvalidation.ApplyToCandle(zone, candle, symbolIntervalData.Interval, maxTouches);

            //if (zone.CloseTime != null) // remove all closed oldZones
            //{
            //    //zoneList.RemoveAt(index);
            //    //GlobalData.ThreadSaveObjects!.AddToQueue(zone);
            //    //symbolIntervalData.Fvg.Zones.ShortClosed.Add(zone);
            //    //GlobalData.AddTextToLogTab($"{symbol.Name} Removed fvg zone #{zone.Id} {zone.Side} {zone.Description}");
            //}
            //else index++;
            index++;
        }
    }



    // The two candles before the first one this scan acts on. A gap right at the start of the window
    // pushes them further back, so ask for a couple more than the arithmetic minimum of one.
    private const int FvgScanLeadInCandles = 3;

    private static void CreateFvgZones(CryptoSymbol symbol, CryptoInterval interval, CandleTime minDate,
        CandleTime maxDate, CryptoSymbolInterval symbolIntervalData,
        OrderedList<CryptoZone> longZones, OrderedList<CryptoZone> shortZones)
    {

        // Scan for long and short fvg (in memory)
        CryptoCandle prev = default;
        CryptoCandle prev2 = default;
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        // Read a bounded range under the read lock instead of enumerating the live list. Two reasons:
        //
        // (1) Values is the inherited SortedDictionary collection and does not take the
        // ReaderWriterLockSlim that CryptoCandleList guards itself with, so an Add from the kline
        // stream bumps the tree version and the enumerator throws "Collection was modified after the
        // enumerator was instantiated". This runs inline in SignalPrepare, so it is on the signal path
        // while that stream is writing. A lock() on the list object would not help - the writers hold
        // the ReaderWriterLockSlim and never the monitor.
        //
        // (2) GetSnapshot() would copy the WHOLE list, and for the lower intervals that is far more
        // than this scan uses: the loop below does nothing for a candle at or before minDate except
        // carry it as prev/prev2. The range is what the zones are actually built from, so the copy
        // stays at roughly CandleTools.CandleCountFetch entries whatever the list grows to.
        CandleTime scanFrom = minDate - FvgScanLeadInCandles * interval.Duration;
        foreach (var candle in symbolInterval.CandleList.GetRange(scanFrom, maxDate, interval.Duration))
        {
            if (prev2.OpenTime != 0 && prev.OpenTime != 0 && candle.OpenTime > minDate) // Need the last 3 candles
            {
                if (SignalPrepare.ZoneFvgActive())
                {
                    // scan for long FVG
                    var zone = ScanForLongFvg(symbol, interval, prev2, prev, candle);
                    if (zone != null)
                    {
                        //GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} {CryptoTradeSide.Long} FVG {prev2.High}..{candle.Low} {zone.Description}");
                        longZones.Add(zone); // memory only
                                             //GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                    }
                    InvalidateLongZones(symbolIntervalData, longZones, candle); // Remove closed fvg

                    // scan for short FVG
                    zone = ScanForShortFvg(symbol, interval, prev2, prev, candle);
                    if (zone != null)
                    {
                        //GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} {CryptoTradeSide.Short} FVG {candle.Low}..{prev2.High} {zone.Description}");
                        shortZones.Add(zone); // memory only
                                              //GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                    }
                    InvalidateShortZones(symbolIntervalData, shortZones, candle); // Remove closed fvg
                }

            }
            prev2 = prev;
            prev = candle;
        }
    }


    private static async Task<(CandleTime minDate, CandleTime maxDate)> LoadHistoricCandles(CryptoSymbol symbol, CryptoInterval interval,
        SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        // Determine the period (using the candlecount). Same depth as DLZ and as the candles
        // themselves - see CandleTools.CandleCountFetch.
        int candleFetchCount = CandleTools.CandleCountFetch;
        CandleTime maxDate = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow, interval.Duration);
        CandleTime minDate = maxDate - candleFetchCount * interval.Duration;
        await ZoneCandleEngine.FetchFrom(loadedCandlesInMemory, symbol, interval, minDate, candleFetchCount);

#if DEBUG
        var count = symbol.GetSymbolInterval(interval).CandleList.Count;
        GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} fvg from {minDate.ToLocalTime()} .. {maxDate.ToLocalTime()} candles = {count}");
#endif
        return (minDate, maxDate);
    }


    /// <summary>
    /// Apply the same per-candle FVG scan + invalidation that <see cref="CreateFvgZones"/> uses, but
    /// only to the candles strictly after <paramref name="lastProcessed"/> — i.e. the ones that
    /// arrived since the previous call. A gap can only be formed by the latest 3 candles, so once the
    /// history up to <paramref name="lastProcessed"/> has been scanned once (see
    /// <see cref="CalculateZonesAsync"/>'s first-run branch) there is no need to ever replay it again.
    /// New zones are appended directly to the live open lists and queued for DB persistence, mirroring
    /// the realtime path in <see cref="ScanForNew"/> (which this duplicates intentionally: ScanForNew
    /// only fires on its own interval-boundary tick, while this is the periodic catch-up/backfill path —
    /// running the same per-candle logic here as well keeps both lists in sync if a tick is ever missed).
    /// </summary>
    private static void ProcessNewCandlesIncremental(CryptoSymbol symbol, CryptoInterval interval,
        CryptoSymbolInterval symbolIntervalData, CryptoSymbolIntervalZones zones,
        CandleTime lastProcessed, CandleTime maxDate)
    {
        if (!SignalPrepare.ZoneFvgActive())
            return;

        CandleTime loop = lastProcessed + interval.Duration;
        while (loop <= maxDate)
        {
            if (symbolIntervalData.CandleList.TryGetValue(loop, out CryptoCandle candle)
                && symbolIntervalData.CandleList.TryGetValue(loop - interval.Duration, out CryptoCandle prev)
                && symbolIntervalData.CandleList.TryGetValue(loop - (2 * interval.Duration), out CryptoCandle prev2))
            {
                var longZone = ScanForLongFvg(symbol, interval, prev2, prev, candle);
                if (longZone != null)
                {
                    zones.LongOpen.Add(longZone);
                    GlobalData.ThreadSaveObjects!.AddToQueue(longZone);
                }
                InvalidateRealtimeList(zones.LongOpen, zones.LongClosed, candle, interval);

                var shortZone = ScanForShortFvg(symbol, interval, prev2, prev, candle);
                if (shortZone != null)
                {
                    zones.ShortOpen.Add(shortZone);
                    GlobalData.ThreadSaveObjects!.AddToQueue(shortZone);
                }
                InvalidateRealtimeList(zones.ShortOpen, zones.ShortClosed, candle, interval);
            }
            loop += interval.Duration;
        }
    }


    /// <summary>
    /// Shared by <see cref="ProcessNewCandlesIncremental"/> (periodic catch-up) and
    /// <see cref="InvalidateRealtime"/> (the live per-tick path): reads MaxTouches itself so callers
    /// don't need to thread the setting through.
    /// </summary>
    private static void InvalidateRealtimeList(OrderedList<CryptoZone> openZones,
        OrderedList<CryptoZone> closedZones, CryptoCandle candle, CryptoInterval interval)
    {
        int maxTouches = GlobalData.Settings.Signal.ZonesFvg.MaxTouches;
        InvalidateRealtime(openZones, closedZones, candle, interval, maxTouches);
    }


    public static async Task CalculateZonesAsync(AddTextEvent? sender, CryptoSymbol symbol, CryptoInterval interval,
        SortedList<CryptoIntervalPeriod, bool> loadedCandlesInMemory)
    {
        if (SignalPrepare.ZoneFvgActive())
        {
            try
            {
                sender?.Invoke($"Calculating fvg zones {symbol.Exchange.Name} {symbol.Name} {interval.Name}");
                var (minDate, maxDate) = await LoadHistoricCandles(symbol, interval, loadedCandlesInMemory);

                CryptoSymbolData symbolData = symbol.Data;
                var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);
                CryptoSymbolIntervalZones zones = symbolIntervalData.Fvg.Zones;

                if (symbolIntervalData.Fvg.ProcessedCandleMarker != null && symbolIntervalData.Fvg.ProcessedCandleMarker.Value >= minDate)
                {
                    // Already scanned this window once before: only the candles that arrived since the
                    // last call can contain a new gap. No DB diff needed — the live lists are already
                    // the authoritative state (see ZoneThreadCalculate's load-once guard).
                    ProcessNewCandlesIncremental(symbol, interval, symbolIntervalData, zones,
                        symbolIntervalData.Fvg.ProcessedCandleMarker.Value, maxDate);
                }
                else
                {
                    // First run for this (symbol, interval) — or minDate slid forward past the cursor
                    // (CandleCount setting shrunk) — fall back to the full historical scan + DB diff,
                    // same as before.

                    //if (symbol.Name == "1000PEPEUSDT")
                    //    GlobalData.AddTextToLogTab($"{symbol.Name} {interval.Name} " +
                    //        $"{minDate.ToLocalTime():yyyy-MM-dd HH:mm} .. {maxDate.ToLocalTime():yyyy-MM-dd HH:mm} " +
                    //        $"fvg zones long = {zones.LongOpen.Count} " +
                    //        $"fvg zones short = {zones.ShortOpen.Count} ");

                    // Index old zones for DB merge (must happen before Reset)
                    DatabaseStatistics statistics = new();
                    SortedList<(CryptoTradeSide, CandleTime?, decimal, decimal), CryptoZone> oldZones = [];
                    ZoneTools.CreateZoneIndex(zones.LongOpen, oldZones, statistics);
                    ZoneTools.CreateZoneIndex(zones.ShortOpen, oldZones, statistics);
                    ZoneTools.CreateZoneIndex(zones.LongClosed, oldZones, statistics);
                    ZoneTools.CreateZoneIndex(zones.ShortClosed, oldZones, statistics);

                    // Compute new zones into local lists (never visible to other threads)
                    OrderedList<CryptoZone> longZones = new(new CompareZoneDescending());
                    OrderedList<CryptoZone> shortZones = new(new CompareZoneAscending());
                    CreateFvgZones(symbol, interval, minDate, maxDate, symbolIntervalData, longZones, shortZones);

                    // Merge with DB state into a fresh object, then atomically replace the live reference.
                    // Other threads always see either the old complete object or the new complete one —
                    // never a half-built list (which was the source of null holes in OrderedList).
                    CryptoSymbolIntervalZones freshZones = new();
                    ZoneTools.AddZonesToInternalLists(freshZones, oldZones, longZones, statistics);
                    ZoneTools.AddZonesToInternalLists(freshZones, oldZones, shortZones, statistics);
                    // Gaps older than the window cannot be rediscovered from the candles we hold, so
                    // they are carried until their right edge leaves the window as well.
                    ZoneTools.DeleteRemainingZones(oldZones, statistics, freshZones, minDate);
                    symbolIntervalData.Fvg.Zones = freshZones;
                }

                symbolIntervalData.Fvg.ProcessedCandleMarker = maxDate;
            }
            catch (Exception error)
            {
                ScannerLog.Logger.Info($"ERROR {error}");
                GlobalData.AddErrorToLogTab($"ERROR {error}");
            }
        }
    }
}
