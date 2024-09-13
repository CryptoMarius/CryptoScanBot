using System.Text.Json;

using Bybit.Net.Clients;
using Bybit.Net.Enums;
using Bybit.Net.Objects.Models.Spot;
using Bybit.Net.Objects.Models.V5;

using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Exchange.BybitApi.Spot;

/// <summary>
/// De Trades ophalen
/// </summary>
public class Trade
{
    static public void PickupTradeV3(CryptoAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BybitSpotUserTradeV3 item)
    {
        trade.TradeTime = item.TradeTime;

        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        trade.TradeId = item.TradeId.ToString();
        trade.OrderId = item.OrderId.ToString();

        trade.Price = item.Price;
        trade.Quantity = item.Quantity;
        trade.QuoteQuantity = item.Price * item.Quantity;
        trade.Commission = item.Fee;
        trade.CommissionAsset = item.FeeAsset;
    }


    static public void PickupTrade(CryptoAccount tradeAccount, CryptoSymbol symbol, CryptoTrade trade, BybitUserTrade item)
    {
        trade.TradeTime = item.Timestamp;

        trade.TradeAccount = tradeAccount;
        trade.TradeAccountId = tradeAccount.Id;
        trade.Exchange = symbol.Exchange;
        trade.ExchangeId = symbol.ExchangeId;
        trade.Symbol = symbol;
        trade.SymbolId = symbol.Id;

        trade.TradeId = item.TradeId.ToString();
        trade.OrderId = item.OrderId.ToString();

        trade.Price = item.Price;
        trade.Quantity = item.Quantity;
        trade.QuoteQuantity = item.Price * item.Quantity;
        trade.Commission = (decimal)item.Fee;
        trade.CommissionAsset = item.FeeAsset;
    }


    /// <summary>
    /// Haal de trades van 1 symbol op
    /// </summary>
    public static async Task<int> FetchTradesForSymbolAsync(CryptoDatabase database, CryptoPosition position)
    {
        using BybitRestClient client = new();
        int tradeCount = 0;
        try
        {
            bool isChanged = false;
            long? fromId = position.Symbol.LastTradeIdFetched;
            List<CryptoTrade> tradeCache = [];

            //GlobalData.AddTextToLogTab($"FetchTradesForSymbolAsync {position.Symbol.Name} fetching trades from exchange {fromId}");
            //ScannerLog.Logger.Trace($"FetchTradesForSymbolAsync {position.Symbol.Name} fetching trades from exchange {fromId}");

            while (true)
            {
                // Administration via Symbol.LastTradeIdFetched (number)
                if (fromId != null)
                    fromId += 1;

                LimitRate.WaitForFairWeight(1);
                ScannerLog.Logger.Trace($"FetchTradesForSymbolAsync {position.Symbol.Name} fetching trades from exchange {fromId}");
                var result = await client.SpotApiV3.Trading.GetUserTradesAsync(position.Symbol.Name, fromId: fromId, limit: 1000);
                if (!result.Success)
                {
                    GlobalData.AddTextToLogTab($"{position.Symbol.Name} error retreiving trades {result.Error}");
                    break;
                }

                if (result.Data != null && result.Data.Any())
                {
                    foreach (var item in result.Data)
                    {
                        string tradeId = item.TradeId.ToString();
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
                                PickupTradeV3(position.Account, position.Symbol, trade, item);
                                string text = JsonSerializer.Serialize(item, ExchangeHelper.JsonSerializerNotIndented).Trim();
                                ScannerLog.Logger.Trace($"{item.Symbol} Trade added json={text}");
#if DEBUG
                                ScannerLog.Logger.Debug($"{item} json={text}");
#endif

                                tradeCache.Add(trade);
                                position.TradeList.AddTrade(trade);

                                //if (!position.Symbol.LastTradeIdFetched.HasValue || item.TradeId > position.Symbol.LastTradeIdFetched)
                                //{
                                //    isChanged = true;
                                //    fromId = item.TradeId;
                                //    position.Symbol.LastTradeIdFetched = item.TradeId;
                                //    position.Symbol.LastTradeFetched = trade.TradeTime;
                                //}
                            }
                        }

                        if (!position.Symbol.LastTradeIdFetched.HasValue || item.TradeId > position.Symbol.LastTradeIdFetched)
                        {
                            isChanged = true;
                            //fromId = item.TradeId;
                            position.Symbol.LastTradeIdFetched = item.TradeId;
                            position.Symbol.LastTradeFetched = item.TradeTime;
                        }
                        fromId = item.TradeId;
                    }
                }
                else break;
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



    // V5 does not return the FeeAsset and therefore we cannot use this newer api..
    public static async Task<int> FetchTradesForSymbolAsyncV5(CryptoDatabase database, CryptoPosition position)
    {
        using BybitRestClient client = new();
        int tradeCount = 0;
        try
        {
            bool isChanged = false;
            List<CryptoTrade> tradeCache = [];

            //GlobalData.AddTextToLogTab($"FetchTradesForSymbolAsync {position.Symbol.Name} fetching trades from exchange {fromId}");
            //ScannerLog.Logger.Trace($"FetchTradesForSymbolAsync {position.Symbol.Name} fetching trades from exchange {fromId}");
            //Verzin een begin datum
            if (position.Symbol.LastTradeFetched == null)
            {
                isChanged = true;
                position.Symbol.LastTradeFetched = position.CreateTime.AddMinutes(-1);
            }
            // Bybit doet het alleen in blokken van 7 dagen
            DateTime? startTime = position.Symbol.LastTradeFetched;


            while (true)
            {
                LimitRate.WaitForFairWeight(1);
                ScannerLog.Logger.Trace($"FetchTradesForSymbolAsync {position.Symbol.Name} fetching trades from exchange {startTime}");
                var result = await client.V5Api.Trading.GetUserTradesAsync(Category.Spot, position.Symbol.Name, startTime: startTime, limit: 1000);
                if (!result.Success)
                {
                    GlobalData.AddTextToLogTab($"{position.Symbol.Name} error retreiving trades {result.Error}");
                    break;
                }

                if (result.Data != null && result.Data.List.Any())
                {
                    foreach (var item in result.Data.List)
                    {
                        string tradeId = item.TradeId.ToString();
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

                    if (startTime > position.Symbol.LastTradeFetched)
                    {
                        isChanged = true;
                        position.Symbol.LastTradeFetched = startTime;
                    }
                    startTime = startTime?.AddDays(7);
                }
                else break;
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
