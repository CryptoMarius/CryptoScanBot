using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.Core.Zones;

public class DatabaseStatistics
{
    public int Inserted { get; set; }
    public int Modified { get; set; }
    public int Deleted { get; set; }
    public int Untouched { get; set; }
    public int Retained { get; set; }
    public int Total { get; set; }
}


public class ZoneTools
{
    public static void CreateZoneIndex(IList<CryptoZone> zones,
        SortedList<(CryptoTradeSide, CandleTime?, decimal, decimal), CryptoZone> zonesFromDatabase,
        DatabaseStatistics statistics)
    {
        foreach (var zone in zones)
        {
            // Warning, there can be duplicate zones, remove them!
            if (zonesFromDatabase.ContainsKey((zone.Side, zone.OpenTime, zone.Bottom, zone.Top)))
            {
                if (zone.Id > 0)
                {
                    zone.Id *= -1;
                    statistics.Deleted++;
                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                }
            }
            else zonesFromDatabase.Add((zone.Side, zone.OpenTime, zone.Bottom, zone.Top), zone);
        }
    }


    public static void AddZonesToInternalLists(CryptoSymbolIntervalZones zoneData,
        SortedList<(CryptoTradeSide, CandleTime?, decimal, decimal), CryptoZone> oldZones,
        IList<CryptoZone> newZones, DatabaseStatistics dbStats)
    {
        foreach (var zone in newZones)
        {
            // reuse an previous zone from the database if it exists
            bool zoneExistsInDatabase = false;
            if (oldZones.TryGetValue((zone.Side, zone.OpenTime, zone.Bottom, zone.Top), out CryptoZone? zoneInDb))
            {
                zone.Id = zoneInDb.Id;
                zoneExistsInDatabase = zoneInDb.Id > 0; // might still be zero

                // nothing important has changed, do not change the zone, skip..
                if (zoneInDb.CloseTime == zone.CloseTime && zoneInDb.Description == zone.Description &&
                    zoneInDb.IsValid == zone.IsValid && zoneInDb.Strength == zone.Strength &&
                    zoneInDb.TouchCount == zone.TouchCount && zoneInDb.ReachedMidpoint == zone.ReachedMidpoint)
                {
                    oldZones.Remove((zone.Side, zone.OpenTime, zone.Bottom, zone.Top));
                    dbStats.Untouched++;
                    //if (zone.Symbol.Name == "1000PEPEUSDT")
                    //    GlobalData.AddTextToLogTab($"{zone.ZoneText("Reusing")}");
                    zoneData.Add(zone);
                    continue;
                }
            }


            // Add the new calculated zones to the internal zones
            zoneData.Add(zone);

            if (zoneExistsInDatabase)
            {
                dbStats.Modified++;
                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                //if (data.Symbol.Name == "1000PEPEUSDT")
                //    GlobalData.AddTextToLogTab($"{data.Symbol.Name} modified={zone.Id} {zone.Kind} {zone.Side} {zone.Bottom:N8} {zone.Top:N8}");
            }
            else
            {
                dbStats.Inserted++;
                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                //if (zone.Symbol.Name == "1000PEPEUSDT")
                //    GlobalData.AddTextToLogTab($"{zone.ZoneText("Inserted")}");
            }
        }


        dbStats.Total = zoneData.LongOpen.Count + zoneData.LongClosed.Count + zoneData.ShortOpen.Count + zoneData.ShortClosed.Count;
    }



    /// <summary>
    /// Whatever the calculation did not produce this pass. Most of it is genuinely gone and is
    /// deleted, but not all of it: a calculation may only delete zones it could have produced.
    /// <para>
    /// A zone is derived from a pivot, and its OpenTime is that pivot's candle. Once the pivot has
    /// aged out of the candle window the calculation cannot produce that zone any more - not
    /// because it is wrong, but because it is out of sight. Deleting it there meant the zone
    /// history was pruned back to the window on every full calculation, so on every restart and on
    /// every press of the chart's "Calculate" button, and the candles to rebuild it were long gone.
    /// </para>
    /// <para>
    /// Those are carried into <paramref name="carryInto"/> instead, until their RIGHT edge leaves
    /// the window too: CloseTime for a zone that has been broken, and never for one that is still
    /// open - an open zone is still tradeable however old it is. Pass no window and the old
    /// behaviour returns, which is what the callers that own their whole result want.
    /// </para>
    /// </summary>
    public static void DeleteRemainingZones(SortedList<(CryptoTradeSide, CandleTime?, decimal, decimal), CryptoZone> zonesFromDatabase,
        DatabaseStatistics dbStats, CryptoSymbolIntervalZones? carryInto = null, CandleTime? windowStart = null)
    {
        foreach (var zone in zonesFromDatabase.Values)
        {
            if (carryInto != null && windowStart != null && OutOfSightButStillRelevant(zone, windowStart.Value))
            {
                carryInto.Add(zone);
                dbStats.Retained++;
                continue;
            }

            if (zone.Id != 0)
            {
                zone.Id *= -1;
                dbStats.Deleted++;
                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                //if (zone.Symbol.Name == "1000PEPEUSDT")
                //    GlobalData.AddTextToLogTab($"{zone.ZoneText("Deleting")}");
            }
        }
    }


    /// <summary>
    /// True when this zone starts before the calculation's window - so it could not have been
    /// produced this pass - while its right edge is still inside it.
    /// </summary>
    public static bool OutOfSightButStillRelevant(CryptoZone zone, CandleTime windowStart)
    {
        if (zone.OpenTime >= windowStart)
            return false; // inside the window: the calculation had its say and did not produce it
        return zone.CloseTime == null || zone.CloseTime.Value >= windowStart;
    }


    public static decimal? ZoneDistance(CryptoSymbol symbol)
    {
        // Set the date of the last swing point for the automatic zone calculation
        CryptoSymbolData symbolData = symbol.Data;

        // no data
        if (symbolData.DlzZoneDistance.BestLongZone == null && symbolData.DlzZoneDistance.BestShortZone == null)
            return null;

        // only one of them
        if (symbolData.DlzZoneDistance.BestLongZone == null)
            return symbolData.DlzZoneDistance.BestShortZone;
        if (symbolData.DlzZoneDistance.BestShortZone == null)
            return symbolData.DlzZoneDistance.BestLongZone;

        // which of the two is closeby
        return Math.Min(symbolData.DlzZoneDistance.BestLongZone.Value, symbolData.DlzZoneDistance.BestShortZone.Value);
    }


    public static CryptoTradeSide? ZoneTradeSide(CryptoSymbol symbol)
    {
        // Set the date of the last swing point for the automatic zone calculation
        CryptoSymbolData symbolData = symbol.Data;

        // no data
        if (symbolData.DlzZoneDistance.BestLongZone == null && symbolData.DlzZoneDistance.BestShortZone == null)
            return null;

        // only one of them
        if (symbolData.DlzZoneDistance.BestLongZone == null)
            return CryptoTradeSide.Short;
        if (symbolData.DlzZoneDistance.BestShortZone == null)
            return CryptoTradeSide.Long;


        // which of the two is closeby
        if (symbolData.DlzZoneDistance.BestLongZone.Value < symbolData.DlzZoneDistance.BestShortZone.Value)
            return CryptoTradeSide.Long;
        else
            return CryptoTradeSide.Short;
    }

}
