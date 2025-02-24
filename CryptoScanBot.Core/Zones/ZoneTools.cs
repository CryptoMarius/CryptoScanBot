using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;

namespace CryptoScanBot.Core.Zones;

public class DatabaseStatistics
{
    public int Inserted { get; set; }
    public int Modified { get; set; }
    public int Deleted { get; set; }
    public int Untouched { get; set; }
    public int Total { get; set; }
}


public class ZoneTools
{
    public static void CreateZoneIndex(SortedList<(CryptoTradeSide, long?, decimal, decimal), CryptoZone> zonesFromDatabase, 
        IList<CryptoZone> zones, DatabaseStatistics dbStats)
    {
        foreach (var zone in zones)
        {
            // Warning, there can be duplicate zones, remove them!
            if (zonesFromDatabase.ContainsKey((zone.Side, zone.OpenTime!.Value, zone.Bottom, zone.Top)))
            {
                if (zone.Id > 0)
                {
                    zone.Id *= -1;
                    dbStats.Deleted++;
                    GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                }
            }
            else zonesFromDatabase.Add((zone.Side, zone.OpenTime!.Value, zone.Bottom, zone.Top), zone);
        }
    }


    //public static void CollectAllZones(ZoneDataList zoneData,
    //    SortedList<(CryptoTradeSide, long?, decimal, decimal), CryptoZone> zonesFromDatabase,
    //    DatabaseStatistics dbStats)
    //{
    //    CreateZoneIndex(zonesFromDatabase, zoneData.LongOpen, dbStats);
    //    CreateZoneIndex(zonesFromDatabase, zoneData.ShortOpen, dbStats);
    //    CreateZoneIndex(zonesFromDatabase, zoneData.LongClosed, dbStats);
    //    CreateZoneIndex(zonesFromDatabase, zoneData.ShortClosed, dbStats);
    //}



    public static void AddZonesToInternalLists(ZoneDataList zoneData,
        SortedList<(CryptoTradeSide, long?, decimal, decimal), CryptoZone> zonesFromDatabase,
        IList<CryptoZone> newCalculatedZones, DatabaseStatistics dbStats)
    {
        foreach (var zone in newCalculatedZones)
        {
            // reuse an previous zone from the database if it exists
            bool zoneExistsInDatabase = false;
            if (zonesFromDatabase.TryGetValue((zone.Side, zone.OpenTime, zone.Bottom, zone.Top), out CryptoZone? zoneInDb))
            {
                zone.Id = zoneInDb.Id;
                zoneExistsInDatabase = zoneInDb.Id > 0; // might still be zero

                // nothing important has changed, do not change the zone, skip..
                if (zoneInDb.CloseTime == zone.CloseTime && zoneInDb.Description == zone.Description && 
                    zoneInDb.IsValid == zone.IsValid && zoneInDb.Strength == zone.Strength)
                {
                    zonesFromDatabase.Remove((zone.Side, zone.OpenTime, zone.Bottom, zone.Top));
                    dbStats.Untouched++;
                    //if (data.Symbol.Name == "HMSTRUSDT")
                    //    GlobalData.AddTextToLogTab($"{data.Symbol.Name} reusing={zone.Id} {zone.Kind} {zone.Side} {zone.Bottom:N8} {zone.Top:N8}");
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
                //if (data.Symbol.Name == "HMSTRUSDT")
                //    GlobalData.AddTextToLogTab($"{data.Symbol.Name} modified={zone.Id} {zone.Kind} {zone.Side} {zone.Bottom:N8} {zone.Top:N8}");
            }
            else
            {
                dbStats.Inserted++;
                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                //if (data.Symbol.Name == "HMSTRUSDT")
                //    GlobalData.AddTextToLogTab($"{data.Symbol.Name} inserted={zone.Id} {zone.Kind} {zone.Side} {zone.Bottom:N8} {zone.Top:N8}");
            }
        }


        dbStats.Total = zoneData.LongOpen.Count + zoneData.LongClosed.Count + zoneData.ShortOpen.Count + zoneData.ShortClosed.Count;
    }



    public static void DeleteRemainingZones(SortedList<(CryptoTradeSide, long?, decimal, decimal), CryptoZone> zonesFromDatabase, DatabaseStatistics dbStats)
    {
        // delete the remaining zones
        foreach (var zone in zonesFromDatabase.Values)
        {
            if (zone.Id != 0)
            {
                zone.Id *= -1;
                dbStats.Deleted++;
                GlobalData.ThreadSaveObjects!.AddToQueue(zone);
                //if (data.Symbol.Name == "HMSTRUSDT")
                //    GlobalData.AddTextToLogTab($"{data.Symbol.Name} deleting={zone.Id} {zone.Kind} {zone.Side} {zone.Bottom:N8} {zone.Top:N8}");
            }
        }
    }


    public static decimal? ZoneDistance(CryptoSymbol symbol)
    {
        // Set the date of the last swing point for the automatic zone calculation
        AccountSymbol symbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);

        // no data
        if (symbolData.BestLongZone == null && symbolData.BestShortZone == null)
            return null;

        // only one of them
        if (symbolData.BestLongZone == null)
            return symbolData.BestShortZone;
        if (symbolData.BestShortZone == null)
            return symbolData.BestLongZone;

        // which of the two is closeby
        return Math.Min(symbolData.BestLongZone.Value, symbolData.BestShortZone.Value);
    }


    public static CryptoTradeSide? ZoneTradeSide(CryptoSymbol symbol)
    {
        // Set the date of the last swing point for the automatic zone calculation
        AccountSymbol symbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);

        // no data
        if (symbolData.BestLongZone == null && symbolData.BestShortZone == null)
            return null;

        // only one of them
        if (symbolData.BestLongZone == null)
            return CryptoTradeSide.Short;
        if (symbolData.BestShortZone == null)
            return CryptoTradeSide.Long;


        // which of the two is closeby
        if (symbolData.BestLongZone.Value < symbolData.BestShortZone.Value)
            return CryptoTradeSide.Long;
        else
            return CryptoTradeSide.Short;
    }

}
