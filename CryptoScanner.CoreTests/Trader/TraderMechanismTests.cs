using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;

using static CryptoScanner.Core.Trader.StopLossCalculator;

using ExchangeModel = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Integration-style tests for the trader mechanism: SL placement, SL repositioning
/// after DCA fills, break-even calculation, TP price calculation, and DCA price grid.
///
/// These tests build a CryptoPosition with parts and filled steps, then call
/// TradeTools.CalculateProfitAndBreakEvenPrice to verify break-even, profit, and
/// position state (PartCount, ActiveDca). SL prices are verified via
/// StopLossCalculator.Calculate. TP and DCA price grids are verified by
/// replicating the PositionMonitor formulas.
///
/// No exchange calls, no database, no async — pure deterministic math.
/// </summary>
[TestClass]
public class TraderMechanismTests
{
    // ── Constants ───────────────────────────────────────────────────────────

    private const decimal FeeRate = 0.1m; // 0.1% per trade (Binance VIP0)
    private const decimal SignalPrice = 100m;
    private const decimal GlobalSlPct = 5m;
    private const decimal GlobalSlLimitPct = 6m;

    // ── Helpers ─────────────────────────────────────────────────────────────

    private static ExchangeModel MakeExchange() => new()
    {
        Id = 1,
        Name = "TestExchange",
        FeeRate = FeeRate,
    };

    private static CryptoSymbol MakeSymbol(ExchangeModel exchange) => new()
    {
        Id = 1,
        Name = "TESTUSDT",
        Base = "TEST",
        Quote = "USDT",
        Exchange = exchange,
        ExchangeId = exchange.Id,
        ExchangeName = exchange.Name,
        QuoteData = GlobalData.AddQuoteData("USDT"),
        PriceTickSize = 0.01m,
        PriceMinimum = 0.01m,
        PriceMaximum = 1_000_000m,
        QuantityTickSize = 0.001m,
        QuantityMinimum = 0.001m,
        QuantityMaximum = 1_000_000m,
        QuoteValueMinimum = 1m,
        QuoteValueMaximum = 1_000_000m,
    };

    private static CryptoInterval MakeInterval() => GlobalData.IntervalListPeriod.Count > 0
        ? GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m]
        : new CryptoInterval { Id = 6, Name = "15m", Duration = 900 };

    // Why? There is a PositionTools.CreatePosition? Why duplicate this many code?
    private static CryptoPosition MakePosition(ExchangeModel exchange, CryptoSymbol symbol,
        CryptoInterval interval, CryptoTradeSide side, decimal? slPercentage = null)
    {
        var position = new CryptoPosition
        {
            Id = 1,
            CreateTime = DateTime.UtcNow,
            Exchange = exchange,
            ExchangeId = exchange.Id,
            Symbol = symbol,
            SymbolId = symbol.Id,
            Interval = interval,
            IntervalId = interval.Id,
            Side = side,
            Status = CryptoPositionStatus.Trading,
            SignalPrice = SignalPrice,
            SlPercentage = slPercentage,
            HasOrdersAndTradesLoaded = true,
        };
        return position;
    }

    private static int _stepId = 1;

    private static CryptoPositionPart AddPart(CryptoPosition position, CryptoPartPurpose purpose,
        ExchangeModel exchange, CryptoSymbol symbol)
    {
        int partId = position.PartList.Count + 1;
        var part = new CryptoPositionPart
        {
            Id = partId,
            PositionId = position.Id,
            Position = position,
            Exchange = exchange,
            ExchangeId = exchange.Id,
            Symbol = symbol,
            SymbolId = symbol.Id,
            Purpose = purpose,
            PartNumber = partId,
            CreateTime = DateTime.UtcNow,
        };
        position.PartList.Add(partId, part);
        return part;
    }

    private static CryptoPositionStep AddFilledStep(CryptoPositionPart part,
        CryptoOrderSide side, decimal price, decimal quantity, decimal feeRate)
    {
        decimal quoteQty = price * quantity;
        decimal commission = quoteQty * feeRate / 100m;
        decimal commissionBase = 0m;

        var step = new CryptoPositionStep
        {
            Id = _stepId++,
            PositionId = part.PositionId,
            PositionPartId = part.Id,
            CreateTime = DateTime.UtcNow,
            Side = side,
            Status = CryptoOrderStatus.Filled,
            OrderType = CryptoOrderType.Limit,
            Price = price,
            Quantity = quantity,
            AveragePrice = price,
            QuantityFilled = quantity,
            QuoteQuantityFilled = quoteQty,
            Commission = commission,
            CommissionBase = commissionBase,
            CommissionQuote = commission,
        };
        part.StepList.Add(step.Id, step);
        return step;
    }

    private static CryptoPositionStep AddOpenStep(CryptoPositionPart part,
        CryptoOrderSide side, decimal price, decimal quantity)
    {
        var step = new CryptoPositionStep
        {
            Id = _stepId++,
            PositionId = part.PositionId,
            PositionPartId = part.Id,
            CreateTime = DateTime.UtcNow,
            Side = side,
            Status = CryptoOrderStatus.New,
            OrderType = CryptoOrderType.Limit,
            Price = price,
            Quantity = quantity,
        };
        part.StepList.Add(step.Id, step);
        return step;
    }

    /// <summary>
    /// Recalculates position state and returns the SL result for the current position.
    /// </summary>
    private static SlResult RecalcAndGetSl(CryptoPosition position)
    {
        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        decimal? extremeDca = FindExtremeDcaPrice(position);

        return Calculate(new SlInput
        {
            Side = position.Side,
            SlPercentage = position.SlPercentage,
            EntryPrice = position.EntryPrice ?? position.BreakEvenPrice,
            ExtremeDcaPrice = extremeDca,
            GlobalStopLossPercentage = GlobalSlPct,
            GlobalStopLossLimitPercentage = GlobalSlLimitPct,
        });
    }

    /// <summary>
    /// Mirrors PositionMonitor.FindExtremeDcaPrice: lowest buy for long, highest sell for short.
    /// </summary>
    private static decimal? FindExtremeDcaPrice(CryptoPosition position)
    {
        CryptoOrderSide dcaSide = position.GetEntryOrderSide();
        CryptoPositionStep? step;
        if (dcaSide == CryptoOrderSide.Buy)
        {
            step = position.PartList.Values
                .Where(p => p.Purpose == CryptoPartPurpose.Dca)
                .SelectMany(p => p.StepList.Values)
                .Where(s => s.Side == dcaSide)
                .MinBy(s => s.Price);
        }
        else
        {
            step = position.PartList.Values
                .Where(p => p.Purpose == CryptoPartPurpose.Dca)
                .SelectMany(p => p.StepList.Values)
                .Where(s => s.Side == dcaSide)
                .MaxBy(s => s.Price);
        }
        return step?.Price;
    }

    /// <summary>
    /// Mirrors PositionMonitor.CalculateTpPrice.
    /// </summary>
    private static decimal CalculateTpPrice(CryptoPosition position, decimal percentage)
    {
        int multiplier = position.Side == CryptoTradeSide.Long ? +1 : -1;
        return position.TpGridBreakEvenPrice + (multiplier * position.TpGridBreakEvenPrice * (percentage / 100m));
    }

    /// <summary>
    /// Mirrors PositionMonitor.GetMissingFixedPercentageDcaPrices.
    /// </summary>
    private static List<decimal> GetDcaPrices(CryptoPosition position, List<CryptoDcaEntry> dcaList)
    {
        List<decimal> prices = [];
        if (!position.EntryPrice.HasValue || position.TpGridBreakEvenPrice == 0 || position.Invested == 0)
            return prices;

        int existingDcaParts = position.PartList.Values.Count(
            p => p.Purpose == CryptoPartPurpose.Dca && !p.CloseTime.HasValue);

        decimal entryPrice = position.TpGridBreakEvenPrice;
        for (int i = existingDcaParts; i < dcaList.Count; i++)
        {
            decimal diffPrice = entryPrice * Math.Abs(dcaList[i].Percentage) / 100m;
            prices.Add(position.Side == CryptoTradeSide.Long
                ? entryPrice - diffPrice
                : entryPrice + diffPrice);
        }
        return prices;
    }


    // ═════════════════════════════════════════════════════════════════════════
    //  1. SL PLACEMENT AT ENTRY
    // ═════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Entry_Long_SignalSl_StopBelowSignalPrice()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long, slPercentage: 3m);
        position.EntryPrice = 100m;

        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 1m, FeeRate);

        var sl = RecalcAndGetSl(position);

        Assert.AreEqual(SlSource.Signal, sl.Source);
        Assert.AreEqual(97m, sl.Stop, "Long 3% signal SL from 100 = 97");
        Assert.AreEqual(0, position.PartCount, "No DCA filled yet");
        Assert.IsFalse(position.ActiveDca, "No pending DCA");
    }

    [TestMethod]
    public void Entry_Short_SignalSl_StopAboveSignalPrice()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Short, slPercentage: 3m);
        position.EntryPrice = 100m;

        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Sell, 100m, 1m, FeeRate);

        var sl = RecalcAndGetSl(position);

        Assert.AreEqual(SlSource.Signal, sl.Source);
        Assert.AreEqual(103m, sl.Stop, "Short 3% signal SL from 100 = 103");
    }

    [TestMethod]
    public void Entry_NoSignalSl_UsesGlobal()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long, slPercentage: null);
        position.EntryPrice = 100m;

        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 1m, FeeRate);

        var sl = RecalcAndGetSl(position);

        Assert.AreEqual(SlSource.Global, sl.Source);
        Assert.AreEqual(95m, sl.Stop, "Long 5% global SL from entry 100 = 95");
    }


    // ═════════════════════════════════════════════════════════════════════════
    //  2. SL REPOSITIONING AFTER 1st DCA
    // ═════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Dca1_Long_SignalSlStaysAnchoredOnEntry()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long, slPercentage: 3m);
        position.EntryPrice = 100m;

        // Entry at 100
        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 1m, FeeRate);

        // DCA 1 filled at 95 (5% below entry)
        var dcaPart = AddPart(position, CryptoPartPurpose.Dca, exchange, symbol);
        AddFilledStep(dcaPart, CryptoOrderSide.Buy, 95m, 2m, FeeRate);

        var sl = RecalcAndGetSl(position);

        Assert.AreEqual(1, position.PartCount, "1 DCA filled");
        Assert.AreEqual(SlSource.Signal, sl.Source, "Signal SL stays active after DCA fill");
        // Signal SL always anchors on EntryPrice (StopLossCalculator.Calculate), even after a DCA
        // fill: 100 - 3% = 97.
        Assert.AreEqual(100m - 100m * 0.03m, sl.Stop, "Signal SL = 100 - 3% = 97");
    }

    [TestMethod]
    public void Dca1_Short_SignalSlStaysAnchoredOnEntry()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Short, slPercentage: 3m);
        position.EntryPrice = 100m;

        // Entry at 100 (short = sell)
        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Sell, 100m, 1m, FeeRate);

        // DCA 1 filled at 105 (5% above entry for short)
        var dcaPart = AddPart(position, CryptoPartPurpose.Dca, exchange, symbol);
        AddFilledStep(dcaPart, CryptoOrderSide.Sell, 105m, 2m, FeeRate);

        var sl = RecalcAndGetSl(position);

        Assert.AreEqual(1, position.PartCount);
        Assert.AreEqual(SlSource.Signal, sl.Source, "Signal SL stays active after DCA fill");
        // Signal SL always anchors on EntryPrice, even after a DCA fill: 100 + 3% = 103.
        Assert.AreEqual(100m + 100m * 0.03m, sl.Stop, "Signal SL = 100 + 3% = 103");
    }

    [TestMethod]
    public void Dca1_PendingNotFilled_SignalSlStillActive()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long, slPercentage: 3m);
        position.EntryPrice = 100m;

        // Entry filled
        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 1m, FeeRate);

        // DCA placed but NOT filled (open order)
        var dcaPart = AddPart(position, CryptoPartPurpose.Dca, exchange, symbol);
        AddOpenStep(dcaPart, CryptoOrderSide.Buy, 95m, 2m);

        var sl = RecalcAndGetSl(position);

        Assert.AreEqual(0, position.PartCount, "DCA not filled, PartCount stays 0");
        Assert.IsTrue(position.ActiveDca, "Pending DCA sets ActiveDca");
        Assert.AreEqual(SlSource.Signal, sl.Source, "Signal SL remains while DCA is only pending");
        // Signal SL always anchors on EntryPrice (100), whether or not a DCA is pending: 97.
        Assert.AreEqual(97m, sl.Stop);
    }


    // ═════════════════════════════════════════════════════════════════════════
    //  3. SL REPOSITIONING AFTER 2nd DCA
    // ═════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Dca2_Long_SignalSlStaysAnchoredOnEntry()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long, slPercentage: 3m);
        position.EntryPrice = 100m;

        // Entry at 100
        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 1m, FeeRate);

        // DCA 1 at 95
        var dca1 = AddPart(position, CryptoPartPurpose.Dca, exchange, symbol);
        AddFilledStep(dca1, CryptoOrderSide.Buy, 95m, 2m, FeeRate);

        // DCA 2 at 88 (lower)
        var dca2 = AddPart(position, CryptoPartPurpose.Dca, exchange, symbol);
        AddFilledStep(dca2, CryptoOrderSide.Buy, 88m, 4m, FeeRate);

        var sl = RecalcAndGetSl(position);

        Assert.AreEqual(2, position.PartCount, "2 DCAs filled");
        Assert.AreEqual(SlSource.Signal, sl.Source, "Signal SL stays active after multiple DCA fills");
        // Signal SL always anchors on EntryPrice (100), regardless of how many DCAs filled: 97.
        Assert.AreEqual(100m - 100m * 0.03m, sl.Stop, "Signal SL stays anchored on entry price (100)");
    }

    [TestMethod]
    public void Dca2_Short_SignalSlStaysAnchoredOnEntry()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Short, slPercentage: 3m);
        position.EntryPrice = 100m;

        // Entry at 100
        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Sell, 100m, 1m, FeeRate);

        // DCA 1 at 105
        var dca1 = AddPart(position, CryptoPartPurpose.Dca, exchange, symbol);
        AddFilledStep(dca1, CryptoOrderSide.Sell, 105m, 2m, FeeRate);

        // DCA 2 at 112 (higher)
        var dca2 = AddPart(position, CryptoPartPurpose.Dca, exchange, symbol);
        AddFilledStep(dca2, CryptoOrderSide.Sell, 112m, 4m, FeeRate);

        var sl = RecalcAndGetSl(position);

        Assert.AreEqual(2, position.PartCount);
        Assert.AreEqual(SlSource.Signal, sl.Source, "Signal SL stays active after multiple DCA fills");
        // Signal SL always anchors on EntryPrice (100), regardless of how many DCAs filled: 103.
        Assert.AreEqual(100m + 100m * 0.03m, sl.Stop, "Signal SL stays anchored on entry price (100)");
    }


    // ═════════════════════════════════════════════════════════════════════════
    //  4. BREAK-EVEN CALCULATION
    // ═════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void BreakEven_Long_EntryOnly()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long);
        position.EntryPrice = 100m;

        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 1m, FeeRate);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        // BE = (Invested + Commission + PredictedCommission) / Quantity
        // Invested = 100, Commission = 0.1 (0.1% of 100), PredictedCommission = 100 * 0.001 * 1 = 0.1
        // Quantity = 1
        // BE = (100 + 0.1 + 0.1) / 1 = 100.2
        Assert.AreEqual(100.2m, position.BreakEvenPrice,
            "BE includes entry fee + predicted exit fee");
    }

    [TestMethod]
    public void BreakEven_Long_AfterDca_AveragesDown()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long);
        position.EntryPrice = 100m;

        // Entry: buy 1 @ 100
        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 1m, FeeRate);

        // DCA: buy 2 @ 90
        var dcaPart = AddPart(position, CryptoPartPurpose.Dca, exchange, symbol);
        AddFilledStep(dcaPart, CryptoOrderSide.Buy, 90m, 2m, FeeRate);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        // Invested = 100 + 180 = 280
        // Commission = 0.1 + 0.18 = 0.28
        // Quantity = 3
        // avgPrice = 280/3 = 93.333...
        // PredictedCommission = 93.333... * 0.001 * 3 = 0.28
        // BE = (280 + 0.28 + 0.28) / 3 = 280.56 / 3 = 93.52
        decimal invested = 280m;
        decimal commission = 0.28m;
        decimal avgPrice = invested / 3m;
        decimal predicted = avgPrice * FeeRate / 100m * 3m;
        decimal expectedBe = (invested + commission + predicted) / 3m;

        Assert.AreEqual(expectedBe, position.BreakEvenPrice,
            "BE averages down after DCA at lower price");
        Assert.IsTrue(position.BreakEvenPrice < 100m,
            "DCA at 90 should bring BE below original entry of 100");
    }

    [TestMethod]
    public void BreakEven_Short_EntryOnly()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Short);
        position.EntryPrice = 100m;

        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Sell, 100m, 1m, FeeRate);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        // Short BE = (Invested - Commission - PredictedCommission) / Quantity
        // = (100 - 0.1 - 0.1) / 1 = 99.8
        Assert.AreEqual(99.8m, position.BreakEvenPrice,
            "Short BE subtracts fees (need to buy back below this price to profit)");
    }


    // ═════════════════════════════════════════════════════════════════════════
    //  5. TP GRID ANCHOR PRICE
    // ═════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void TpGridAnchor_DoesNotMoveOnTpFill()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long);
        position.EntryPrice = 100m;

        // Entry: buy 2 @ 100
        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 2m, FeeRate);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);
        decimal anchorBeforeTp = position.TpGridBreakEvenPrice;

        // TP: sell 1 @ 102 (partial TP)
        var tpPart = AddPart(position, CryptoPartPurpose.TakeProfit, exchange, symbol);
        AddFilledStep(tpPart, CryptoOrderSide.Sell, 102m, 1m, FeeRate);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        Assert.AreEqual(anchorBeforeTp, position.TpGridBreakEvenPrice,
            "TpGridBreakEvenPrice must NOT move on TP fill — only on DCA fill");
    }

    [TestMethod]
    public void TpGridAnchor_MovesOnDcaFill()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long);
        position.EntryPrice = 100m;

        // Entry: buy 1 @ 100
        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 1m, FeeRate);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);
        decimal anchorBeforeDca = position.TpGridBreakEvenPrice;

        // DCA: buy 1 @ 90
        var dcaPart = AddPart(position, CryptoPartPurpose.Dca, exchange, symbol);
        AddFilledStep(dcaPart, CryptoOrderSide.Buy, 90m, 1m, FeeRate);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        Assert.AreNotEqual(anchorBeforeDca, position.TpGridBreakEvenPrice,
            "TpGridBreakEvenPrice MUST move on DCA fill (cost basis shifts)");
        Assert.IsTrue(position.TpGridBreakEvenPrice < anchorBeforeDca,
            "Long DCA at lower price should lower the TP anchor");
    }


    // ═════════════════════════════════════════════════════════════════════════
    //  6. TP PRICE CALCULATION
    // ═════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void TpPrice_Long_AboveAnchor()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long);
        position.EntryPrice = 100m;

        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 1m, FeeRate);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        decimal tpPrice = CalculateTpPrice(position, 1.5m); // 1.5% TP
        decimal expected = position.TpGridBreakEvenPrice * (1 + 1.5m / 100m);

        Assert.AreEqual(expected, tpPrice, "Long TP at 1.5% above anchor");
        Assert.IsTrue(tpPrice > position.TpGridBreakEvenPrice);
    }

    [TestMethod]
    public void TpPrice_Short_BelowAnchor()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Short);
        position.EntryPrice = 100m;

        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Sell, 100m, 1m, FeeRate);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        decimal tpPrice = CalculateTpPrice(position, 1.5m);
        decimal expected = position.TpGridBreakEvenPrice * (1 - 1.5m / 100m);

        Assert.AreEqual(expected, tpPrice, "Short TP at 1.5% below anchor");
        Assert.IsTrue(tpPrice < position.TpGridBreakEvenPrice);
    }


    // ═════════════════════════════════════════════════════════════════════════
    //  7. DCA PRICE GRID
    // ═════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void DcaPriceGrid_Long_BelowEntry()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long);
        position.EntryPrice = 100m;

        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 1m, FeeRate);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        var dcaList = new List<CryptoDcaEntry>
        {
            new() { Percentage = 1.5m, Factor = 100m },  // DCA1: 1.5% below
            new() { Percentage = 4.5m, Factor = 200m },  // DCA2: 4.5% below
        };

        List<decimal> prices = GetDcaPrices(position, dcaList);

        Assert.AreEqual(2, prices.Count);

        decimal anchor = position.TpGridBreakEvenPrice;
        Assert.AreEqual(anchor - anchor * 1.5m / 100m, prices[0], "DCA1 at 1.5% below anchor");
        Assert.AreEqual(anchor - anchor * 4.5m / 100m, prices[1], "DCA2 at 4.5% below anchor");
        Assert.IsTrue(prices[0] > prices[1], "DCA2 is lower than DCA1");
    }

    [TestMethod]
    public void DcaPriceGrid_Short_AboveEntry()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Short);
        position.EntryPrice = 100m;

        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Sell, 100m, 1m, FeeRate);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        var dcaList = new List<CryptoDcaEntry>
        {
            new() { Percentage = 1.5m, Factor = 100m },
            new() { Percentage = 4.5m, Factor = 200m },
        };

        List<decimal> prices = GetDcaPrices(position, dcaList);

        Assert.AreEqual(2, prices.Count);

        decimal anchor = position.TpGridBreakEvenPrice;
        Assert.AreEqual(anchor + anchor * 1.5m / 100m, prices[0], "Short DCA1 at 1.5% above anchor");
        Assert.AreEqual(anchor + anchor * 4.5m / 100m, prices[1], "Short DCA2 at 4.5% above anchor");
        Assert.IsTrue(prices[0] < prices[1], "Short DCA2 is higher than DCA1");
    }

    [TestMethod]
    public void DcaPriceGrid_SkipsAlreadyPlacedLevels()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long);
        position.EntryPrice = 100m;

        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 1m, FeeRate);

        // DCA1 already exists (open, not closed)
        var dca1 = AddPart(position, CryptoPartPurpose.Dca, exchange, symbol);
        AddOpenStep(dca1, CryptoOrderSide.Buy, 98.5m, 1m);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        var dcaList = new List<CryptoDcaEntry>
        {
            new() { Percentage = 1.5m, Factor = 100m },
            new() { Percentage = 4.5m, Factor = 200m },
            new() { Percentage = 8.0m, Factor = 400m },
        };

        List<decimal> prices = GetDcaPrices(position, dcaList);

        Assert.AreEqual(2, prices.Count, "Only 2 missing levels (DCA1 already placed)");
    }


    // ═════════════════════════════════════════════════════════════════════════
    //  8. FULL LIFECYCLE: ENTRY → DCA1 → DCA2 → TP
    // ═════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Lifecycle_Long_EntryDca1Dca2()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long, slPercentage: 2m);
        position.EntryPrice = 100m;

        // ── Phase 1: Entry ──
        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 1m, FeeRate);

        var sl1 = RecalcAndGetSl(position);
        Assert.AreEqual(SlSource.Signal, sl1.Source, "Phase 1: signal SL");
        Assert.AreEqual(98m, sl1.Stop, "Phase 1: signal SL = 100 - 2% = 98");
        Assert.AreEqual(0, position.PartCount);
        Assert.IsTrue(position.BreakEvenPrice > 100m, "Phase 1: BE above entry due to fees");

        // ── Phase 2: DCA 1 filled at 95 ──
        var dca1 = AddPart(position, CryptoPartPurpose.Dca, exchange, symbol);
        AddFilledStep(dca1, CryptoOrderSide.Buy, 95m, 2m, FeeRate);

        var sl2 = RecalcAndGetSl(position);
        Assert.AreEqual(SlSource.Signal, sl2.Source, "Phase 2: signal SL stays active after DCA fill");
        Assert.AreEqual(1, position.PartCount, "Phase 2: 1 DCA filled");
        // Signal SL always anchors on EntryPrice (StopLossCalculator.Calculate), unaffected by the DCA fill.
        Assert.AreEqual(98m, sl2.Stop, "Phase 2: signal SL stays anchored on entry price = 100 - 2% = 98");
        Assert.IsTrue(position.BreakEvenPrice < 100m, "Phase 2: BE below 100 after DCA at 95");

        decimal beAfterDca1 = position.BreakEvenPrice;

        // ── Phase 3: DCA 2 filled at 88 ──
        var dca2 = AddPart(position, CryptoPartPurpose.Dca, exchange, symbol);
        AddFilledStep(dca2, CryptoOrderSide.Buy, 88m, 4m, FeeRate);

        var sl3 = RecalcAndGetSl(position);
        Assert.AreEqual(SlSource.Signal, sl3.Source, "Phase 3: signal SL still active");
        Assert.AreEqual(2, position.PartCount, "Phase 3: 2 DCAs filled");
        Assert.AreEqual(98m, sl3.Stop, "Phase 3: signal SL still anchored on entry price = 100 - 2% = 98");
        Assert.IsTrue(position.BreakEvenPrice < beAfterDca1, "Phase 3: BE further reduced after DCA2");

        // Signal SL is anchored on EntryPrice, so it does not move as further DCAs fill.
        Assert.AreEqual(sl2.Stop, sl3.Stop, "Signal SL is unchanged by DCA fills (anchored on entry, not on DCA price)");
    }


    // ═════════════════════════════════════════════════════════════════════════
    //  9. PARTCOUNT AND ACTIVEDCA TRACKING
    // ═════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void PartCount_OnlyCountsFilledDcaParts()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long);
        position.EntryPrice = 100m;

        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 1m, FeeRate);

        // DCA1: filled
        var dca1 = AddPart(position, CryptoPartPurpose.Dca, exchange, symbol);
        AddFilledStep(dca1, CryptoOrderSide.Buy, 95m, 2m, FeeRate);

        // DCA2: open (not yet filled)
        var dca2 = AddPart(position, CryptoPartPurpose.Dca, exchange, symbol);
        AddOpenStep(dca2, CryptoOrderSide.Buy, 88m, 4m);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        Assert.AreEqual(1, position.PartCount, "Only the filled DCA counts");
        Assert.IsTrue(position.ActiveDca, "Unfilled DCA sets ActiveDca");
    }

    [TestMethod]
    public void PartCount_EntryPartDoesNotCount()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long);
        position.EntryPrice = 100m;

        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 1m, FeeRate);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        Assert.AreEqual(0, position.PartCount, "Entry part does not increment PartCount");
        Assert.IsFalse(position.ActiveDca, "No DCA present");
    }


    // ═════════════════════════════════════════════════════════════════════════
    //  10. PROFIT CALCULATION
    // ═════════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Profit_Long_PartialTpFill()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Long);
        position.EntryPrice = 100m;

        // Buy 2 @ 100
        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Buy, 100m, 2m, FeeRate);

        // Sell 1 @ 110 (partial TP)
        var tpPart = AddPart(position, CryptoPartPurpose.TakeProfit, exchange, symbol);
        AddFilledStep(tpPart, CryptoOrderSide.Sell, 110m, 1m, FeeRate);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        // Invested = 200, Returned = 110
        // Commission = 0.2 (entry) + 0.11 (tp) = 0.31
        // Profit = Returned - Invested - Commission = 110 - 200 - 0.31 = -90.31
        // (position still open with 1 unit, so this is unrealized)
        Assert.AreEqual(1m, position.Quantity, "1 unit remaining after partial TP");
        Assert.AreEqual(200m, position.Invested);
        Assert.AreEqual(110m, position.Returned);
        Assert.IsTrue(position.Commission > 0, "Commission accumulated from entry + TP");
    }

    [TestMethod]
    public void Profit_Short_FullClose()
    {
        var exchange = MakeExchange();
        var symbol = MakeSymbol(exchange);
        var interval = MakeInterval();
        var position = MakePosition(exchange, symbol, interval, CryptoTradeSide.Short);
        position.EntryPrice = 100m;

        // Sell 1 @ 100
        var entryPart = AddPart(position, CryptoPartPurpose.Entry, exchange, symbol);
        AddFilledStep(entryPart, CryptoOrderSide.Sell, 100m, 1m, FeeRate);

        // Buy back 1 @ 95 (TP)
        var tpPart = AddPart(position, CryptoPartPurpose.TakeProfit, exchange, symbol);
        AddFilledStep(tpPart, CryptoOrderSide.Buy, 95m, 1m, FeeRate);

        TradeTools.CalculateProfitAndBreakEvenPrice(position);

        // Short profit = Invested - Returned - Commission
        // Invested = 100 (sell), Returned = 95 (buy back)
        // Commission = 0.1 + 0.095 = 0.195
        decimal expectedProfit = 100m - 95m - 0.195m;
        Assert.AreEqual(expectedProfit, position.Profit, "Short profit = entry - exit - fees");
        Assert.IsTrue(position.Profit > 0, "Sold at 100, bought at 95 = profit");
        Assert.AreEqual(0m, position.Quantity, "Fully closed");
    }
}
