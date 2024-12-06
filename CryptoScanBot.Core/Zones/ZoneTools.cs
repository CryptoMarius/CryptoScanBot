﻿using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Zones;
public class ZoneTools
{
    public static void LoadAllZones()
    {
        // Alle openstaande posities lezen 
        //GlobalData.AddTextToLogTab("Reading open zones");

        foreach (var x in GlobalData.ActiveAccount!.Data.SymbolDataList.Values)
        {
            x.ResetZoneData();
        }


        using var database = new CryptoDatabase();
        string sql = "select * from zone"; // where CloseTime is null
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
                            symbolData.ZoneListLong.Add(zone.Top, zone);
                        else
                            symbolData.ZoneListShort.Add(zone.Bottom, zone);
                    }
                }
            }
        }
    }

    public static void SaveZonesForSymbol(CryptoSymbol symbol, CryptoInterval interval, List<ZigZagResult> zigZagList)
    {
        using CryptoDatabase databaseThread = new();
        databaseThread.Connection.Open();

        //using var transaction = databaseThread.BeginTransaction();
        //databaseThread.Connection.Execute($"delete from zone where symbolId={symbol.Id}", transaction);
        //transaction.Commit();



        var symbolData = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);

        // Oops, there are duplicate zones (strange, didn't expect this!)
        // We remove the duplicates with this code which is fine for now.
        // (still curious why this is happening..... ENAUSDT 14 Aug 23:00 UTC)
        SortedList<(decimal, decimal, CryptoTradeSide), CryptoZone> zoneIndex = [];
        foreach (var zone in symbolData.ZoneListLong.Values)
            zoneIndex.TryAdd((zone.Bottom, zone.Top, zone.Side), zone);
        foreach (var zone in symbolData.ZoneListShort.Values)
            zoneIndex.TryAdd((zone.Bottom, zone.Top, zone.Side), zone);
        symbolData.ResetZoneData();

        int inserted = 0;
        int deleted = 0;
        int modified = 0;
        foreach (var zigZag in zigZagList)
        {
            if (zigZag.Dominant && !zigZag.Dummy && zigZag.IsValid) // all zones (also the closed ones)
            {
                CryptoTradeSide side = zigZag.PointType == 'L' ? CryptoTradeSide.Long : CryptoTradeSide.Short;

                // Try to reuse the previous zones.
                if (!zoneIndex.TryGetValue((zigZag.Bottom, zigZag.Top, side), out CryptoZone? zone))
                {
                    zone = new()
                    {
                        CreateTime = DateTime.UtcNow,
                        AccountId = GlobalData.ActiveAccount.Id,
                        Account = GlobalData.ActiveAccount,
                        ExchangeId = symbol.Exchange.Id,
                        Exchange = symbol.Exchange,
                        SymbolId = symbol.Id,
                        Symbol = symbol,
                        OpenTime = zigZag.Candle.OpenTime,
                        Top = zigZag.Top,
                        Bottom = zigZag.Bottom,
                        Side = side,
                        Strategy = CryptoSignalStrategy.DominantLevel,
                        Description = $"{interval.Name}: {zigZag.Percentage:N2}%",
                    };
                }
                zone.CloseTime = zigZag.CloseDate;


                if (side == CryptoTradeSide.Long)
                {
                    zone.AlarmPrice = zone.Top * (100 + GlobalData.Settings.Signal.Zones.WarnPercentage) / 100;
                    symbolData.ZoneListLong.Add(zone.Top, zone);
                }
                else
                {
                    zone.AlarmPrice = zone.Bottom * (100 - GlobalData.Settings.Signal.Zones.WarnPercentage) / 100;
                    symbolData.ZoneListShort.Add(zone.Bottom, zone);
                }

                if (zone.Id > 0)
                {
                    modified++;
                    databaseThread.Connection.Update(zone);
                }
                else
                {
                    inserted++;
                    databaseThread.Connection.Insert(zone);
                }
                zoneIndex.Remove((zigZag.Bottom, zigZag.Top, side));
            }
        }

        // delete the remaining zones
        foreach (var zone in zoneIndex.Values)
        {
            deleted++;
            databaseThread.Connection.Delete(zone);
        }
        GlobalData.AddTextToLogTab($"{symbol.Name} Zones calculated, inserted={inserted} modified={modified} deleted={deleted}");
    }

}