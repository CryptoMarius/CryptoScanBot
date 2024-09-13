using System.Text.Json;

using Binance.Net;
using Binance.Net.Clients;
using Binance.Net.ExtensionMethods;
using Binance.Net.Objects.Models.Spot;
using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Exchange.Binance.Spot;

/// <summary>
/// De Trades ophalen
/// </summary>
public class Trade
{
    public static void PickupTrade(CryptoAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BinanceTrade item)
    {
        trade.TradeTime = item.Timestamp;

        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        trade.TradeId = item.Id.ToString();
        trade.OrderId = item.OrderId.ToString();

        trade.Price = item.Price;
        trade.Quantity = item.Quantity;
        trade.QuoteQuantity = item.Price * item.Quantity;
        trade.Commission = item.Fee;
        trade.CommissionAsset = item.FeeAsset;
    }


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

            if (position.Symbol.LastTradeFetched == null)
            {
                isChanged = true;
                position.Symbol.LastTradeFetched = position.CreateTime.AddMinutes(-1);
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
                    GlobalData.AddTextToLogTab($"{ExchangeBase.ExchangeOptions.ExchangeName} delay needed for weight: {weight} (because of rate limits)");
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
                        string tradeId = item.Id.ToString();
                        string orderId = item.OrderId.ToString();

                        if (position.StepOrderList.TryGetValue(orderId, out var order))
                        {
                            CryptoTrade? trade = position.TradeList.Find(tradeId);
                            if (trade == null)
                            {
                                trade = new()
                                {
                                    TradeAccount = position.Account!,
                                    Exchange = position.Exchange,
                                    Symbol = position.Symbol,
                                };
                                PickupTrade(position.Account, position.Symbol, trade, item);
                                string text = JsonSerializer.Serialize(item, ExchangeHelper.JsonSerializerNotIndented).Trim();
                                ScannerLog.Logger.Trace($"{item.Symbol} Trade added json={text}");
#if DEBUG
                                ScannerLog.Logger.Debug($"{item} json={text}");
#endif
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
                    if (result.Data.Count() < 1000)
                        break;
                }
            }



            if (tradeCache.Count > 0 || isChanged)
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

