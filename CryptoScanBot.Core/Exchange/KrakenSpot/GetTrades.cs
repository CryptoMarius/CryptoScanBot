using CryptoExchange.Net.Objects;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

using Kraken.Net.Clients;
using Kraken.Net.Objects.Models;

namespace CryptoScanBot.Core.Exchange.KrakenSpot;

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
        using KrakenRestClient client = new();
        int tradeCount = 0;
        try
        {
            // TODO
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
                LimitRates.WaitForFairWeight(1); // *5x ivm API weight waarschuwingen

                // TODO: Dit moet nog omgeschreven worden, want zo werkt deze methode niet goed
                WebCallResult<KrakenUserTradesPage> result = await client.SpotApi.Trading.GetUserTradesAsync();
                if (!result.Success)
                {
                    GlobalData.AddTextToLogTab("error getting mytrades " + result.Error);
                }


                if (result.Data != null)
                {
                    foreach (var item in result.Data.Trades.Values)
                    {
                        CryptoTrade? trade = position.TradeAccount.TradeList.Find(position.Symbol, item.Id);
                        if (trade == null)
                        {
                            trade = new();
                            Api.PickupTrade(position.TradeAccount, position.Symbol, trade, item);
                            
                            tradeCache.Add(trade);
                            position.TradeAccount.TradeList.Add(trade);

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
            Monitor.Enter(position.TradeAccount.TradeList);
            try
            {
                database.Open();
                using var transaction = database.BeginTransaction();
                GlobalData.AddTextToLogTab("Trades " + position.Symbol.Name + " " + tradeCache.Count.ToString());
                foreach (var trade in tradeCache)
                {
                    database.Connection.Insert(trade, transaction);
                }

                tradeCount += tradeCache.Count;

                if (isChanged)
                    database.Connection.Update(position.Symbol, transaction);
                transaction.Commit();
            }
            finally
            {
                Monitor.Exit(position.TradeAccount.TradeList);
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