using System.Text.Encodings.Web;
using System.Text.Json;

using Bybit.Net.Clients;
using Bybit.Net.Enums;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Exchange.BybitFutures;

#if TRADEBOT
/// <summary>
/// De Trades ophalen
/// </summary>
public class GetTrades
{
    /// <summary>
    /// Haal de trades van 1 symbol op
    /// </summary>
    public static async Task<int> FetchTradesForSymbolAsync(CryptoDatabase database, CryptoPosition position)
    {
        int tradeCount = 0;
        using BybitRestClient client = new();
        try
        {
            // Haal de trades op van 1 symbol

            bool isChanged = false;
            DateTime? startTime;
            List<CryptoTrade> tradeCache = [];

            //Verzin een begin datum
            if (position.Symbol.LastTradeFetched == null)
            {
                isChanged = true;
                position.Symbol.LastTradeFetched = DateTime.Today.AddMonths(-2);
            }
            // Bybit doet het alleen in blokken van 7 dagen
            startTime = position.Symbol.LastTradeFetched;

            while (startTime < DateTime.UtcNow)
            {
                // Weight verdubbelt omdat deze wel erg aggressief trades ophaalt
                //BinanceWeights.WaitForFairBinanceWeight(5, "mytrades");
                LimitRates.WaitForFairWeight(1); // *5x ivm API weight waarschuwingen

                //var result = await client.V5Api.Trading.GetUserTradesAsync(Category.Linear, symbol.Name, null, null, null,
                //    symbol.LastTradeFetched, null, null, limit = 1000);

                // If only startTime is passed, return range between startTime and startTime+7days!!

                var result = await client.V5Api.Trading.GetUserTradesAsync(Category.Linear, position.Symbol.Name, startTime: startTime, limit: 1000);
                if (!result.Success)
                {
                    GlobalData.AddTextToLogTab("error retreiving mytrades " + result.Error);
                }


                if (result.Data != null)
                {
                    foreach (var item in result.Data.List)
                    {
                        if (!position.Symbol.TradeList.TryGetValue(item.TradeId, out CryptoTrade? trade))
                        {
                            trade = new CryptoTrade();
                            Api.PickupTrade(position.TradeAccount, position.Symbol, trade, item);
                            string text = JsonSerializer.Serialize(item, ExchangeHelper.JsonSerializerNotIndented).Trim();
                            ScannerLog.Logger.Trace($"{item.Symbol} Trade added json={text}");

                            tradeCache.Add(trade);

                            GlobalData.AddTrade(trade);

                            //De fetch administratie bijwerken
                            if (trade.TradeTime > position.Symbol.LastTradeFetched)
                            {
                                isChanged = true;
                                position.Symbol.LastTradeFetched = trade.TradeTime;
                            }
                        }
                    }

                    //We hebben een volledige aantal trades meegekregen, nog eens proberen
                    if (!result.Success)
                        break;
                }

                if (startTime > position.Symbol.LastTradeFetched)
                {
                    isChanged = true;
                    position.Symbol.LastTradeFetched = startTime;
                }
                startTime = startTime?.AddDays(7);
            }



            // Verwerk de trades
            if (position.TradeAccount.Id > 0) // debug
            {
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