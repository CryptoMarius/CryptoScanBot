using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.ExtensionMethods;
using Binance.Net.Objects.Models.Spot;

using CryptoScanBot.Context;
using CryptoScanBot.Intern;
using CryptoScanBot.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Exchange.BinanceSpot;

#if TRADEBOT
/// <summary>
/// De Trades ophalen
/// </summary>
public class BinanceFetchTrades
{
    /// <summary>
    /// Haal de trades van 1 symbol op
    /// </summary>
    public static async Task<int> FetchTradesForSymbolAsync(CryptoDatabase database, CryptoPosition position)
    {
        int tradeCount = 0;
        using BinanceRestClient client = new();
        try
        {
            // Haal de trades op van 1 symbol

            bool isChanged = false;
            List<CryptoTrade> tradeCache = [];

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

                var result = await client.SpotApi.Trading.GetUserTradesAsync(position.Symbol.Name, null, position.Symbol.LastTradeFetched, null, 1000);
                if (!result.Success)
                {
                    GlobalData.AddTextToLogTab("error getting mytrades " + result.Error);
                }

                // Als we over het randje gaan qua API verzoeken even inhouden
                int? weight = result.ResponseHeaders.UsedWeight();
                if (weight > 700)
                {
                    GlobalData.AddTextToLogTab($"{Api.ExchangeOptions.ExchangeName} delay needed for weight: {weight} (because of rate limits)");
                    if (weight > 800)
                        await Task.Delay(10000);
                    if (weight > 900)
                        await Task.Delay(10000);
                    if (weight > 1000)
                        await Task.Delay(15000);
                    if (weight > 1100)
                        await Task.Delay(15000);
                }

                if (result.Data != null)
                {
                    foreach (BinanceTrade item in result.Data)
                    {
                        if (!position.Symbol.TradeList.TryGetValue(item.Id.ToString(), out CryptoTrade trade))
                        {
                            trade = new CryptoTrade();
                            Api.PickupTrade(position.TradeAccount, position.Symbol, trade, item);


                            // TODO! Converteer de BNB of enzovoort naar de QUOTE van de BASE

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
                    if (result.Data.Count() < 1000)
                        break;
                }
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