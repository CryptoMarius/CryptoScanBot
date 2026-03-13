using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Trader;

public class PaperTrading
{
    public static async Task CreatePaperTrade(
        CryptoDatabase database, CryptoPosition position, CryptoPositionPart part,
        CryptoPositionStep step, decimal price, CandleTime lastCandle1mOpenTime)
    {
        // We have a stupid bug which adds duplicate orders (and trades)
        // This leads to all kind of troubles, balances and wrong fees
        if (step.OrderId == null)
            return;
        if (position.OrderList.Find(step.OrderId) != null)
            return;

        // Als een surrogaat van de exchange...
        var symbol = position.Symbol;

        CryptoOrder order = new()
        {
            Exchange = symbol.Exchange,
            ExchangeId = position.ExchangeId,
            Symbol = position.Symbol,
            SymbolId = position.SymbolId,
            OrderId = step.OrderId, //Database.CreateNewUniqueId(), // Een fake trade ID (als er maar een getal in zit)

            Status = CryptoOrderStatus.PartiallyAndClosed, //Filled,
            Type = step.OrderType,
            Side = step.Side,

            CreateTime = step.CreateTime,
            UpdateTime = lastCandle1mOpenTime.AddMinutes(1).ToDateTime(), // Datum van sluiten candle en een beetje extra

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
        position.OrderList.AddOrder(order);



        CryptoTrade trade = new()
        {
            Exchange = symbol.Exchange,
            ExchangeId = position.ExchangeId,
            Symbol = position.Symbol,
            SymbolId = position.SymbolId,
            TradeId = database.CreateNewUniqueId(),
            OrderId = step.OrderId, //Database.CreateNewUniqueId(), // Een fake trade ID (als er maar een getal in zit)

            TradeTime = lastCandle1mOpenTime.AddMinutes(1).ToDateTime(), // Datum van sluiten candle en een beetje extra

            Price = price,
            Quantity = step.Quantity,
            QuoteQuantity = step.Quantity * price,

            Commission = 0,
            CommissionAsset = "",
        };

        // full commission = 0.1, met BNB korting=0.075 (zonder kickback, anders was het 0.065?)
        decimal feeRate = position.Exchange.FeeRate;

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
        position.TradeList.AddTrade(trade);

        ScannerLog.Logger.Trace($"{position.Symbol.Name} created papertrade order id={order.Id} and trade={trade.Id} for orderid={order.OrderId}");
        //ScannerLog.Logger.Debug($"{position.Symbol.Name} Debug candle {lastCandle1m.OhlcText(position.Symbol, GlobalData.IntervalList[0], position.Symbol.PriceDisplayFormat, true, true, true)}");

        await TradeHandler.HandleTradeAsync(position.Symbol, CryptoOrderStatus.Filled, order);
    }



    /// <summary>
    /// Controle van alle posities na het opnieuw opstarten
    /// </summary>
    public static async Task CheckPositionsAfterRestart(Model.CryptoExchange activeExchange)
    {
        // Positions - Parts - Steps 1 voor 1 bij langs om te zien of de prijs ooit boven of beneden de prijs is geweest

        if (activeExchange.Data.PositionList.Count != 0)
        {
            CryptoDatabase database = new();
            database.Open();

            foreach (var position in activeExchange.Data.PositionList.Values.ToList())
            {
                SortedList<DateTime, (CryptoPositionPart part, CryptoPositionStep step)> indexList = [];

                // Verzamel de open steps
                foreach (var part in position.PartList.Values.ToList())
                {
                    if (!part.CloseTime.HasValue)
                    {
                        foreach (var step in part.StepList.Values.ToList())
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
                    CandleTime from = CandleTime.AlignFromDateTime(step.CreateTime, 1) + 1;
                    CandleTime limit = CandleTime.AlignFromDateTime(GlobalData.GetCurrentDateTime(), 1);
                    while (from < limit)
                    {
                        // Eventueel missende candles hebben op deze manier geen impact
                        CryptoSymbolInterval symbolInterval = position.Symbol.GetSymbolInterval(Enums.CryptoIntervalPeriod.interval1m);
                        if (symbolInterval.CandleList.TryGetValue(from, out CryptoCandle candle))
                        {
                            await PaperTradingCheckStep(database, position, part, step, candle);
                        }
                        from += 1;
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
                if (step.OrderType == CryptoOrderType.Market)
                    await CreatePaperTrade(database, position, part, step, lastCandle1m.Close, lastCandle1m.OpenTime);
                else if (step.StopPrice.HasValue && lastCandle1m.High >= step.StopPrice)
                    await CreatePaperTrade(database, position, part, step, step.StopPrice.Value, lastCandle1m.OpenTime);
                else if (lastCandle1m.Low < step.Price)
                    await CreatePaperTrade(database, position, part, step, step.Price, lastCandle1m.OpenTime);
            }
            else if (step.Side == CryptoOrderSide.Sell)
            {
                if (step.OrderType == CryptoOrderType.Market)
                    await CreatePaperTrade(database, position, part, step, lastCandle1m.Close, lastCandle1m.OpenTime);
                else if (step.StopPrice.HasValue && lastCandle1m.Low <= step.StopPrice)
                    await CreatePaperTrade(database, position, part, step, step.StopPrice.Value, lastCandle1m.OpenTime);
                else if (lastCandle1m.High > step.Price)
                    await CreatePaperTrade(database, position, part, step, step.Price, lastCandle1m.OpenTime);
            }
        }
    }


    public static async Task PaperTradingCheckOrders(CryptoDatabase database, Model.CryptoExchange activeExchange, CryptoSymbol symbol, CryptoCandle lastCandle1m)
    {
        // Is er iets gekocht of verkocht?
        // Zoja dan de HandleTrade aanroepen.

        if (activeExchange.Data.PositionList.TryGetValue(symbol.Name, out var position))
        {
            foreach (CryptoPositionPart part in position.PartList.Values.ToList())
            {
                if (!part.CloseTime.HasValue)
                {
                    foreach (CryptoPositionStep step in part.StepList.Values.ToList())
                    {
                        await PaperTradingCheckStep(database, position, part, step, lastCandle1m);
                    }
                }
            }
        }
    }

}
