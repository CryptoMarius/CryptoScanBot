using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.Spot;

using CryptoSbmScanner.Context;
using CryptoSbmScanner.Intern;
using CryptoSbmScanner.Model;

using Dapper.Contrib.Extensions;

namespace CryptoSbmScanner.Exchange.BybitFutures;

#if TRADEBOT
/// <summary>
/// De Trades ophalen
/// </summary>
public class FetchTrades
{
    //Om meerdere updates te voorkomen (gebruiker die meerdere keren erop klikt)
    static private readonly SemaphoreSlim Semaphore = new(1);
    static public int tradeCount;

    /// <summary>
    /// Haal de trades van 1 symbol op
    /// </summary>
    public static async Task<int> FetchTradesForSymbolAsync(CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        using BybitRestClient client = new();
        return await FetchTradesInternal(client, tradeAccount, symbol);
    }

    private static async Task<int> FetchTradesInternal(BybitRestClient client, CryptoTradeAccount tradeAccount, CryptoSymbol symbol)
    {
        int tradeCount = 0;
        try
        {
            // Haal de trades op van 1 symbol

            bool isChanged = false;
            List<CryptoTrade> tradeCache = new();

            //Verzin een begin datum
            if (symbol.LastTradeFetched == null)
            {
                isChanged = true;
                symbol.LastTradeFetched = DateTime.Today.AddMonths(-2);
            }

            while (true)
            {
                // Weight verdubbelt omdat deze wel erg aggressief trades ophaalt
                //BinanceWeights.WaitForFairBinanceWeight(5, "mytrades");
                LimitRates.WaitForFairWeight(1); // *5x ivm API weight waarschuwingen

                // CRAP, bybit doet het door middel van ID's ;-) symbol.LastTradeFetched
                var result = await client.V5Api.Trading.GetUserTradesAsync(Category.Linear, symbol.Name, null, null, null, symbol.LastTradeFetched, null, null, 1000);
                if (!result.Success)
                {
                    GlobalData.AddTextToLogTab("error getting mytrades " + result.Error);
                }


                if (result.Data != null)
                {
                    foreach (var item in result.Data.List)
                    {
                        if (!symbol.TradeList.TryGetValue(item.TradeId, out CryptoTrade trade))
                        {
                            trade = new CryptoTrade();
                            Api.PickupTrade(tradeAccount, symbol, trade, item);
                            tradeCache.Add(trade);

                            GlobalData.AddTrade(trade);

                            //De fetch administratie bijwerken
                            if (trade.TradeTime > symbol.LastTradeFetched)
                            {
                                isChanged = true;
                                symbol.LastTradeFetched = trade.TradeTime;
                            }
                        }
                    }

                    //We hebben een volledige aantal trades meegekregen, nog eens proberen
                    if (result.Data.List.Count() < 1000)
                        break;
                }
            }



            // Verwerk de trades
            if (tradeAccount.Id > 0) // debug
            {
                using CryptoDatabase databaseThread = new();
                {
                    // Extra close vanwege transactie problemen (hergebuik van connecties wellicht?)
                    databaseThread.Close();
                    databaseThread.Open();
                    try
                    {
                        //GlobalData.AddTextToLogTab(symbol.Name);
                        Monitor.Enter(symbol.TradeList);
                        try
                        {
                            using (var transaction = databaseThread.BeginTransaction())
                            {
                                GlobalData.AddTextToLogTab("Trades " + symbol.Name + " " + tradeCache.Count.ToString());
#if SQLDATABASE
                                databaseThread.BulkInsertTrades(symbol, tradeCache, transaction);
#else
                                foreach (var trade in tradeCache)
                                {
                                    databaseThread.Connection.Insert(trade, transaction);
                                }
#endif

                                tradeCount += tradeCache.Count;

                                if (isChanged)
                                    databaseThread.Connection.Update(symbol, transaction);
                                transaction.Commit();
                            }
                        }
                        finally
                        {
                            Monitor.Exit(symbol.TradeList);
                        }
                    }
                    finally
                    {
                        databaseThread.Close();
                    }
                }
            }
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("error get prices " + error.ToString()); // symbol.Text + " " + 
        }

        return tradeCount;
    }


    private static async Task<int> ExecuteInternalAsync(Queue<CryptoSymbol> queue)
    {
        int tradeCount = 0;
        try
        {
            // We hergebruiken de client binnen deze thread, teveel connecties opnenen resulteerd in een foutmelding:
            // "An operation on a socket could not be performed because the system lacked sufficient buffer space or because a queue was full"
            using BybitRestClient client = new();
            {
                while (true)
                {
                    CryptoSymbol symbol;

                    // Omdat er meer threads bezig zijn moet de queue gelocked worden
                    Monitor.Enter(queue);
                    try
                    {
                        if (queue.Count > 0)
                            symbol = queue.Dequeue();
                        else
                            break;
                    }
                    finally
                    {
                        Monitor.Exit(queue);
                    }

                    tradeCount += await FetchTradesInternal(client, GlobalData.ExchangeRealTradeAccount, symbol);
                }
            }
        }
        catch (Exception error)
        {
            GlobalData.Logger.Error(error);
            GlobalData.AddTextToLogTab("error getting trades " + error.ToString()); // symbol.Text + " " + 
        }
        return tradeCount;
    }

    public static async Task ExecuteAsync()
    {
        if (GlobalData.ExchangeListName.TryGetValue(Api.ExchangeName, out Model.CryptoExchange exchange))
        {
            try
            {
                int tradeCount = 0;

                //Zorgen dat er maar 1 thread tegelijk loopt die de Trades ophaal
                //(want dan krijgen we dubbele Trades, dus blokkeren die hap)
                await Semaphore.WaitAsync();
                try
                {
                    GlobalData.AddTextToLogTab("\r\n\r\n" + "Trades ophalen");

                    Queue<CryptoSymbol> queue = new();
                    foreach (var symbol in exchange.SymbolListName.Values)
                    {
                        //if (symbol.Quote.Equals(quoteData.Name) && (symbol.Status == 1) && (!symbol.IsBarometerSymbol()))
                        //if (CandleTools.MatchingQuote(symbol) && (symbol.Status == 1) && (!symbol.IsBarometerSymbol()))
                        if (symbol.QuoteData.FetchCandles && symbol.Status == 1 && !symbol.IsBarometerSymbol())
                            queue.Enqueue(symbol);
                    }

                    // En dan door x tasks de queue leeg laten trekken
                    List<Task> taskList = new();
                    while (taskList.Count < 3)
                    {
                        Task task = Task.Run(async () => { tradeCount += await ExecuteInternalAsync(queue); });
                        taskList.Add(task);
                    }
                    Task t = Task.WhenAll(taskList);
                    t.Wait();


                    GlobalData.AddTextToLogTab(string.Format("Trades ophalen klaar ({0} records)", tradeCount), true);
                }
                finally
                {
                    Semaphore.Release();
                }

            }
            catch (Exception error)
            {
                GlobalData.Logger.Error(error);
                GlobalData.AddTextToLogTab("error get trades " + error.ToString());
            }
        }

    }
}

#endif