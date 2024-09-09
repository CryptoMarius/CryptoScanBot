using System.Text.Json;

using CryptoExchange.Net.Objects;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

using Kraken.Net.Clients;
using Kraken.Net.Objects.Models;

namespace CryptoScanBot.Core.Exchange.Kraken.Spot;

/// <summary>
/// De Trades ophalen
/// </summary>
public class Trade
{
    static public void PickupTrade(CryptoAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, KrakenUserTrade item)
    {
        trade.TradeTime = item.Timestamp;

        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        trade.TradeId = item.Id;
        trade.OrderId = item.OrderId;

        trade.Price = item.Price;
        trade.Quantity = item.Quantity;
        trade.QuoteQuantity = item.Price * item.Quantity;
        trade.Commission = item.Fee;
        trade.CommissionAsset = symbol.Quote; // item.FeeAsset;?
    }


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

            if (position.Symbol.LastTradeFetched == null)
            {
                isChanged = true;
                position.Symbol.LastTradeFetched = position.CreateTime.AddMinutes(-1);
            }

            while (true)
            {
                // Weight verdubbelt omdat deze wel erg aggressief trades ophaalt
                //BinanceWeights.WaitForFairBinanceWeight(5, "mytrades");
                LimitRate.WaitForFairWeight(1); // *5x ivm API weight waarschuwingen

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
                        string tradeId = item.Id;
                        string orderId = item.OrderId.ToString();

                        if (position.StepOrderList.TryGetValue(orderId, out var order))
                        {
                            CryptoTrade? trade = position.TradeList.Find(tradeId);
                            if (trade == null)
                            {
                                trade = new();
                                PickupTrade(position.Account, position.Symbol, trade, item);
                                string text = JsonSerializer.Serialize(item, ExchangeHelper.JsonSerializerNotIndented).Trim();
                                ScannerLog.Logger.Trace($"{item.Symbol} Trade added json={text}");

                                tradeCache.Add(trade);
                                position.TradeList.AddTrade(trade);

                                if (trade.TradeTime > position.Symbol.LastTradeFetched)
                                {
                                    isChanged = true;
                                    position.Symbol.LastTradeFetched = trade.TradeTime;
                                }
                            }
                        }

                    }

                    // Full run, try again
                    if (result.Data.Trades.Count < 1000)
                        break;
                }
            }



            if (tradeCache.Count > 0)
            {
                database.Open();
                GlobalData.AddTextToLogTab("Trades " + position.Symbol.Name + " " + tradeCache.Count.ToString());
                foreach (var trade in tradeCache)
                    database.Connection.Insert(trade);
                tradeCount += tradeCache.Count;

                if (isChanged)
                    database.Connection.Update(position.Symbol);
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

