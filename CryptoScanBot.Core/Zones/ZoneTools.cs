using CryptoScanBot.Core.Account;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

using Dapper;

namespace CryptoScanBot.Core.Zones;

public class ZoneTools
{
    public static void LoadAllZones()
    {
        // Alle openstaande posities lezen 
        //GlobalData.AddTextToLogTab("Reading open zones");

        foreach (var x in GlobalData.ActiveAccount!.Data.SymbolDataList.Values.ToList())
        {
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
                        if (zone.Side == CryptoTradeSide.Long)
                            symbolData.ZoneListLong.Add(zone);
                        else
                            symbolData.ZoneListShort.Add(zone);
                        todoSorting.TryAdd(symbol.Name, symbolData);

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
            zoneIndex.TryAdd((zone.Bottom, zone.Top, zone.Side), zone);
        foreach (var zone in data.ZoneListShort)
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

}
