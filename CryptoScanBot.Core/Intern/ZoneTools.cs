using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Model;
using CryptoScanBot.Core.Trend;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Intern;
public class ZoneTools
{
    public static void LoadAllZones()
    {
        // Alle openstaande posities lezen 
        GlobalData.AddTextToLogTab("Reading open zones");

        foreach (var x in GlobalData.ActiveAccount!.Data.SymbolDataList.Values)
        {
            x.ResetZoneData();
        }
            

        using var database = new CryptoDatabase();
        string sql = "select * from zone"; // where CloseDate is null
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
                        if (zone.Side == Enums.CryptoTradeSide.Long)
                            symbolData.ZoneListLong.Add(zone);
                        else
                            symbolData.ZoneListShort.Add(zone);
                    }
                }
            }
        }
    }

    public static void SaveZonesForSymbol(CryptoSymbol symbol, List<ZigZagResult> zigZagList)
    {
        using CryptoDatabase databaseThread = new();
        databaseThread.Connection.Open();

        using var transaction = databaseThread.BeginTransaction();
        databaseThread.Connection.Execute($"delete from zone where symbolId={symbol.Id}", transaction);
        transaction.Commit();

        var x = GlobalData.ActiveAccount!.Data.GetSymbolData(symbol.Name);
        x.ResetZoneData();

        foreach (var zigZag in zigZagList)
        {
            if (zigZag.Dominant) //zigZag.Bottom > 0 && zigZag.CloseDate == null
            {
                CryptoZone zone = new()
                {
                    CreateTime = CandleTools.GetUnixDate(zigZag.Candle.OpenTime),
                    AccountId = GlobalData.ActiveAccount.Id,
                    Account = GlobalData.ActiveAccount,
                    ExchangeId = symbol.Exchange.Id,
                    Exchange = symbol.Exchange,
                    SymbolId = symbol.Id,
                    Symbol = symbol,
                    OpenTime = zigZag.Candle.OpenTime,
                    Top = zigZag.Top,
                    Bottom = zigZag.Bottom,
                    Side = CryptoTradeSide.Long,
                    Strategy = CryptoSignalStrategy.DominantLevel,
                    Description = zigZag.Percentage.ToString("N2"),
                };
                if (zigZag.CloseDate != null)
                    zone.CloseDate = zigZag.CloseDate;

                if (zigZag.PointType == 'L')
                {
                    zone.Side = CryptoTradeSide.Long;
                    zone.AlarmPrice = zone.Top * (100 + GlobalData.Settings.Signal.Zones.WarnPercentage) / 100;
                    x.ZoneListLong.Add(zone);
                }
                else
                {
                    zone.Side = CryptoTradeSide.Short;
                    zone.AlarmPrice = zone.Bottom * (100 - GlobalData.Settings.Signal.Zones.WarnPercentage) / 100;
                    x.ZoneListShort.Add(zone);
                }

                databaseThread.Connection.Insert(zone);

            }
        }
    }

}
