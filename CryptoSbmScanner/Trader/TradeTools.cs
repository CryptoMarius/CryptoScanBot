using CryptoSbmScanner.Context;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;
using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Trader;

#if TRADEBOT
public class TradeTools
{
    static public void LoadAssets()
    {
        GlobalData.AddTextToLogTab("Reading asset information");

        // ALLE assets laden
        foreach (var account in GlobalData.ActiveTradeAccountList.Values.ToList())
        {
            account.AssetList.Clear();
        }


        using var database = new CryptoDatabase();
        foreach (CryptoAsset asset in database.Connection.GetAll<CryptoAsset>())
        {
            if (GlobalData.ActiveTradeAccountList.TryGetValue(asset.TradeAccountId, out var account))
            {
                account.AssetList.TryAdd(asset.Name, asset);
            }
        }
       
    }


    static public void LoadClosedPositions()
    {
        // Alle gesloten posities lezen 
        // TODO - beperken tot de laatste 2 dagen? (en wat handigheden toevoegen wellicht)
        GlobalData.AddTextToLogTab("Reading closed position");
#if SQLDATABASE
        string sql = "select top 250 * from position where not closetime is null order by id desc";
#else
        string sql = "select * from position where not closetime is null order by id desc limit 250";
#endif
        using var database = new CryptoDatabase();
        foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql))
            PositionTools.AddPositionClosed(position);
    }

    static public void LoadOpenPositions()
    {
        // Alle openstaande posities lezen 
        GlobalData.AddTextToLogTab("Reading open position");

        using var database = new CryptoDatabase();
        string sql = "select * from position where closetime is null and status < 2";
        foreach (CryptoPosition position in database.Connection.Query<CryptoPosition>(sql))
        {
            if (!GlobalData.TradeAccountList.TryGetValue(position.TradeAccountId, out CryptoTradeAccount tradeAccount))
                throw new Exception("Geen trade account gevonden");

            PositionTools.AddPosition(tradeAccount, position);
            PositionTools.LoadPosition(database, position);
        }
    }

    static async public Task CheckOpenPositions()
    {
        // Alle openstaande posities lezen 
        GlobalData.AddTextToLogTab("Checking open positions");

        using var database = new CryptoDatabase();
        foreach (CryptoTradeAccount tradeAccount in GlobalData.TradeAccountList.Values.ToList())
        {
            //foreach (CryptoPosition position in tradeAccount.PositionList.Values.ToList())
            foreach (var positionList in tradeAccount.PositionList.Values.ToList())
            {
                foreach (var position in positionList.Values.ToList())
                {
                    // Controleer de openstaande orders, zijn ze ondertussen gevuld
                    // Haal de trades van deze positie op vanaf de 1e order
                    // TODO - Hoe doen we dit met papertrading (er is niets geregeld!)
                    await PositionTools.LoadTradesfromDatabaseAndExchange(database, position);
                    PositionTools.CalculatePositionResultsViaTrades(database, position);
                }
            }
        }
    }

}
#endif
