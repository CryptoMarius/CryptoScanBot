using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Zones;

public class ZoneBroken
{

    private static void CalculateBrokenZones(CryptoSymbolInterval symbolInterval,
        ref CandleTime key, CandleTime checkUpTo, long delay,
        List<CryptoZone> zonesLong, List<CryptoZone> zonesShort,
        ref CandleGapWalk gaps)
    {
        while (key <= checkUpTo)
        {
            if (symbolInterval.CandleList.TryGetValue(key, out CryptoCandle candle))
            {
                gaps.Hit();
                // Note: A candle could break multiple long or short boxes, that might be an unforseen problem..

                foreach (var zone in zonesLong)
                {
                    if (key >= zone.OpenTime + delay && candle.Low < zone.Top)
                    {
                        zone.CloseTime = candle.OpenTime + symbolInterval.Interval.Duration;
                        GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                        zonesLong.Remove(zone); // breaks iterator
                        break;
                    }
                }
                foreach (var zone in zonesShort)
                {
                    if (key >= zone.OpenTime + delay && candle.High > zone.Bottom)
                    {
                        zone.CloseTime = candle.OpenTime + symbolInterval.Interval.Duration;
                        GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                        zonesShort.Remove(zone); // breaks iterator
                        break;
                    }
                }
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

                CalculateBrokenZones(symbolInterval, ref key, checkUpTo, delay, zonesLong, zonesShort, ref gaps);
            }
            CalculateBrokenZones(symbolInterval, ref key, maxTime, delay, zonesLong, zonesShort, ref gaps);
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
