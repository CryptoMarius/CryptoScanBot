using CryptoExchange.Net.Objects;

using CryptoScanBot.Context;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

using Dapper.Contrib.Extensions;

using Kraken.Net.Clients;
using Kraken.Net.Objects.Models;

namespace CryptoScanBot.Exchange.Kraken;

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
        using KrakenRestClient client = new();
        int tradeCount = 0;
        try
        {
            // TODO
            // Haal de trades op van 1 symbol

            bool isChanged = false;
            List<CryptoTrade> tradeCache = new();

            //Verzin een begin datum
            if (position.Symbol.LastTradeFetched == null)
            {
                isChanged = true;
                position.Symbol.LastTradeFetched = DateTime.Today.AddMonths(-2);
            }

            while (true)
            {
                // Weight verdubbelt omdat deze wel erg aggressief trades ophaalt
                //BinanceWeights.WaitForFairBinanceWeight(5, "mytrades");
                LimitRates.WaitForFairWeight(1); // *5x ivm API weight waarschuwingen

                // CRAP, bybit doet het door middel van ID's ;-) symbol.LastTradeFetched
                // Wederom een uitdaging!
                WebCallResult<KrakenUserTradesPage> result = null;
                //var result = await client.SpotApi.Trading.GetUserTradesAsync(symbol.Name, startTime: symbol.LastTradeFetched);
                if (!result.Success)
                {
                    GlobalData.AddTextToLogTab("error getting mytrades " + result.Error);
                }


                if (result.Data != null)
                {
                    foreach (var item in result.Data.Trades.Values)
                    {
                        if (!position.Symbol.TradeList.TryGetValue(item.Id, out CryptoTrade trade))
                        {
                            trade = new CryptoTrade();
                            Api.PickupTrade(position.TradeAccount, position.Symbol, trade, item);
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
                    if (result.Data.Trades.Count < 1000)
                        break;
                }
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
                foreach (var x in tradeCache)
                {
                    database.Connection.Insert(position.Symbol, transaction);
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
            GlobalData.AddTextToLogTab("error get prices " + error.ToString()); // symbol.Text + " " + 
        }

        return tradeCount;
    }




}

#endif