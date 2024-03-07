using System.Text.Encodings.Web;
using System.Text.Json;

using Bybit.Net.Clients;
using Bybit.Net.Enums;

using CryptoScanBot.Context;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Exchange.BybitSpot;

#if TRADEBOT
public class FetchTradeForOrder
{

    // Lukt pas vanaf V5, en voor de fee zijn we terug naar V3Spot
//    public static async Task FetchTradesForOrderAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, string orderId)
//    {
//        int tradeCount = 0;
//        try
//        {
//            using BybitRestClient client = Api.CreateRestClient();
//            var result = await client.SpotApiV3.Trading.GetUserTradesAsync(orderId: orderId);
//            if (!result.Success)
//            {
//                GlobalData.AddTextToLogTab($"FetchTradesForOrderAsync: error getting trades order {orderId} {result.Error}");
//            }

//            List<CryptoTrade> tradeCache = [];
//            if (result.Data != null)
//            {
//                foreach (var item in result.Data.List)
//                {
//                    tradeCount++;
//                    if (!symbol.TradeList.TryGetValue(item.TradeId, out CryptoTrade trade))
//                    {
//                        trade = new CryptoTrade();
//                        Api.PickupTrade(tradeAccount, symbol, trade, item);
//                        string text = JsonSerializer.Serialize(item, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = false }).Trim();
//                        GlobalData.AddTextToLogTab($"{item.Symbol} Trade details json={text}");
//                        tradeCache.Add(trade);
//                        GlobalData.AddTrade(trade);
//                    }
//                }
//            }


//            // Verwerk de trades
//            if (tradeAccount.Id > 0)
//            {
//                using CryptoDatabase databaseThread = new();
//                {
//                    databaseThread.Open();
//                    Monitor.Enter(symbol.TradeList);
//                    try
//                    {
//                        using var transaction = databaseThread.BeginTransaction();
//#if SQLDATABASE
//                        databaseThread.BulkInsertTrades(symbol, tradeCache, transaction);
//#else
//                        foreach (var trade in tradeCache)
//                        {
//                            databaseThread.Connection.Insert(trade, transaction);
//                            GlobalData.AddTextToLogTab($"{symbol.Name} fetching trades for orderid {orderId} - added tradeid {trade.TradeId}");
//                        }
//#endif
//                        //GlobalData.AddTextToLogTab($"FetchTradesForOrderAsync {symbol.Name} ORDER {orderId} {tradeCache.Count}");
//                        transaction.Commit();
//                    }
//                    finally
//                    {
//                        Monitor.Exit(symbol.TradeList);
//                    }
//                }
//            }
//        }
//        catch (Exception error)
//        {
//            ScannerLog.Logger.Error(error, "");
//            GlobalData.AddTextToLogTab("error get trades " + error.ToString()); // symbol.Text + " " + 
//        }

//        return;
//    }


}

#endif