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
        CryptoOrder? order = CreatePaperTradeOrder(database, position, part, step, price, lastCandle1mOpenTime);
        if (order != null)
            await TradeHandler.HandleTradeAsync(position.Symbol, CryptoOrderStatus.Filled, order);
    }


    /// <summary>
    /// Create the order+trade records without triggering the HandleTradeAsync cascade.
    /// Returns the order so the caller can decide when to call HandleTradeAsync.
    /// </summary>
    internal static CryptoOrder? CreatePaperTradeOrder(
        CryptoDatabase database, CryptoPosition position, CryptoPositionPart part,
        CryptoPositionStep step, decimal price, CandleTime lastCandle1mOpenTime)
    {
        // We have a stupid bug which adds duplicate orders (and trades)
        // This leads to all kind of troubles, balances and wrong fees
        if (step.OrderId == null)
            return null;
        if (position.OrderList.Find(step.OrderId) != null)
            return null;

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

            // Commission on the order stays 0 — the real commission is set on the trade below
            // and is read back via CalculateOrderFeeFromTrades (which reads from TradeList, not OrderList).
            Commission = 0,
            CommissionAsset = "",
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

        // full commission = 0.1%, met BNB korting = 0.075% (zonder kickback, anders 0.065%)
        decimal feeRate = position.Exchange.FeeRate;

        if (position.Exchange.TradingType == CryptoTradingType.Futures)
        {
            // Linear futures (USDT-margined): commission is always in quote, for both entry and TP.
            // Contract quantity is never reduced by commission — only cash (quote) is deducted.
            // This matches Bybit Futures behaviour (CommissionAsset hardcoded to Quote in real trade pickup).
            trade.CommissionAsset = symbol.Quote;
            trade.Commission = (decimal)(step.Quantity * price * feeRate / 100);
        }
        else
        {
            // Spot: which asset the commission is charged in depends on which side of the trade you receive.
            //   Entry Buy  (long)  → receive base  → fee in base
            //   Entry Sell (short) → receive quote → fee in quote
            //   TP    Sell (long)  → receive quote → fee in quote
            //   TP    Buy  (short) → receive base  → fee in base
            // CommissionBase > 0 flows into filledQuantity = QuantityFilled - CommissionBase,
            // which correctly reduces the net quantity received.
            if (step.Side == position.GetEntryOrderSide())
            {
                if (position.Side == CryptoTradeSide.Long)
                {
                    trade.CommissionAsset = symbol.Base;
                    trade.Commission = (decimal)(step.Quantity * feeRate / 100);
                }
                else
                {
                    trade.CommissionAsset = symbol.Quote;
                    trade.Commission = (decimal)(step.Quantity * price * feeRate / 100);
                }
            }

            if (step.Side == position.GetTakeProfitOrderSide())
            {
                if (position.Side == CryptoTradeSide.Long)
                {
                    trade.CommissionAsset = symbol.Quote;
                    trade.Commission = (decimal)(step.Quantity * price * feeRate / 100);
                }
                else
                {
                    trade.CommissionAsset = symbol.Base;
                    trade.Commission = (decimal)(step.Quantity * feeRate / 100);
                }
            }
        }
        database.Connection.Insert<CryptoTrade>(trade);
        position.TradeList.AddTrade(trade);

        // In paper/emulator mode all orders+trades are created in-memory above, so
        // skip the expensive DB reload in CalculatePositionResultsViaOrders.
        position.HasOrdersAndTradesLoaded = true;

        ScannerLog.Logger.Trace($"{position.Symbol.Name} created papertrade order id={order.Id} and trade={trade.Id} for orderid={order.OrderId}");
        //ScannerLog.Logger.Debug($"{position.Symbol.Name} Debug candle {lastCandle1m.OhlcText(position.Symbol, GlobalData.IntervalList[0], position.Symbol.PriceDisplayFormat, true, true, true)}");

        return order;
    }



    /// <summary>
    /// Controle van alle posities na het opnieuw opstarten.
    /// Walks all 1m candles since the earliest open step and checks every step per candle.
    /// Fills are collected first (order+trade only), then HandleTradeAsync is called
    /// in chronological order so the cascade does not interfere with the scan.
    /// </summary>
    public static async Task CheckPositionsAfterRestart(Model.CryptoExchange activeExchange)
    {
        if (activeExchange.Data.PositionList.Count == 0)
            return;

        using CryptoDatabase database = new();
        database.Open();

        foreach (var position in activeExchange.Data.PositionList.Values.ToList())
        {
            // Collect open steps and determine the earliest CreateTime
            List<(CryptoPositionPart part, CryptoPositionStep step)> openSteps = [];
            DateTime earliest = DateTime.MaxValue;
            foreach (var part in position.PartList.Values.ToList())
            {
                if (!part.CloseTime.HasValue)
                {
                    foreach (var step in part.StepList.Values.ToList())
                    {
                        if (step.Status == CryptoOrderStatus.New)
                        {
                            openSteps.Add((part, step));
                            if (step.CreateTime < earliest)
                                earliest = step.CreateTime;
                        }
                    }
                }
            }
            if (earliest == DateTime.MaxValue)
                continue;

            // Walk every 1m candle; per candle check all open steps.
            // Filled steps are recorded without triggering HandleTradeAsync.
            List<CryptoOrder> orderList = [];
            CryptoSymbolInterval symbolInterval = position.Symbol.GetSymbolInterval(Enums.CryptoIntervalPeriod.interval1m);
            CandleTime from = CandleTime.AlignFromDateTime(earliest, 1) + 1;
            CandleTime limit = CandleTime.AlignFromDateTime(GlobalData.Clock.UtcNow, 1);

            while (from < limit)
            {
                if (symbolInterval.CandleList.TryGetValue(from, out CryptoCandle candle))
                {
                    foreach (var (part, step) in openSteps)
                    {
                        CryptoOrder? order = CheckStepAgainstCandle(database, position, part, step, candle);
                        if (order != null)
                        {
                            orderList.Add(order);
                            break;
                        }
                    }
                }
                from += 1;
            }

            // Process all collected fills in chronological order
            if (orderList.Count > 0)
            {
                ScannerLog.Logger.Info($"{position.Symbol.Name} catch-up detected {orderList.Count} fills");
                foreach (var order in orderList)
                    await TradeHandler.HandleTradeAsync(position.Symbol, CryptoOrderStatus.Filled, order);
            }

            await TradeTools.CalculatePositionResultsViaOrders(database, position);
        }
    }


    /// <summary>
    /// Check a single step against a candle and create the paper trade if filled.
    /// Returns the order when filled, null otherwise.
    /// </summary>
    private static CryptoOrder? CheckStepAgainstCandle(CryptoDatabase database, 
        CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step, CryptoCandle candle)
    {
        if (step.Status != CryptoOrderStatus.New)
            return null;
        // Fix: A step/order cannot be closed if it is not yet created.
        if (step.CreateTime > candle.Date)
            return null;

        if (step.Side == CryptoOrderSide.Buy)
        {
            if (step.OrderType == CryptoOrderType.Market)
                return CreatePaperTradeOrder(database, position, part, step, candle.Close, candle.OpenTime);
            if (step.StopPrice.HasValue && candle.High >= step.StopPrice)
                return CreatePaperTradeOrder(database, position, part, step, step.StopPrice.Value, candle.OpenTime);
            if (candle.Low < step.Price)
                return CreatePaperTradeOrder(database, position, part, step, step.Price, candle.OpenTime);
        }
        else if (step.Side == CryptoOrderSide.Sell)
        {
            if (step.OrderType == CryptoOrderType.Market)
                return CreatePaperTradeOrder(database, position, part, step, candle.Close, candle.OpenTime);
            if (step.StopPrice.HasValue && candle.Low <= step.StopPrice)
                return CreatePaperTradeOrder(database, position, part, step, step.StopPrice.Value, candle.OpenTime);
            if (candle.High > step.Price)
                return CreatePaperTradeOrder(database, position, part, step, step.Price, candle.OpenTime);
        }

        return null;
    }


    internal static async Task PaperTradingCheckStep(CryptoDatabase database, CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step, CryptoCandle lastCandle1m)
    {
        CryptoOrder? order = CheckStepAgainstCandle(database, position, part, step, lastCandle1m);
        if (order != null)
            await TradeHandler.HandleTradeAsync(position.Symbol, CryptoOrderStatus.Filled, order);
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
