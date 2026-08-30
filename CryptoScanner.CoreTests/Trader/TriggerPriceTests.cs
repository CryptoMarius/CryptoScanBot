using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Tests for the HandlePosition trigger-price optimization: TriggerPriceTop/Bottom
/// form a price fence so HandlePosition only runs when the candle crosses a boundary.
/// Covers UpdateTriggerPrices (fence computation) and ShouldRunHandlePosition (gate logic).
/// </summary>
[TestClass]
public class TriggerPriceTests
{
    // ── Helpers ─────────────────────────────────────────────────────────────

    private static Exchange MakeExchange() => new()
    {
        Id = 1,
        Name = "TestExchange",
        FeeRate = 0.1m,
    };

    private static CryptoSymbol MakeSymbol(Exchange exchange) => new()
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

    private static CryptoPosition MakePosition(CryptoTradeSide side,
        decimal breakEvenPrice = 0, bool slMovedToBreakEven = false)
    {
        var exchange = MakeExchange();
        return new CryptoPosition
        {
            Id = 1,
            CreateTime = DateTime.UtcNow,
            Exchange = exchange,
            ExchangeId = exchange.Id,
            Symbol = MakeSymbol(exchange),
            SymbolId = 1,
            Interval = MakeInterval(),
            IntervalId = 6,
            Side = side,
            Status = CryptoPositionStatus.Trading,
            BreakEvenPrice = breakEvenPrice,
            SlMovedToBreakEven = slMovedToBreakEven,
            HasOrdersAndTradesLoaded = true,
        };
    }

    private bool _savedMoveSlToBreakEven;
    private decimal _savedMoveSlToBreakEvenPercentage;
    private decimal _savedMoveSlToBreakEvenSlPercentage;

    [TestInitialize]
    public void SaveSettings()
    {
        _savedMoveSlToBreakEven = GlobalData.Settings.Trading.MoveSlToBreakEven;
        _savedMoveSlToBreakEvenPercentage = GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage;
        _savedMoveSlToBreakEvenSlPercentage = GlobalData.Settings.Trading.MoveSlToBreakEvenSlPercentage;
    }

    [TestCleanup]
    public void RestoreSettings()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = _savedMoveSlToBreakEven;
        GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage = _savedMoveSlToBreakEvenPercentage;
        GlobalData.Settings.Trading.MoveSlToBreakEvenSlPercentage = _savedMoveSlToBreakEvenSlPercentage;
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  UpdateTriggerPrices — Long
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Long_WithTpAndSl_TopIsTp_BottomIsSl()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Long);

        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: 95m);

        Assert.AreEqual(105m, position.TriggerPriceTop);
        Assert.AreEqual(95m, position.TriggerPriceBottom);
    }

    [TestMethod]
    public void Long_WithTpAndNoSl_TopIsTp_BottomIsNull()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Long);

        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: null);

        Assert.AreEqual(105m, position.TriggerPriceTop);
        Assert.IsNull(position.TriggerPriceBottom);
    }

    [TestMethod]
    public void Long_ProfitLockCloserThanTp_TopIsLockThreshold()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = true;
        GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage = 2m;
        // BE=100, lockPct=2% → threshold = 100 + 100*2/100 = 102, nearer than TP=105
        var position = MakePosition(CryptoTradeSide.Long, breakEvenPrice: 100m);

        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: 95m);

        Assert.AreEqual(102m, position.TriggerPriceTop);
        Assert.AreEqual(95m, position.TriggerPriceBottom);
    }

    [TestMethod]
    public void Long_ProfitLockFartherThanTp_TopIsTp()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = true;
        GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage = 10m;
        // BE=100, lockPct=10% → threshold = 110, farther than TP=105
        var position = MakePosition(CryptoTradeSide.Long, breakEvenPrice: 100m);

        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: 95m);

        Assert.AreEqual(105m, position.TriggerPriceTop);
        Assert.AreEqual(95m, position.TriggerPriceBottom);
    }

    [TestMethod]
    public void Long_ProfitLockAlreadyTriggered_TopIsTp()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = true;
        GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage = 2m;
        // SlMovedToBreakEven = true → lock threshold is ignored
        var position = MakePosition(CryptoTradeSide.Long, breakEvenPrice: 100m, slMovedToBreakEven: true);

        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: 102m);

        Assert.AreEqual(105m, position.TriggerPriceTop);
        Assert.AreEqual(102m, position.TriggerPriceBottom);
    }

    [TestMethod]
    public void Long_MoveSlDisabled_TopIsTp()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Long, breakEvenPrice: 100m);

        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: 95m);

        Assert.AreEqual(105m, position.TriggerPriceTop);
        Assert.AreEqual(95m, position.TriggerPriceBottom);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  UpdateTriggerPrices — Short
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Short_WithTpAndSl_BottomIsTp_TopIsSl()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Short);

        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 95m, slStop: 105m);

        Assert.AreEqual(95m, position.TriggerPriceBottom);
        Assert.AreEqual(105m, position.TriggerPriceTop);
    }

    [TestMethod]
    public void Short_WithTpAndNoSl_BottomIsTp_TopIsNull()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Short);

        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 95m, slStop: null);

        Assert.AreEqual(95m, position.TriggerPriceBottom);
        Assert.IsNull(position.TriggerPriceTop);
    }

    [TestMethod]
    public void Short_ProfitLockCloserThanTp_BottomIsLockThreshold()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = true;
        GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage = 2m;
        // BE=100, lockPct=2% → threshold = 100 - 100*2/100 = 98, nearer than TP=95
        var position = MakePosition(CryptoTradeSide.Short, breakEvenPrice: 100m);

        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 95m, slStop: 105m);

        Assert.AreEqual(98m, position.TriggerPriceBottom);
        Assert.AreEqual(105m, position.TriggerPriceTop);
    }

    [TestMethod]
    public void Short_ProfitLockAlreadyTriggered_BottomIsTp()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = true;
        GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage = 2m;
        var position = MakePosition(CryptoTradeSide.Short, breakEvenPrice: 100m, slMovedToBreakEven: true);

        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 95m, slStop: 98m);

        Assert.AreEqual(95m, position.TriggerPriceBottom);
        Assert.AreEqual(98m, position.TriggerPriceTop);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  ShouldRunHandlePosition — gate logic
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Gate_BothTriggersNull_MustRun()
    {
        var position = MakePosition(CryptoTradeSide.Long);
        position.TriggerPriceTop = null;
        position.TriggerPriceBottom = null;

        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 100, candleLow: 99));
    }

    [TestMethod]
    public void Gate_PriceWithinBounds_Skip()
    {
        var position = MakePosition(CryptoTradeSide.Long);
        position.TriggerPriceTop = 105m;
        position.TriggerPriceBottom = 95m;

        Assert.IsFalse(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 103, candleLow: 97));
    }

    [TestMethod]
    public void Gate_HighCrossesTop_MustRun()
    {
        var position = MakePosition(CryptoTradeSide.Long);
        position.TriggerPriceTop = 105m;
        position.TriggerPriceBottom = 95m;

        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 106, candleLow: 103));
    }

    [TestMethod]
    public void Gate_HighExactlyAtTop_MustRun()
    {
        var position = MakePosition(CryptoTradeSide.Long);
        position.TriggerPriceTop = 105m;
        position.TriggerPriceBottom = 95m;

        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 105, candleLow: 103));
    }

    [TestMethod]
    public void Gate_LowCrossesBottom_MustRun()
    {
        var position = MakePosition(CryptoTradeSide.Long);
        position.TriggerPriceTop = 105m;
        position.TriggerPriceBottom = 95m;

        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 97, candleLow: 94));
    }

    [TestMethod]
    public void Gate_LowExactlyAtBottom_MustRun()
    {
        var position = MakePosition(CryptoTradeSide.Long);
        position.TriggerPriceTop = 105m;
        position.TriggerPriceBottom = 95m;

        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 97, candleLow: 95));
    }

    [TestMethod]
    public void Gate_OnlyTopSet_PriceBelow_Skip()
    {
        var position = MakePosition(CryptoTradeSide.Long);
        position.TriggerPriceTop = 105m;
        position.TriggerPriceBottom = null;

        Assert.IsFalse(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 103, candleLow: 90));
    }

    [TestMethod]
    public void Gate_OnlyTopSet_PriceCrossesTop_MustRun()
    {
        var position = MakePosition(CryptoTradeSide.Long);
        position.TriggerPriceTop = 105m;
        position.TriggerPriceBottom = null;

        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 106, candleLow: 103));
    }

    [TestMethod]
    public void Gate_OnlyBottomSet_PriceAbove_Skip()
    {
        var position = MakePosition(CryptoTradeSide.Short);
        position.TriggerPriceTop = null;
        position.TriggerPriceBottom = 95m;

        Assert.IsFalse(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 110, candleLow: 97));
    }

    [TestMethod]
    public void Gate_OnlyBottomSet_PriceCrossesBottom_MustRun()
    {
        var position = MakePosition(CryptoTradeSide.Short);
        position.TriggerPriceTop = null;
        position.TriggerPriceBottom = 95m;

        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 97, candleLow: 94));
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Invalidation
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Invalidation_ForceCheckPosition_ClearsTriggers()
    {
        var position = MakePosition(CryptoTradeSide.Long);
        position.TriggerPriceTop = 105m;
        position.TriggerPriceBottom = 95m;

        // Simulate what ThreadCheckFinishedPosition.ProcessPosition does
        position.ForceCheckPosition = true;
        if (position.ForceCheckPosition)
        {
            position.ForceCheckPosition = false;
            position.TriggerPriceTop = null;
            position.TriggerPriceBottom = null;
        }

        Assert.IsNull(position.TriggerPriceTop);
        Assert.IsNull(position.TriggerPriceBottom);
        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 100, candleLow: 99));
    }

    [TestMethod]
    public void Invalidation_AfterUpdate_GateBlocksUntilCrossed()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Long);

        // First call: triggers are null → must run → HandlePosition sets triggers
        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 100, candleLow: 99));
        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: 95m);

        // Second call: price within bounds → skip
        Assert.IsFalse(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 102, candleLow: 98));

        // Third call: price crosses top → must run
        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 106, candleLow: 103));
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  DCA scenario: triggers update after DCA fill
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void DcaScenario_TriggersAdjustAfterDcaFill()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Long);

        // Initial triggers: TP at 105, SL at 95
        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: 95m);
        Assert.AreEqual(105m, position.TriggerPriceTop);
        Assert.AreEqual(95m, position.TriggerPriceBottom);

        // DCA fills → ForceCheckPosition → triggers cleared
        position.TriggerPriceTop = null;
        position.TriggerPriceBottom = null;

        // HandlePosition runs, new BE shifts TP and SL
        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 103m, slStop: 93m);
        Assert.AreEqual(103m, position.TriggerPriceTop);
        Assert.AreEqual(93m, position.TriggerPriceBottom);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  DCA-aware fence: nearest unfilled DCA tightens the fence
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Long_DcaCloserThanSl_BottomIsDca()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Long);

        // SL=95, DCA=97 → DCA is closer to current price, so Bottom should be 97
        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: 95m, nearestDcaPrice: 97m);

        Assert.AreEqual(105m, position.TriggerPriceTop);
        Assert.AreEqual(97m, position.TriggerPriceBottom);
    }

    [TestMethod]
    public void Long_DcaFartherThanSl_BottomIsSl()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Long);

        // SL=97, DCA=95 → SL is closer to current price, so Bottom should be 97
        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: 97m, nearestDcaPrice: 95m);

        Assert.AreEqual(105m, position.TriggerPriceTop);
        Assert.AreEqual(97m, position.TriggerPriceBottom);
    }

    [TestMethod]
    public void Long_DcaWithNoSl_BottomIsDca()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Long);

        // No SL, DCA=97 → Bottom should be the DCA price
        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: null, nearestDcaPrice: 97m);

        Assert.AreEqual(105m, position.TriggerPriceTop);
        Assert.AreEqual(97m, position.TriggerPriceBottom);
    }

    [TestMethod]
    public void Long_NoDca_BottomIsSl()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Long);

        // No DCA → Bottom stays at SL
        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: 95m, nearestDcaPrice: null);

        Assert.AreEqual(105m, position.TriggerPriceTop);
        Assert.AreEqual(95m, position.TriggerPriceBottom);
    }

    [TestMethod]
    public void Short_DcaCloserThanSl_TopIsDca()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Short);

        // SL=105, DCA=103 → DCA is closer to current price, so Top should be 103
        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 95m, slStop: 105m, nearestDcaPrice: 103m);

        Assert.AreEqual(95m, position.TriggerPriceBottom);
        Assert.AreEqual(103m, position.TriggerPriceTop);
    }

    [TestMethod]
    public void Short_DcaFartherThanSl_TopIsSl()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Short);

        // SL=103, DCA=105 → SL is closer to current price, so Top should be 103
        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 95m, slStop: 103m, nearestDcaPrice: 105m);

        Assert.AreEqual(95m, position.TriggerPriceBottom);
        Assert.AreEqual(103m, position.TriggerPriceTop);
    }

    [TestMethod]
    public void Short_DcaWithNoSl_TopIsDca()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Short);

        // No SL, DCA=103 → Top should be the DCA price
        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 95m, slStop: null, nearestDcaPrice: 103m);

        Assert.AreEqual(95m, position.TriggerPriceBottom);
        Assert.AreEqual(103m, position.TriggerPriceTop);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Profit-lock scenario: triggers tighten after lock
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void ProfitLockScenario_BeforeLock_FenceIncludesThreshold()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = true;
        GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage = 2m;
        var position = MakePosition(CryptoTradeSide.Long, breakEvenPrice: 100m);

        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: 95m);

        // Lock threshold = 102, closer than TP = 105
        Assert.AreEqual(102m, position.TriggerPriceTop);
        Assert.AreEqual(95m, position.TriggerPriceBottom);

        // Candle reaches 102 → gate fires
        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 102.5m, candleLow: 101m));
    }

    [TestMethod]
    public void ProfitLockScenario_AfterLock_FenceUsesNewSlAndTp()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = true;
        GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage = 2m;
        var position = MakePosition(CryptoTradeSide.Long, breakEvenPrice: 100m);

        // Before lock: threshold is the near boundary
        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: 95m);
        Assert.AreEqual(102m, position.TriggerPriceTop);

        // Profit lock triggers
        position.SlMovedToBreakEven = true;
        // SL moved to BE+2% = 102, TP stays at 105
        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: 102m);

        // Now Top = TP (lock threshold no longer applies), Bottom = new SL
        Assert.AreEqual(105m, position.TriggerPriceTop);
        Assert.AreEqual(102m, position.TriggerPriceBottom);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  FindNearestUnfilledDcaPrice
    // ═══════════════════════════════════════════════════════════════════════

    private static CryptoPositionPart AddDcaPart(CryptoPosition position, decimal stepPrice, bool filled)
    {
        var exchange = position.Exchange;
        var symbol = position.Symbol;
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
            Purpose = CryptoPartPurpose.Dca,
            CreateTime = DateTime.UtcNow,
            CloseTime = filled ? DateTime.UtcNow : null,
        };

        var step = new CryptoPositionStep
        {
            Id = partId * 10,
            PositionId = position.Id,
            PositionPartId = partId,
            CreateTime = DateTime.UtcNow,
            CloseTime = filled ? DateTime.UtcNow : null,
            Side = position.Side == CryptoTradeSide.Long ? CryptoOrderSide.Buy : CryptoOrderSide.Sell,
            Status = filled ? CryptoOrderStatus.Filled : CryptoOrderStatus.New,
            Price = stepPrice,
            Quantity = 1m,
        };

        part.StepList.Add(step.Id, step);
        position.PartList.Add(partId, part);
        return part;
    }

    [TestMethod]
    public void FindNearest_NoDcaParts_ReturnsNull()
    {
        var position = MakePosition(CryptoTradeSide.Long);

        Assert.IsNull(PositionMonitor.FindNearestUnfilledDcaPrice(position));
    }

    [TestMethod]
    public void FindNearest_AllDcasFilled_ReturnsNull()
    {
        var position = MakePosition(CryptoTradeSide.Long);
        AddDcaPart(position, 97m, filled: true);
        AddDcaPart(position, 94m, filled: true);

        Assert.IsNull(PositionMonitor.FindNearestUnfilledDcaPrice(position));
    }

    [TestMethod]
    public void FindNearest_Long_ReturnsHighestUnfilled()
    {
        var position = MakePosition(CryptoTradeSide.Long);
        AddDcaPart(position, 97m, filled: false);
        AddDcaPart(position, 94m, filled: false);
        AddDcaPart(position, 91m, filled: false);

        // Long: highest unfilled DCA is closest to current price
        Assert.AreEqual(97m, PositionMonitor.FindNearestUnfilledDcaPrice(position));
    }

    [TestMethod]
    public void FindNearest_Short_ReturnsLowestUnfilled()
    {
        var position = MakePosition(CryptoTradeSide.Short);
        AddDcaPart(position, 103m, filled: false);
        AddDcaPart(position, 106m, filled: false);
        AddDcaPart(position, 109m, filled: false);

        // Short: lowest unfilled DCA is closest to current price
        Assert.AreEqual(103m, PositionMonitor.FindNearestUnfilledDcaPrice(position));
    }

    [TestMethod]
    public void FindNearest_Long_MixedFilledAndUnfilled()
    {
        var position = MakePosition(CryptoTradeSide.Long);
        AddDcaPart(position, 97m, filled: true);   // filled → skip
        AddDcaPart(position, 94m, filled: false);   // open
        AddDcaPart(position, 91m, filled: false);   // open, farther

        // Nearest unfilled is 94
        Assert.AreEqual(94m, PositionMonitor.FindNearestUnfilledDcaPrice(position));
    }

    [TestMethod]
    public void FindNearest_SingleUnfilled()
    {
        var position = MakePosition(CryptoTradeSide.Long);
        AddDcaPart(position, 97m, filled: false);

        Assert.AreEqual(97m, PositionMonitor.FindNearestUnfilledDcaPrice(position));
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  DCA + gate integration: fence fires at DCA level
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Gate_Long_DcaInFence_CandleReachesDca_MustRun()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Long);

        // Fence: Top=105 (TP), Bottom=97 (DCA, closer than SL=95)
        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: 95m, nearestDcaPrice: 97m);

        // Price drops to DCA level → gate fires
        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 99, candleLow: 97));
        // Price stays above DCA → skip
        Assert.IsFalse(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 102, candleLow: 98));
    }

    [TestMethod]
    public void Gate_Short_DcaInFence_CandleReachesDca_MustRun()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = false;
        var position = MakePosition(CryptoTradeSide.Short);

        // Fence: Bottom=95 (TP), Top=103 (DCA, closer than SL=105)
        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 95m, slStop: 105m, nearestDcaPrice: 103m);

        // Price rises to DCA level → gate fires
        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 103, candleLow: 101));
        // Price stays below DCA → skip
        Assert.IsFalse(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 102, candleLow: 98));
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  UpdateTriggerPricesForWaiting — sets trigger fence for unfilled
    //  entry orders so Waiting positions can be skipped when the candle
    //  stays outside the fill zone.
    // ═══════════════════════════════════════════════════════════════════════

    private static CryptoPosition MakeWaitingPosition(CryptoTradeSide side)
    {
        var exchange = MakeExchange();
        return new CryptoPosition
        {
            Id = 1,
            CreateTime = DateTime.UtcNow,
            Exchange = exchange,
            ExchangeId = exchange.Id,
            Symbol = MakeSymbol(exchange),
            SymbolId = 1,
            Interval = MakeInterval(),
            IntervalId = 6,
            Side = side,
            Status = CryptoPositionStatus.Waiting,
            HasOrdersAndTradesLoaded = true,
        };
    }

    private static void AddEntryPart(CryptoPosition position,
        CryptoOrderType orderType, decimal price, decimal? stopPrice = null,
        CryptoOrderStatus status = CryptoOrderStatus.New, bool closed = false)
    {
        int partId = position.PartList.Count + 1;

        var part = new CryptoPositionPart
        {
            Id = partId,
            PositionId = position.Id,
            Position = position,
            Exchange = position.Exchange,
            ExchangeId = position.Exchange.Id,
            Symbol = position.Symbol,
            SymbolId = position.Symbol.Id,
            Purpose = CryptoPartPurpose.Entry,
            CreateTime = DateTime.UtcNow,
            CloseTime = closed ? DateTime.UtcNow : null,
        };

        var step = new CryptoPositionStep
        {
            Id = partId * 10,
            PositionId = position.Id,
            PositionPartId = partId,
            CreateTime = DateTime.UtcNow,
            CloseTime = closed ? DateTime.UtcNow : null,
            Side = position.Side == CryptoTradeSide.Long ? CryptoOrderSide.Buy : CryptoOrderSide.Sell,
            Status = status,
            OrderType = orderType,
            Price = price,
            StopPrice = stopPrice,
            Quantity = 1m,
        };

        part.StepList.Add(step.Id, step);
        position.PartList.Add(partId, part);
    }


    [TestMethod]
    public void WaitingLong_LimitBuy_BottomIsEntryPrice_TopIsMax()
    {
        var position = MakeWaitingPosition(CryptoTradeSide.Long);
        AddEntryPart(position, CryptoOrderType.Limit, price: 100m);

        PositionMonitor.UpdateTriggerPricesForWaiting(position);

        Assert.AreEqual(100m, position.TriggerPriceBottom);
        Assert.AreEqual(decimal.MaxValue, position.TriggerPriceTop);
    }

    [TestMethod]
    public void WaitingShort_LimitSell_TopIsEntryPrice_BottomIsZero()
    {
        var position = MakeWaitingPosition(CryptoTradeSide.Short);
        AddEntryPart(position, CryptoOrderType.Limit, price: 100m);

        PositionMonitor.UpdateTriggerPricesForWaiting(position);

        Assert.AreEqual(100m, position.TriggerPriceTop);
        Assert.AreEqual(0m, position.TriggerPriceBottom);
    }

    [TestMethod]
    public void WaitingLong_StopLimitBuy_TopIsStopPrice_BottomIsLimitPrice()
    {
        var position = MakeWaitingPosition(CryptoTradeSide.Long);
        AddEntryPart(position, CryptoOrderType.StopLimit, price: 98m, stopPrice: 102m);

        PositionMonitor.UpdateTriggerPricesForWaiting(position);

        Assert.AreEqual(102m, position.TriggerPriceTop);
        Assert.AreEqual(98m, position.TriggerPriceBottom);
    }

    [TestMethod]
    public void WaitingShort_StopLimitSell_BottomIsStopPrice_TopIsLimitPrice()
    {
        var position = MakeWaitingPosition(CryptoTradeSide.Short);
        AddEntryPart(position, CryptoOrderType.StopLimit, price: 102m, stopPrice: 98m);

        PositionMonitor.UpdateTriggerPricesForWaiting(position);

        Assert.AreEqual(98m, position.TriggerPriceBottom);
        Assert.AreEqual(102m, position.TriggerPriceTop);
    }

    [TestMethod]
    public void Waiting_MarketOrder_TriggersUnchanged()
    {
        var position = MakeWaitingPosition(CryptoTradeSide.Long);
        AddEntryPart(position, CryptoOrderType.Market, price: 100m);
        position.TriggerPriceTop = null;
        position.TriggerPriceBottom = null;

        PositionMonitor.UpdateTriggerPricesForWaiting(position);

        Assert.IsNull(position.TriggerPriceTop);
        Assert.IsNull(position.TriggerPriceBottom);
    }

    [TestMethod]
    public void Waiting_NoEntryPart_TriggersUnchanged()
    {
        var position = MakeWaitingPosition(CryptoTradeSide.Long);
        position.TriggerPriceTop = null;
        position.TriggerPriceBottom = null;

        PositionMonitor.UpdateTriggerPricesForWaiting(position);

        Assert.IsNull(position.TriggerPriceTop);
        Assert.IsNull(position.TriggerPriceBottom);
    }

    [TestMethod]
    public void Waiting_StepAlreadyFilled_TriggersUnchanged()
    {
        var position = MakeWaitingPosition(CryptoTradeSide.Long);
        AddEntryPart(position, CryptoOrderType.Limit, price: 100m, status: CryptoOrderStatus.Filled);
        position.TriggerPriceTop = null;
        position.TriggerPriceBottom = null;

        PositionMonitor.UpdateTriggerPricesForWaiting(position);

        Assert.IsNull(position.TriggerPriceTop);
        Assert.IsNull(position.TriggerPriceBottom);
    }

    [TestMethod]
    public void Waiting_ClosedEntryPart_TriggersUnchanged()
    {
        var position = MakeWaitingPosition(CryptoTradeSide.Long);
        AddEntryPart(position, CryptoOrderType.Limit, price: 100m, closed: true);
        position.TriggerPriceTop = null;
        position.TriggerPriceBottom = null;

        PositionMonitor.UpdateTriggerPricesForWaiting(position);

        Assert.IsNull(position.TriggerPriceTop);
        Assert.IsNull(position.TriggerPriceBottom);
    }

    [TestMethod]
    public void WaitingLong_LimitBuy_GateIntegration()
    {
        var position = MakeWaitingPosition(CryptoTradeSide.Long);
        AddEntryPart(position, CryptoOrderType.Limit, price: 100m);

        PositionMonitor.UpdateTriggerPricesForWaiting(position);

        // Price above entry → skip (candle doesn't reach limit buy)
        Assert.IsFalse(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 105, candleLow: 101));
        // Price reaches entry → must run (limit buy can fill)
        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 103, candleLow: 100));
    }

    [TestMethod]
    public void WaitingShort_LimitSell_GateIntegration()
    {
        var position = MakeWaitingPosition(CryptoTradeSide.Short);
        AddEntryPart(position, CryptoOrderType.Limit, price: 100m);

        PositionMonitor.UpdateTriggerPricesForWaiting(position);

        // Price below entry → skip (candle doesn't reach limit sell)
        Assert.IsFalse(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 99, candleLow: 95));
        // Price reaches entry → must run (limit sell can fill)
        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 100, candleLow: 97));
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Profit lock: the fence follows the TRIGGER, not where the SL lands
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void Long_ProfitLockFenceUsesTriggerNotSlPercentage()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = true;
        GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage = 2m;    // trigger
        GlobalData.Settings.Trading.MoveSlToBreakEvenSlPercentage = 1m;  // where the SL lands
        var position = MakePosition(CryptoTradeSide.Long, breakEvenPrice: 100m);

        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 105m, slStop: 95m);

        // The fence has to open at the trigger (102), not at the SL level (101) - the lock only
        // arms when the price reaches the trigger.
        Assert.AreEqual(102m, position.TriggerPriceTop);
        Assert.AreEqual(95m, position.TriggerPriceBottom);

        // A candle that only reaches the SL level must not wake HandlePosition
        Assert.IsFalse(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 101.5m, candleLow: 100.5m));
        // A candle that reaches the trigger must
        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 102.5m, candleLow: 101m));
    }

    [TestMethod]
    public void Short_ProfitLockFenceUsesTriggerNotSlPercentage()
    {
        GlobalData.Settings.Trading.MoveSlToBreakEven = true;
        GlobalData.Settings.Trading.MoveSlToBreakEvenPercentage = 2m;
        GlobalData.Settings.Trading.MoveSlToBreakEvenSlPercentage = 1m;
        var position = MakePosition(CryptoTradeSide.Short, breakEvenPrice: 100m);

        PositionMonitor.UpdateTriggerPrices(position, nearestTpPrice: 95m, slStop: 105m);

        // Short: trigger sits below break-even (98), the SL would land at 99
        Assert.AreEqual(98m, position.TriggerPriceBottom);
        Assert.AreEqual(105m, position.TriggerPriceTop);

        Assert.IsFalse(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 99.5m, candleLow: 98.5m));
        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 99m, candleLow: 97.5m));
    }
}
