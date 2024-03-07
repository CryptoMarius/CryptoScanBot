using System.Text.Encodings.Web;
using System.Text.Json;

using Bybit.Net.Clients;

using CryptoScanBot.Context;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Exchange.BybitSpot;

#if TRADEBOT
/// <summary>
/// De Trades ophalen
/// </summary>
public class FetchTrades
{
    /// <summary>
    /// Haal de trades van 1 symbol op
    /// </summary>
    public static async Task<int> FetchTradesForSymbolAsync(CryptoDatabase database, CryptoPosition position)
    {
        using BybitRestClient client = new();
        int tradeCount = 0;
        try
        {
            // Haal de trades op van 1 symbol

            bool isChanged = false;
            long? fromId = position.Symbol.LastTradeIdFetched;
            long? toId = null;
            List<CryptoTrade> tradeCache = [];

            while (true)
            {
                if (fromId != null)
                {
                    fromId += 1;
                    toId = fromId + 500;
                }

                //BinanceWeights.WaitForFairBinanceWeight(5, "mytrades");
                LimitRates.WaitForFairWeight(1);

                var result = await client.SpotApiV3.Trading.GetUserTradesAsync(position.Symbol.Name, fromId: fromId, toId: toId, limit: 1000);
                if (!result.Success)
                {
                    GlobalData.AddTextToLogTab("error getting mytrades " + result.Error);
                    break;
                }


                // We hebben een volledige aantal trades meegekregen, nog eens proberen
                if (result.Data != null && result.Data.Any())
                {
                    foreach (var item in result.Data)
                    {
                        if (!position.Symbol.TradeList.TryGetValue(item.TradeId.ToString(), out CryptoTrade trade))
                        {
                            trade = new CryptoTrade();
                            Api.PickupTrade(position.TradeAccount, position.Symbol, trade, item);
                            string text = JsonSerializer.Serialize(item, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = false }).Trim();
                            GlobalData.AddTextToLogTab($"{item.Symbol} Trade 'input' details json={text}");
                            //text = JsonSerializer.Serialize(trade, new JsonSerializerOptions { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, WriteIndented = false }).Trim();
                            //GlobalData.AddTextToLogTab($"{item.Symbol} Trade 'trade' details json={text}");

                            tradeCache.Add(trade);
                            GlobalData.AddTrade(trade);
                        }

                        // De administratie bijwerken (de Id en TradeId zijn twee verschillende getallen)
                        if (!position.Symbol.LastTradeIdFetched.HasValue || item.Id > position.Symbol.LastTradeIdFetched)
                        {
                            isChanged = true;
                            fromId = item.Id;
                            position.Symbol.LastTradeIdFetched = item.Id;
                            position.Symbol.LastTradeFetched = trade.TradeTime;
                        }
                    }

                }
                else break;
            }



            // Verwerk de trades
            //GlobalData.AddTextToLogTab(symbol.Name);
            Monitor.Enter(position.Symbol.TradeList);
            try
            {
                database.Open();
                using var transaction = database.BeginTransaction();
                GlobalData.AddTextToLogTab("Trades " + position.Symbol.Name + " " + tradeCache.Count.ToString());
#if SQLDATABASE
                databaseThread.BulkInsertTrades(symbol, tradeCache, transaction);
#else
                foreach (var trade in tradeCache)
                {
                    database.Connection.Insert(trade, transaction);
                }
#endif

                tradeCount += tradeCache.Count;

                if (isChanged)
                    database.Connection.Update(position.Symbol, transaction);
                transaction.Commit();
            }
            finally
            {
                Monitor.Exit(position.Symbol.TradeList);
            }
        }
        catch (Exception error)
        {
            ScannerLog.Logger.Error(error, "");
            GlobalData.AddTextToLogTab("error get trades " + error.ToString()); // symbol.Text + " " + 
        }

        return tradeCount;
    }



}

#endif