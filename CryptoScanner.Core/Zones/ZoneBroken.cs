using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Zones;

public class ZoneBroken
{

    /// <summary>
    /// One candle against one list of still-open zones. A zone that closes drops out of the list so
    /// the remaining candles do not walk past it again, and is queued for the database.
    /// <para>
    /// The delay is the caller's: a zone is not tested against the candles right after its own
    /// OpenTime, because those are the candles that formed it.
    /// </para>
    /// <para>
    /// The rules are picked per zone and not per list: this walk holds the DLZ and the FVG zones of
    /// one symbol together, and those have their own MaxTouches and TouchLevel. Both rule sets are
    /// read once by the caller - reading them here would be a settings lookup per zone per candle.
    /// </para>
    /// </summary>
    private static void ApplyAndRemoveIfClosed(List<CryptoZone> zones, CryptoCandle candle,
        CryptoInterval interval, ZoneInvalidation.ZoneTouchRules dlzRules,
        ZoneInvalidation.ZoneTouchRules fvgRules, long delay, CandleTime key)
    {
        // From the back, so removing one does not shift the zones still to be visited.
        for (int i = zones.Count - 1; i >= 0; i--)
        {
            var zone = zones[i];
            if (key < zone.OpenTime + delay)
                continue;

            var rules = zone.Kind == CryptoZoneKind.FairValueGap ? fvgRules : dlzRules;
            if (ZoneInvalidation.ApplyToCandle(zone, candle, interval, rules))
            {
                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                zones.RemoveAt(i);
            }
        }
    }


    private static void CalculateBrokenZones(CryptoSymbolInterval symbolInterval,
        ref CandleTime key, CandleTime checkUpTo, long delay,
        List<CryptoZone> zonesLong, List<CryptoZone> zonesShort,
        ref CandleGapWalk gaps, ZoneInvalidation.ZoneTouchRules dlzRules,
        ZoneInvalidation.ZoneTouchRules fvgRules)
    {
        while (key <= checkUpTo)
        {
            if (symbolInterval.CandleList.TryGetValue(key, out CryptoCandle candle))
            {
                gaps.Hit();
                // Note: A candle could break multiple long or short boxes, that might be an unforseen problem..

                // The verdict itself comes from ZoneInvalidation, the same four rules every other
                // path uses since 24-08-2026. This walk used to carry its own, older rule:
                //
                //     if (candle.Low < zone.Top) -> the zone is dead
                //
                // which killed a zone on the first wick that reached into it - no visit counting, no
                // midpoint, no MaxTouches, and no body-close requirement. And this is the startup
                // walk: it decides which zones from the database survive when the scanner comes back
                // up, so after every restart it threw away every level price had ever touched. The
                // last place with a rule of its own.
                ApplyAndRemoveIfClosed(zonesLong, candle, symbolInterval.Interval, dlzRules, fvgRules, delay, key);
                ApplyAndRemoveIfClosed(zonesShort, candle, symbolInterval.Interval, dlzRules, fvgRules, delay, key);
            }
            else
                gaps.Miss(key);
            key += symbolInterval.Interval.Duration;
        }
    }


    private static void CalculateBrokenZonesForSymbol(CryptoSymbol symbol, CryptoInterval interval)
    {
        // Collect all active newCreatedZones (FVG + DLZ zones)
        List<CryptoZone> zones = [];
        CryptoSymbolData symbolData = symbol.Data;
        var symbolIntervalData = symbolData.Get(interval.IntervalPeriod);
        foreach (CryptoZone zone in symbolIntervalData.Dlz.Zones.LongOpen)
            zones.Add(zone);
        foreach (CryptoZone zone in symbolIntervalData.Dlz.Zones.ShortOpen)
            zones.Add(zone);
        foreach (CryptoZone zone in symbolIntervalData.Fvg.Zones.LongOpen)
            zones.Add(zone);
        foreach (CryptoZone zone in symbolIntervalData.Fvg.Zones.ShortOpen)
            zones.Add(zone);


        if (zones.Count > 0)
        {
            List<CryptoZone> zonesLong = [];
            List<CryptoZone> zonesShort = [];
            long delay = 4 * interval.Duration; // TODO, this is not right
            CandleTime maxTime = CandleTime.FromDateTime(GlobalData.Clock.UtcNow);
            CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

            // Kind of brute force (on 1h candles so its not that bad)..
            int last = zones.Count - 1;
            // Sort BEFORE the cursor is taken. zones was filled from LongOpen/ShortOpen, and those are
            // OrderedLists on PRICE - descending by Top for long, ascending by Bottom for short (see
            // CryptoSymbolIntervalZones). So zones.First() is the zone with the most extreme price, at
            // an arbitrary moment in time. Taking the candle cursor from it (what happened here until
            // 24-08-2026) started the walk at that arbitrary moment, and every zone older than it never
            // had a single candle checked against it - they could not break, whatever price did.
            zones.Sort((zoneA, zoneB) => zoneA.OpenTime.CompareTo(zoneB.OpenTime));
            CandleTime key = zones.First().OpenTime;
            key = IntervalTools.StartOfIntervalCandle(key, interval.Duration);
            // Startup path: this walks from the oldest zone in the database up to now, which reaches
            // far outside any window the zone engine loaded. A key that is not in memory is read as
            // "no candle touched this zone", so it is counted and reported rather than skipped in
            // silence - see ZoneCandleGaps.
            CandleGapWalk gaps = new();
            CandleTime gapsFrom = key;
            // Read once per symbol: this walk covers every candle since the oldest zone, and the
            // list below holds both kinds - each with its own settings.
            var dlzRules = ZoneInvalidation.RulesFor(CryptoZoneKind.DominantLevel);
            var fvgRules = ZoneInvalidation.RulesFor(CryptoZoneKind.FairValueGap);

            for (int i = 0; i <= last; i++)
            {
                // Might have a problem with equal times?

                var zone = zones[i];
                // The list of active newCreatedZones are growing as we iterate, broken newCreatedZones will be removed to keep the list small
                // Could optimize with sorted list (sort on top or bottom and shorten the loop in CalculateBrokenZones)
                if (zone.Side == CryptoTradeSide.Long)
                    zonesLong.Add(zone);
                else
                    zonesShort.Add(zone);

                CandleTime checkUpTo;
                if (i < last)
                    checkUpTo = zone.OpenTime;
                else
                    checkUpTo = maxTime;

                CalculateBrokenZones(symbolInterval, ref key, checkUpTo, delay, zonesLong, zonesShort, ref gaps, dlzRules, fvgRules);
            }
            CalculateBrokenZones(symbolInterval, ref key, maxTime, delay, zonesLong, zonesShort, ref gaps, dlzRules, fvgRules);
            ZoneCandleGaps.Report(symbol, interval, "brokenStartup", gaps, gapsFrom, maxTime);
        }
    }


    public static Task CalculateBrokenZonesForAllSymbols()
    {
        // Called at startup..
        if (GlobalData.ActiveExchange != null)
        {
            for (var i = 0; i < GlobalData.ActiveExchange.SymbolListName.Count; i++)
            {
                var symbol = GlobalData.ActiveExchange.SymbolListName.Values[i];

                // Only the symbols whose candles are in memory. The walk reads its candles from the
                // in-memory CandleList, so a symbol that was skipped while loading has an empty one:
                // every key is a miss, not one zone can be broken, and ZoneCandleGaps reports the
                // whole stretch as "not in memory". Measured on HyperLiquid Perpetual 24-08-2026: of
                // the 59 ZONE GAP lines at startup, 37 came from symbols without candles, together
                // good for 250 zones that were walked over for nothing. The rule below is the one
                // that decides whether the CandleList was filled at all, see the load loop in
                // CandleDatabase.LoadCandlesAsync.
                if (!symbol.QuoteData.FetchCandles || symbol.Status != 1)
                    continue;
                if (!GlobalData.IsEmulatorMode && !symbol.IsBarometerSymbol() && !symbol.EnoughVolume() && !symbol.IsTrading())
                    continue;

                foreach (string intervalName in GlobalData.Settings.Signal.ZonesDlz.IntervalList)
                {
                    if (GlobalData.IntervalListPeriodName.TryGetValue(intervalName, out CryptoInterval? interval))
                    {
                        CalculateBrokenZonesForSymbol(symbol, interval);
                    }
                }
            }
        }
        return Task.CompletedTask;
    }

}
