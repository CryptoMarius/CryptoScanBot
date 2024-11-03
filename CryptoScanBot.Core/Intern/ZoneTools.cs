using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Model;

using Dapper;

namespace CryptoScanBot.Core.Intern;
public class ZoneTools
{
    public static void LoadActiveZones()
    {
        // Alle openstaande posities lezen 
        GlobalData.AddTextToLogTab("Reading open zones");

        foreach (var x in GlobalData.ActiveAccount.Data.SymbolDataList.Values)
        {
            x.ResetZoneData();
        }
            

        using var database = new CryptoDatabase();
        string sql = "select * from zone where ExpirationDate is null";
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
}
