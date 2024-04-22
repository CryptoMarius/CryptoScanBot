using CryptoScanBot.Core.Context;
using CryptoScanBot.Core.Enums;
using CryptoScanBot.Core.Intern;
using CryptoScanBot.Core.Model;
using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanBot.Core.Trader;

#if TRADEBOT
public class PaperTrading
{
    public static async Task CreatePaperTradeObject(CryptoDatabase database, CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step, decimal price, DateTime LastCandle1mCloseTimeDate)
    {
        // Als een surrogaat van de exchange...
        var symbol = position.Symbol;

        CryptoOrder order = new()
        {
            TradeAccount = position.TradeAccount,
            TradeAccountId = position.TradeAccountId,
            Exchange = symbol.Exchange,
            ExchangeId = position.ExchangeId,
            Symbol = position.Symbol,
            SymbolId = position.SymbolId,
            OrderId = step.OrderId, //Database.CreateNewUniqueId(), // Een fake trade ID (als er maar een getal in zit)

            Status = CryptoOrderStatus.PartiallyAndClosed, //Filled,
            Type = step.OrderType,
            Side = step.Side,

            CreateTime = step.CreateTime,
            UpdateTime = LastCandle1mCloseTimeDate.AddSeconds(2), // Datum van sluiten candle en een beetje extra

            Price = price,
            Quantity = step.Quantity,
            QuoteQuantity = step.Quantity * price,

            AveragePrice = price,
            QuantityFilled = step.Quantity,
            QuoteQuantityFilled = step.Quantity * price,

            Commission = 0, //step.Quantity * price * feeRate * GlobalData.Settings.General.Exchange.FeeRate / 100, // commission, zou ook per quote of munt kunnen?
            CommissionAsset = "" //symbol.Quote,
        };
        if (part.Purpose == CryptoPartPurpose.Dca)
            order.Status = CryptoOrderStatus.Filled;

        database.Connection.Insert<CryptoOrder>(order);
        GlobalData.AddOrder(order);



        CryptoTrade trade = new()
        {
            TradeAccount = position.TradeAccount,
            TradeAccountId = position.TradeAccountId,
            Exchange = symbol.Exchange,
            ExchangeId = position.ExchangeId,
            Symbol = position.Symbol,
            SymbolId = position.SymbolId,
            TradeId = database.CreateNewUniqueId(),
            OrderId = step.OrderId, //Database.CreateNewUniqueId(), // Een fake trade ID (als er maar een getal in zit)

            TradeTime = order.CreateTime,

            Price = price,
            Quantity = step.Quantity,
            QuoteQuantity = step.Quantity * price,

            Commission = 0,
            CommissionAsset = "",
        };

        // full commission = 0.1, met BNB korting=0.075 (zonder kickback, anders was het 0.065?)
        decimal feeRate = 0.1m;
        if (position.Exchange.FeeRate.HasValue)
            feeRate = (decimal)position.Exchange.FeeRate;

        // Entry commissie opboeken in base amount (base/quote)
        if (step.Side == position.GetEntryOrderSide())
        {
            trade.CommissionAsset = symbol.Base;
            trade.Commission = (decimal)(step.Quantity * feeRate / 100);
        }

        // TP commissie opboeken in quote amount (base/quote)
        if (step.Side == position.GetTakeProfitOrderSide())
        {
            trade.CommissionAsset = symbol.Quote;
            trade.Commission = (decimal)(step.Quantity * step.Price * feeRate / 100);
        }
        database.Connection.Insert<CryptoTrade>(trade);
        GlobalData.AddTrade(trade);


        await TradeHandler.HandleTradeAsync(position.Symbol, step.OrderType, step.Side, CryptoOrderStatus.Filled, order);
        PaperAssets.Change(position.TradeAccount, position.Symbol, step.Side, CryptoOrderStatus.Filled, order.Quantity, order.QuoteQuantity);
    }



    /// <summary>
    /// Controle van alle posities na het opnieuw opstarten
    /// </summary>
    public static async Task CheckPositionsAfterRestart(CryptoTradeAccount tradeAccount)
    {
        // Positions - Parts - Steps 1 voor 1 bij langs om te zien of de prijs ooit boven of beneden de prijs is geweest

        if (tradeAccount.PositionList.Count != 0)
        {
            CryptoDatabase database = new();
            database.Open();

            foreach (var position in tradeAccount.PositionList.Values.ToList())
            {
                SortedList<DateTime, (CryptoPositionPart part, CryptoPositionStep step)> indexList = [];

                // Verzamel de open steps
                foreach (var part in position.Parts.Values.ToList())
                {
                    if (!part.CloseTime.HasValue)
                    {
                        foreach (var step in part.Steps.Values.ToList())
                        {
                            if (step.Status == CryptoOrderStatus.New)
                                indexList.TryAdd(step.CreateTime, (part, step));
                        }
                    }
                }


                // controleer vanaf de openstaande step, en het kan vast veel optimaler
                // als we de hogere intervallen inzetten (of een combinatie indien nodig)
                // (maar zoveel posities staan niet open. dus voorlopig is dit prima)
                foreach (var (part, step) in indexList.Values)
                {
                    long from = CandleTools.GetUnixTime(step.CreateTime, 60) + 60;
                    long limit = CandleTools.GetUnixTime(DateTime.UtcNow, 60);
                    while (from < limit)
                    {
                        // Eventueel missende candles hebben op deze manier geen impact
                        if (position.Symbol.CandleList.TryGetValue(from, out CryptoCandle candle))
                        {
                            await PaperTradingCheckStep(database, position, part, step, candle);
                        }
                        from += 60;
                    }
                }

                await TradeTools.CalculatePositionResultsViaOrders(database, position);
            }
        }
    }


    public static async Task PaperTradingCheckStep(CryptoDatabase database, CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step, CryptoCandle lastCandle1m)
    {
        if (step.Status == CryptoOrderStatus.New)
        {
            if (step.Side == CryptoOrderSide.Buy)
            {
                if (step.OrderType == CryptoOrderType.Market) // is reeds afgehandeld
                    await CreatePaperTradeObject(database, position, part, step, lastCandle1m.Close, lastCandle1m.Date.AddMinutes(1));
                if (step.StopPrice.HasValue)
                {
                    if (lastCandle1m.High > step.StopPrice)
                        await CreatePaperTradeObject(database, position, part, step, (decimal)step.StopPrice, lastCandle1m.Date.AddMinutes(1));
                }
                else if (lastCandle1m.Low < step.Price)
                    await CreatePaperTradeObject(database, position, part, step, step.Price, lastCandle1m.Date.AddMinutes(1));
            }
            else if (step.Side == CryptoOrderSide.Sell)
            {
                if (step.OrderType == CryptoOrderType.Market)  // is reeds afgehandeld
                    await CreatePaperTradeObject(database, position, part, step, lastCandle1m.Close, lastCandle1m.Date.AddMinutes(1));
                else if (step.StopPrice.HasValue)
                {
                    if (lastCandle1m.Low < step.StopPrice)
                        await CreatePaperTradeObject(database, position, part, step, (decimal)step.StopPrice, lastCandle1m.Date.AddMinutes(1));
                }
                else if (lastCandle1m.High > step.Price)
                    await CreatePaperTradeObject(database, position, part, step, step.Price, lastCandle1m.Date.AddMinutes(1));

            }
        }
    }


    public static async Task PaperTradingCheckOrders(CryptoDatabase database, CryptoTradeAccount tradeAccount, CryptoSymbol symbol, CryptoCandle lastCandle1m)
    {
        // Is er iets gekocht of verkocht?
        // Zoja dan de HandleTrade aanroepen.

        if (tradeAccount.PositionList.TryGetValue(symbol.Name, out var position))
        {
            foreach (CryptoPositionPart part in position.Parts.Values.ToList())
            {
                // reeds afgesloten
                if (part.CloseTime.HasValue)
                    continue;

                foreach (CryptoPositionStep step in part.Steps.Values.ToList())
                {
                    await PaperTradingCheckStep(database, position, part, step, lastCandle1m);
                }

            }
        }
    }
}
#endif
