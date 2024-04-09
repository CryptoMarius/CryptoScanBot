using Kraken.Net.Clients;
using Kraken.Net.Enums;

using CryptoScanBot.Context;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

using Dapper.Contrib.Extensions;
using Kraken.Net.Objects.Models;
using CryptoExchange.Net.Objects;

namespace CryptoScanBot.Exchange.Kraken;

#if TRADEBOT
public class FetchTradeForOrder
{
    public static async Task FetchTradesForOrderAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol, string orderId)
    {
        //int tradeCount = 0;
        try
        {
            // TODO
            using KrakenRestClient client = new();
            //var result = await client.SpotApi.Trading.GetUserTradesAsync(symbol.Name, orderId: int.Parse(orderId));
            WebCallResult<KrakenUserTradesPage> result = null;
            if (!result.Success)
            {
                GlobalData.AddTextToLogTab($"FetchTradesForOrderAsync: error getting trades order {orderId} {result.Error}");
            }

            List<CryptoTrade> tradeCache = new();
            if (result.Data != null)
            {
                //foreach (var item in result.Data)
                //{
                //    tradeCount++;
                //    if (!symbol.TradeList.TryGetValue(item.Id, out CryptoTrade trade))
                //    {
                //        trade = new CryptoTrade();
                //        Api.PickupTrade(tradeAccount, symbol, trade, item);
                //        tradeCache.Add(trade);
                //        GlobalData.AddTrade(trade);
                //    }
                //}
            }


            // Verwerk de trades
            if (tradeAccount.Id > 0)
            {
                using CryptoDatabase databaseThread = new();
                {
                    databaseThread.Open();
                    Monitor.Enter(symbol.TradeList);
                    try
                    {
                        using var transaction = databaseThread.BeginTransaction();
#if SQLDATABASE
                        databaseThread.BulkInsertTrades(symbol, tradeCache, transaction);
#else
                        foreach (var trade in tradeCache)
                        {
                            databaseThread.Connection.Insert(trade, transaction);
                            GlobalData.AddTextToLogTab($"FetchTradesForOrderAsync: {symbol.Name} ORDER {orderId} TRADE {trade.TradeId} toegevoegd!");
                        }
#endif
                        GlobalData.AddTextToLogTab($"FetchTradesForOrderAsync {symbol.Name} ORDER {orderId} {tradeCache.Count}");
                        transaction.Commit();
                    }
                    finally
                    {
                        Monitor.Exit(symbol.TradeList);
                    }
                }
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("error get trades " + error.ToString()); // symbol.Text + " " + 
        }

        return;
    }


}

#endif