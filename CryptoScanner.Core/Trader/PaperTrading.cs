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
        CryptoPositionStep step, decimal price, CandleTime lastCandle1mOpenTime, uint candleDuration = 1)
    {
        CryptoOrder? order = CreatePaperTradeOrder(database, position, part, step, price, lastCandle1mOpenTime, candleDuration);
        if (order != null)
            await TradeHandler.HandleTradeAsync(position.Symbol, CryptoOrderStatus.Filled, order);
    }


    /// <summary>
    /// Create the order+trade records without triggering the HandleTradeAsync cascade.
    /// Returns the order so the caller can decide when to call HandleTradeAsync.
    /// </summary>
    internal static CryptoOrder? CreatePaperTradeOrder(
        CryptoDatabase database, CryptoPosition position, CryptoPositionPart part,
        CryptoPositionStep step, decimal price, CandleTime lastCandle1mOpenTime, uint candleDuration = 1)
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
            UpdateTime = lastCandle1mOpenTime.AddMinutes(candleDuration).ToDateTime(),

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

            TradeTime = lastCandle1mOpenTime.AddMinutes(candleDuration).ToDateTime(),

            Price = price,
            Quantity = step.Quantity,
            QuoteQuantity = step.Quantity * price,

            Commission = 0,
            CommissionAsset = "",
        };

        // full commission = 0.1%, met BNB korting = 0.075% (zonder kickback, anders 0.065%)
        decimal feeRate = position.Exchange.FeeRate;

        // Everything that is not spot trades in contracts (perpetuals, X-Perps)
        if (position.Exchange.TradingType != CryptoTradingType.Spot)
        {
            // Linear futures (USDT-margined): commission is always in quote, for both entry and TP.
            // Contract quantity is never reduced by commission — only cash (quote) is deducted.
            // This matches Bybit Perpetual behaviour (CommissionAsset hardcoded to Quote in real trade pickup).
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
                            // No break: every order of the candle fills, like CheckAllSteps
                            //break;
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
        CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step, CryptoCandle candle, uint candleDuration = 1)
    {
        // Live, this runs on the candle thread while the position thread may be cancelling the same
        // order (TradeTools.CancelOrder). The position is the lock both take: an order that is being
        // cancelled does not fill, and an order that filled cannot be cancelled any more.
        lock (position)
            return CheckStepAgainstCandleLocked(database, position, part, step, candle, candleDuration);
    }


    private static CryptoOrder? CheckStepAgainstCandleLocked(CryptoDatabase database,
        CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step, CryptoCandle candle, uint candleDuration)
    {
        if (step.Status != CryptoOrderStatus.New || step.CancelInProgress)
            return null;
        // Fix: A step/order cannot be closed if it is not yet created.
        if (step.CreateTime > candle.Date)
            return null;

        if (step.Side == CryptoOrderSide.Buy)
        {
            if (step.OrderType == CryptoOrderType.Market)
                return CreatePaperTradeOrder(database, position, part, step, candle.Close, candle.OpenTime, candleDuration);
            if (step.StopPrice.HasValue && candle.High >= step.StopPrice)
                return CreatePaperTradeOrder(database, position, part, step, StopFillPrice(step.Side, step.StopPrice.Value, candle), candle.OpenTime, candleDuration);
            // Strict < on purpose: the price has to go THROUGH the limit price, not merely touch
            // it - on a real exchange a touch leaves you at the back of the order book. The stop
            // orders above do fill on a touch, which is the pessimistic side for them too.
            if (candle.Low < step.Price)
                return CreatePaperTradeOrder(database, position, part, step, step.Price, candle.OpenTime, candleDuration);
        }
        else if (step.Side == CryptoOrderSide.Sell)
        {
            if (step.OrderType == CryptoOrderType.Market)
                return CreatePaperTradeOrder(database, position, part, step, candle.Close, candle.OpenTime, candleDuration);
            if (step.StopPrice.HasValue && candle.Low <= step.StopPrice)
                return CreatePaperTradeOrder(database, position, part, step, StopFillPrice(step.Side, step.StopPrice.Value, candle), candle.OpenTime, candleDuration);
            // Strict > on purpose: see the buy side above.
            if (candle.High > step.Price)
                return CreatePaperTradeOrder(database, position, part, step, step.Price, candle.OpenTime, candleDuration);
        }

        return null;
    }


    /// <summary>
    /// The price a triggered stop fills at. A stop becomes a market order once it is hit, so when the
    /// candle already opened beyond the stop (a gap, or a stop that was placed beyond the price) it
    /// fills at the open, not at the stop: entry 100, stop 95, candle opens at 90 -> filled at 90.
    /// </summary>
    internal static decimal StopFillPrice(CryptoOrderSide side, decimal stopPrice, CryptoCandle candle)
        => side == CryptoOrderSide.Sell ? Math.Min(stopPrice, candle.Open) : Math.Max(stopPrice, candle.Open);


    internal static async Task PaperTradingCheckStep(CryptoDatabase database, CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step, CryptoCandle lastCandle1m, uint candleDuration = 1)
    {
        CryptoOrder? order = CheckStepAgainstCandle(database, position, part, step, lastCandle1m, candleDuration);
        if (order != null)
            await TradeHandler.HandleTradeAsync(position.Symbol, CryptoOrderStatus.Filled, order);
    }


    /// <summary>
    /// How many of the step's own price levels this candle touches (0, 1 or 2). A take-profit step
    /// carries both a limit price and a stop price, so a single step can already be ambiguous on
    /// its own. Mirrors the conditions in <see cref="CheckStepAgainstCandle"/> exactly — it answers
    /// "would this fill?" without filling anything.
    /// </summary>
    private static int CountTriggeredLevels(CryptoPositionStep step, CryptoCandle candle)
    {
        if (step.Status != CryptoOrderStatus.New)
            return 0;
        if (step.CreateTime > candle.Date)
            return 0;

        // A market order has no price condition: it fills, and there is nothing to disambiguate
        // about WHICH level was hit — but it still competes with the other steps for being first.
        if (step.OrderType == CryptoOrderType.Market)
            return 1;

        int levels = 0;
        if (step.Side == CryptoOrderSide.Buy)
        {
            if (step.StopPrice.HasValue && candle.High >= step.StopPrice)
                levels++;
            if (candle.Low < step.Price)
                levels++;
        }
        else if (step.Side == CryptoOrderSide.Sell)
        {
            if (step.StopPrice.HasValue && candle.Low <= step.StopPrice)
                levels++;
            if (candle.High > step.Price)
                levels++;
        }
        return levels;
    }


    /// <summary>
    /// Total number of price levels this candle touches across every open step of the position.
    /// Two or more means the candle cannot tell us the sequence, and the outcome depends on it.
    /// </summary>
    private static int CountTriggeredLevels(CryptoPosition position, CryptoCandle candle)
    {
        int levels = 0;
        foreach (CryptoPositionPart part in position.PartList.Values.ToList())
        {
            if (part.CloseTime.HasValue)
                continue;
            foreach (CryptoPositionStep step in part.StepList.Values.ToList())
            {
                levels += CountTriggeredLevels(step, candle);
                if (levels > 1)
                    return levels; // enough to know it is ambiguous
            }
        }
        return levels;
    }


    /// <summary>
    /// Runs every open step of the position against one candle, in the existing order.
    /// <para>
    /// All fills of the candle are collected first and handled afterwards, like a real exchange where
    /// every order is live during the whole minute. Handling a fill at once ran its cascade (cancel
    /// and re-place every take profit order, with this candle as their create time) before the next
    /// step was checked, so only ONE order could fill per candle: a stop closed one take profit part
    /// per minute and left the rest open (run 2545, 09-10-2026: 250 of 366 positions, one part open
    /// for six more days with the original stop), and a spike through TP1 to TP3 filled TP1 only.
    /// </para>
    /// </summary>
    private static async Task CheckAllSteps(CryptoDatabase database, CryptoPosition position,
        CryptoCandle candle, uint candleDuration)
    {
        List<CryptoOrder> orderList = [];
        foreach (CryptoPositionPart part in position.PartList.Values.ToList())
        {
            if (!part.CloseTime.HasValue)
            {
                foreach (CryptoPositionStep step in part.StepList.Values.ToList())
                {
                    //await PaperTradingCheckStep(database, position, part, step, candle, candleDuration);
                    CryptoOrder? order = CheckStepAgainstCandle(database, position, part, step, candle, candleDuration);
                    if (order != null)
                        orderList.Add(order);
                }
            }
        }

        // The orders are in the position's order list already, so the first cascade books every
        // fill of this candle before it decides anything; the next calls only re-check the position.
        foreach (CryptoOrder order in orderList)
            await TradeHandler.HandleTradeAsync(position.Symbol, CryptoOrderStatus.Filled, order);

        // A stop that was hit closes the WHOLE position, like on an exchange. The stop orders only
        // carried the quantity from before this candle, so a DCA that filled in the same candle was
        // left over and closed a minute later at a worse price (run 2661, LINK long of 22-08).
        // Altrady's stop belongs to the position and closes all of it at once (changed 10-10-2026). The
        // cascade above placed new orders for that rest, with this candle as their create time;
        // their stop is checked against this same candle, the targets are not (that would be a
        // second, optimistic look at the candle).
        if (orderList.Any(o => IsStopFill(position, o)))
            await CheckStopsPlacedInCandle(database, position, candle, candleDuration);
    }


    /// <summary>Whether a filled exit order was triggered by its stop and not by its target.</summary>
    private static bool IsStopFill(CryptoPosition position, CryptoOrder order)
    {
        if (order.Side != position.GetTakeProfitOrderSide())
            return false;
        CryptoPositionStep? step = position.PartList.Values.SelectMany(p => p.StepList.Values).FirstOrDefault(s => s.OrderId == order.OrderId);
        return step != null && step.StopPrice.HasValue && order.Price != step.Price;
    }


    /// <summary>
    /// Fills the stops of the orders the cascade of this candle has just placed, when the candle
    /// reached them. Emulator only in practice: live the cascade runs later on the position thread,
    /// and the next candle picks the rest up (filled at its open when that is already beyond the stop).
    /// </summary>
    private static async Task CheckStopsPlacedInCandle(CryptoDatabase database, CryptoPosition position,
        CryptoCandle candle, uint candleDuration)
    {
        if (!PositionTools.IsListed(GlobalData.ActiveExchange!, position))
            return;

        CryptoOrderSide exitSide = position.GetTakeProfitOrderSide();
        List<CryptoOrder> orderList = [];
        lock (position)
        {
            foreach (CryptoPositionPart part in position.PartList.Values.ToList())
            {
                if (part.CloseTime.HasValue)
                    continue;
                foreach (CryptoPositionStep step in part.StepList.Values.ToList())
                {
                    if (step.Status != CryptoOrderStatus.New || step.CancelInProgress || step.Side != exitSide || !step.StopPrice.HasValue)
                        continue;
                    bool hit = exitSide == CryptoOrderSide.Sell ? candle.Low <= step.StopPrice : candle.High >= step.StopPrice;
                    if (!hit)
                        continue;
                    CryptoOrder? order = CreatePaperTradeOrder(database, position, part, step,
                        StopFillPrice(step.Side, step.StopPrice.Value, candle), candle.OpenTime, candleDuration);
                    if (order != null)
                        orderList.Add(order);
                }
            }
        }

        foreach (CryptoOrder order in orderList)
            await TradeHandler.HandleTradeAsync(position.Symbol, CryptoOrderStatus.Filled, order);
    }


    public static async Task PaperTradingCheckOrders(CryptoDatabase database, Model.CryptoExchange activeExchange, CryptoSymbol symbol, CryptoCandle lastCandle1m, uint candleDuration = 1)
    {
        // Is er iets gekocht of verkocht?
        // Zoja dan de HandleTrade aanroepen.

        // Every position on the coin: the ordinary one, or the parallel ones of a strategy that may
        // hold several (see PositionTools.PositionListKey). Each has its own orders, so each is
        // checked on its own, exactly the way the single position always was.
        foreach (CryptoPosition position in PositionTools.PositionsOf(activeExchange, symbol))
        {
            // At most one level touched -> the sequence cannot matter, so the candle we already have
            // is precise enough. This is the overwhelmingly common case (and always the case at a 1m
            // base interval), which keeps the drill-down off the hot path.
            if (candleDuration > 1 && CountTriggeredLevels(position, lastCandle1m) > 1
                && IntrabarCandles.TryLoad(symbol, candleDuration, lastCandle1m.OpenTime,
                    out List<CryptoCandle> finerCandles, out uint finerDuration))
            {
                // Walk the finer candles in time order. Each fill runs its full HandleTradeAsync
                // cascade before the next sub-candle is examined, so a take profit that happened
                // after an entry is seen as such, and orders created by that cascade are only
                // considered from the sub-candle after the one that created them (their CreateTime
                // guard in CheckStepAgainstCandle).
                //
                // If a single sub-candle is STILL ambiguous we do not descend further — this is
                // already the finest data available. The step order then decides, which tests the
                // stop price before the limit price: the pessimistic reading, deliberately.
                foreach (CryptoCandle subCandle in finerCandles)
                    await CheckAllSteps(database, position, subCandle, finerDuration);
                continue;
            }

            await CheckAllSteps(database, position, lastCandle1m, candleDuration);
        }
    }

}
