using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.V5;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Core;
using CryptoScanBot.Core.Json;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

using System.Text.Json;

namespace CryptoScanBot.Core.Exchange.BybitApi.Futures;

/// <summary>
/// De Trades ophalen
/// </summary>
public class Trade() : TradeBase(), ITrade
{
    static public void PickupTrade(CryptoAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BybitUserTrade item)
    {
        trade.TradeTime = item.Timestamp;

        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        trade.TradeId = item.TradeId;
        trade.OrderId = item.OrderId;

        trade.Price = item.Price;
        trade.Quantity = item.Quantity;
        trade.QuoteQuantity = item.Price * item.Quantity;
        trade.Commission = item.Fee ?? 0;
        trade.CommissionAsset = symbol.Quote; // item.FeeAsset;?
    }


    /// <summary>
    /// Haal de trades van 1 symbol op
    /// </summary>
    public async Task<int> GetTradesAsync(CryptoDatabase database, CryptoPosition position)
    {
        int tradeCount = 0;
        using BybitRestClient client = new();
        try
        {
            // Haal de trades op van 1 symbol

            bool isChanged = false;
            List<CryptoTrade> tradeCache = [];

            //Verzin een begin datum
            if (position.Symbol.LastTradeFetched == null)
            {
                isChanged = true;
                position.Symbol.LastTradeFetched = position.CreateTime.AddMinutes(-1);
            }
            // Bybit doet het alleen in blokken van 7 dagen
            DateTime? startTime = position.Symbol.LastTradeFetched;

            while (startTime < DateTime.UtcNow)
            {
                // Weight verdubbelt omdat deze wel erg aggressief trades ophaalt
                //BinanceWeights.WaitForFairBinanceWeight(5, "mytrades");
                LimitRate.WaitForFairWeight(1); // *5x ivm API weight waarschuwingen

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
                        string tradeId = item.TradeId;
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
                                string text = JsonSerializer.Serialize(item, JsonTools.JsonSerializerNotIndented).Trim();
                                ScannerLog.Logger.Trace($"{item.Symbol} Trade added json={text}");

                                tradeCache.Add(trade);
                                position.TradeList.AddTrade(trade);
                            }
                        }

                        if (item.Timestamp > position.Symbol.LastTradeFetched)
                        {
                            isChanged = true;
                            position.Symbol.LastTradeFetched = item.Timestamp;
                        }
                    }

                    // Full run, try again
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

