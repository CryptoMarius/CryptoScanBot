using CryptoScanner.Analyzers.FvgLadder;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Taking an entry away while it may have filled already (PositionMonitor.EntryMayHaveFilled), and
/// booking what filled of an entry we cancelled ourselves (TradeTools). The case behind it: ETHEUR
/// on Bitvavo, 25-10-2026 06:40, "entry withdrawn" a millisecond after its paper fill and ten seconds
/// before that fill was booked. Live, a fill takes a while to become a trade: TradeHandler waits ten
/// seconds, a real exchange reports seconds later and may fill an order in steps. Everything that
/// takes an entry away has to reckon with that window, and a cancel on a real exchange only takes
/// what had not filled yet.
/// </summary>
[TestClass]
[DoNotParallelize]
public class EntryWithdrawalTests : TestBase
{
    private const string Standing = "fvgladder";

    private CryptoTradeVia _savedTradeVia;
    private bool _savedEmulatorMode;
    private IClock _savedClock = null!;

    [TestInitialize]
    public void SaveGlobals()
    {
        InitTestSession();
        _savedTradeVia = GlobalData.Settings.Trading.TradeVia;
        _savedEmulatorMode = GlobalData.IsEmulatorMode;
        _savedClock = GlobalData.Clock;
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;
        GlobalData.IsEmulatorMode = true;
        GlobalData.Clock = new SystemClock();
    }

    [TestCleanup]
    public void RestoreGlobals()
    {
        GlobalData.Settings.Trading.TradeVia = _savedTradeVia;
        GlobalData.IsEmulatorMode = _savedEmulatorMode;
        GlobalData.Clock = _savedClock;
    }


    private static (CryptoDatabase db, CryptoSymbol symbol) Setup()
    {
        RegisterPlugin(new FvgLadderPlugin());
        CryptoDatabase database = new();
        database.Open();
        DeleteAllPositionRelatedStuff(database);
        GlobalData.ActiveExchange!.Data.PositionList.Clear();
        return (database, CreateTestSymbol(database));
    }

    private static int _nextId = 9000;

    /// <summary>A waiting long position with one buy limit at <paramref name="price"/>, as the trader leaves it.</summary>
    private static (CryptoPosition position, CryptoPositionPart part, CryptoPositionStep step) WaitingEntry(
        CryptoDatabase database, CryptoSymbol symbol, string strategy = Standing, decimal price = 100m)
    {
        CryptoPosition position = PositionTools.CreatePosition(symbol, strategy, CryptoTradeSide.Long, "EntryWithdrawalTest",
            symbol.Data.SymbolIntervalList[0], DateTime.UtcNow.AddHours(-1));
        position.Id = Interlocked.Increment(ref _nextId);
        position.HasOrdersAndTradesLoaded = true;
        GlobalData.ActiveExchange!.Data.PositionList[PositionTools.PositionListKey(position)] = position;

        CryptoPositionPart part = new()
        {
            Id = Interlocked.Increment(ref _nextId),
            Position = position,
            PositionId = position.Id,
            Exchange = position.Exchange,
            ExchangeId = position.ExchangeId,
            Symbol = position.Symbol,
            SymbolId = position.SymbolId,
            Purpose = CryptoPartPurpose.Entry,
            Strategy = strategy,
            Interval = position.Interval,
            IntervalId = position.IntervalId,
            CreateTime = position.CreateTime,
            SignalPrice = price,
        };
        position.PartList.TryAdd(part.Id, part);

        CryptoPositionStep step = new()
        {
            Id = Interlocked.Increment(ref _nextId),
            PositionId = position.Id,
            PositionPartId = part.Id,
            CreateTime = position.CreateTime,
            Side = CryptoOrderSide.Buy,
            Status = CryptoOrderStatus.New,
            OrderType = CryptoOrderType.Limit,
            OrderId = "STEP" + database.CreateNewUniqueId(),
            Price = price,
            Quantity = 1m,
        };
        PositionTools.AddPositionPartStep(part, step);
        return (position, part, step);
    }

    /// <summary>What an exchange (or the paper fill) leaves in the order list for the step.</summary>
    private static CryptoOrder AddOrder(CryptoPosition position, CryptoPositionStep step, CryptoOrderStatus status, decimal quantityFilled)
    {
        CryptoOrder order = new()
        {
            Exchange = position.Exchange,
            ExchangeId = position.ExchangeId,
            Symbol = position.Symbol,
            SymbolId = position.SymbolId,
            OrderId = step.OrderId,
            CreateTime = step.CreateTime,
            UpdateTime = DateTime.UtcNow,
            Side = step.Side,
            Type = step.OrderType,
            Status = status,
            Price = step.Price,
            Quantity = step.Quantity,
            QuoteQuantity = step.Price * step.Quantity,
            AveragePrice = step.Price,
            QuantityFilled = quantityFilled,
            QuoteQuantityFilled = step.Price * quantityFilled,
        };
        position.OrderList.AddOrder(order, false);
        return order;
    }

    /// <summary>The state our own cancel leaves on a step (TradeTools.CancelOrder).</summary>
    private static void CancelledByUs(CryptoPositionStep step, DateTime cancelTime)
    {
        step.CancelInProgress = true;
        step.Status = CryptoOrderStatus.Timeout;
        step.CloseTime = cancelTime;
    }

    private static CryptoCandle Candle(decimal low, decimal high = 102m) => new()
    {
        TickDecimals = 4,
        OpenTime = CandleTime.FromDateTime(DateTime.UtcNow.AddMinutes(-1)),
        Open = 101m,
        High = high,
        Low = low,
        Close = 101m,
        Volume = 1000m,
    };


    // ─── EntryMayHaveFilled ──────────────────────────────────────────────────

    /// <summary>
    /// The 06:40 case: the paper fill is in the order list, the position still says Waiting because
    /// the fill is booked ten seconds later. Not to be withdrawn.
    /// </summary>
    [TestMethod]
    public void AFillInTheOrderList_WhileStillWaiting_MayHaveFilled()
    {
        var (db, symbol) = Setup();
        var (position, _, step) = WaitingEntry(db, symbol);
        AddOrder(position, step, CryptoOrderStatus.PartiallyAndClosed, 1m);

        Assert.AreEqual(CryptoPositionStatus.Waiting, position.Status);
        Assert.IsTrue(PositionMonitor.EntryMayHaveFilled(position, Candle(low: 101m), DateTime.UtcNow));
    }


    [TestMethod]
    public void AnEntryStepThatIsNoLongerNew_MayHaveFilled()
    {
        var (db, symbol) = Setup();
        var (position, _, step) = WaitingEntry(db, symbol);

        step.Status = CryptoOrderStatus.PartiallyFilled;
        Assert.IsTrue(PositionMonitor.EntryMayHaveFilled(position, Candle(low: 101m), DateTime.UtcNow), "partly filled");

        step.Status = CryptoOrderStatus.Filled;
        Assert.IsTrue(PositionMonitor.EntryMayHaveFilled(position, Candle(low: 101m), DateTime.UtcNow), "filled, not booked yet");
    }


    /// <summary>
    /// The price stayed away from the order and nothing came in: that one may go. Here to prove
    /// the tests around it do not pass because the check says yes to everything.
    /// </summary>
    [TestMethod]
    public void ThePriceStayedAway_NothingFilled()
    {
        var (db, symbol) = Setup();
        var (position, _, _) = WaitingEntry(db, symbol);

        Assert.IsFalse(PositionMonitor.EntryMayHaveFilled(position, Candle(low: 101m), DateTime.UtcNow), "paper");
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.RealTrading;
        Assert.IsFalse(PositionMonitor.EntryMayHaveFilled(position, Candle(low: 101m), DateTime.UtcNow), "real trading");
    }


    /// <summary>
    /// A touch of the order price: a paper order does not fill on a touch (PaperTrading fills below
    /// the price) and that was decided on this very candle, so it may go. A real exchange decides
    /// for itself and reports later, so there it waits a candle.
    /// </summary>
    [TestMethod]
    public void ATouchOfTheOrderPrice_OnlyCountsWithRealTrading()
    {
        var (db, symbol) = Setup();
        var (position, _, _) = WaitingEntry(db, symbol, price: 100m);

        Assert.IsFalse(PositionMonitor.EntryMayHaveFilled(position, Candle(low: 100m), DateTime.UtcNow), "paper");
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.RealTrading;
        Assert.IsTrue(PositionMonitor.EntryMayHaveFilled(position, Candle(low: 100m), DateTime.UtcNow), "real trading, a touch");
        Assert.IsTrue(PositionMonitor.EntryMayHaveFilled(position, Candle(low: 99m), DateTime.UtcNow), "real trading, through it");
    }


    /// <summary>An order update still being handled (TradeHandler's ten seconds), but not in the emulator.</summary>
    [TestMethod]
    public void AnOrderUpdateOnItsWay_MayHaveFilled_ExceptInTheEmulator()
    {
        var (db, symbol) = Setup();
        var (position, _, _) = WaitingEntry(db, symbol);
        DateTime now = DateTime.UtcNow;
        position.DelayUntil = now.AddSeconds(10);

        GlobalData.IsEmulatorMode = false;
        Assert.IsTrue(PositionMonitor.EntryMayHaveFilled(position, Candle(low: 101m), now));
        Assert.IsFalse(PositionMonitor.EntryMayHaveFilled(position, Candle(low: 101m), now.AddSeconds(11)), "handled by then");

        // The emulator handles a fill on the spot, like ThreadCheckPosition, which ignores DelayUntil there
        GlobalData.IsEmulatorMode = true;
        Assert.IsFalse(PositionMonitor.EntryMayHaveFilled(position, Candle(low: 101m), now));
    }


    /// <summary>
    /// An earlier entry order that was cancelled can be in the order list as well. Only a fill
    /// counts, otherwise that order would keep the entry on the book for good.
    /// </summary>
    [TestMethod]
    public void ACancelledOrderWithoutFill_DoesNotCount()
    {
        var (db, symbol) = Setup();
        var (position, _, step) = WaitingEntry(db, symbol);
        AddOrder(position, step, CryptoOrderStatus.Canceled, 0m);

        Assert.IsFalse(PositionMonitor.EntryMayHaveFilled(position, Candle(low: 101m), DateTime.UtcNow));
    }


    // ─── EntryWithdrawn lapses ───────────────────────────────────────────────

    /// <summary>
    /// A withdrawal is about the order on the book. Once the position trades the flag means
    /// nothing, and the gates must not keep forcing the position through on every quiet candle.
    /// </summary>
    [TestMethod]
    public void TheWithdrawal_LapsesOnceThePositionTrades()
    {
        var (db, symbol) = Setup();
        var (position, _, _) = WaitingEntry(db, symbol);
        position.EntryWithdrawn = true;
        position.TriggerPriceTop = 110m;
        position.TriggerPriceBottom = 90m;
        DateTime now = DateTime.UtcNow;

        Assert.IsTrue(PositionMonitor.IsEntryWithdrawn(position));
        Assert.IsTrue(PositionMonitor.NeedsAttentionRegardlessOfPrice(position, CandleTime.FromDateTime(now)));
        Assert.IsTrue(PositionMonitor.CandleCanMovePosition(position, Candle(low: 101m), CandleTime.FromDateTime(now)));

        position.Status = CryptoPositionStatus.Trading;
        Assert.IsFalse(PositionMonitor.IsEntryWithdrawn(position));
        Assert.IsFalse(PositionMonitor.NeedsAttentionRegardlessOfPrice(position, CandleTime.FromDateTime(now)));
        Assert.IsFalse(PositionMonitor.CandleCanMovePosition(position, Candle(low: 101m), CandleTime.FromDateTime(now)));
    }


    // ─── The cancel itself ───────────────────────────────────────────────────

    /// <summary>
    /// Paper trading, the strategy withdrew the entry and the price stayed away: cancelled and
    /// closed as a timeout on the spot, exactly as before.
    /// </summary>
    [TestMethod]
    public async Task Paper_AWithdrawnEntry_IsCancelledAndClosedAsATimeout()
    {
        var (db, symbol) = Setup();
        var (position, part, step) = WaitingEntry(db, symbol);
        position.EntryWithdrawn = true;

        using PositionMonitor monitor = new(symbol, Candle(low: 101m));
        await monitor.CancelOrdersIfClosedOrTimeoutOrReposition(position);

        Assert.AreEqual(CryptoOrderStatus.Timeout, step.Status);
        Assert.IsNotNull(part.CloseTime);
        Assert.AreEqual(CryptoPositionStatus.Timeout, position.Status);
        Assert.IsNotNull(position.CloseTime);
        Assert.IsFalse(PositionTools.IsListed(GlobalData.ActiveExchange!, position), "moved to the closed positions");
    }


    /// <summary>The 06:40 case once more, now through the cancel: the filled entry is left alone.</summary>
    [TestMethod]
    public async Task AWithdrawnEntryThatFilled_IsLeftAlone()
    {
        var (db, symbol) = Setup();
        var (position, part, step) = WaitingEntry(db, symbol);
        position.EntryWithdrawn = true;
        AddOrder(position, step, CryptoOrderStatus.PartiallyAndClosed, 1m);

        using PositionMonitor monitor = new(symbol, Candle(low: 99m));
        await monitor.CancelOrdersIfClosedOrTimeoutOrReposition(position);

        Assert.AreEqual(CryptoOrderStatus.New, step.Status, "not cancelled");
        Assert.IsNull(part.CloseTime);
        Assert.IsNull(position.CloseTime);
        Assert.IsTrue(PositionTools.IsListed(GlobalData.ActiveExchange!, position));
    }


    /// <summary>
    /// Real trading, the candle reached the order and nothing has been reported yet: the cancel
    /// waits a candle instead of going to the exchange. (Reaching the exchange would fail this
    /// test with a network call: the guard is what keeps it local.)
    /// </summary>
    [TestMethod]
    public async Task Real_AWithdrawnEntryTheCandleReached_WaitsACandle()
    {
        var (db, symbol) = Setup();
        var (position, part, step) = WaitingEntry(db, symbol, price: 100m);
        position.EntryWithdrawn = true;
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.RealTrading;

        using PositionMonitor monitor = new(symbol, Candle(low: 100m));
        await monitor.CancelOrdersIfClosedOrTimeoutOrReposition(position);

        Assert.AreEqual(CryptoOrderStatus.New, step.Status);
        Assert.IsFalse(step.CancelInProgress, "the cancel was not even started");
        Assert.IsNull(part.CloseTime);
        Assert.AreEqual(CryptoPositionStatus.Waiting, position.Status);
    }


    // ─── A partial fill behind our cancel ────────────────────────────────────

    [TestMethod]
    public void OurCancelWithAFill_IsBookedAsAFill_ACancelByTheUserIsNot()
    {
        var (db, symbol) = Setup();
        var (position, _, step) = WaitingEntry(db, symbol);
        CryptoOrder order = AddOrder(position, step, CryptoOrderStatus.Canceled, 0.4m);

        Assert.AreEqual(CryptoOrderStatus.Canceled, TradeTools.OrderStatusToBook(step, order), "cancelled on the exchange by the user: a takeover, as before");

        step.CancelInProgress = true;
        Assert.AreEqual(CryptoOrderStatus.PartiallyAndClosed, TradeTools.OrderStatusToBook(step, order), "our cancel, 0.4 filled before it");

        order.Status = CryptoOrderStatus.Expired;
        Assert.AreEqual(CryptoOrderStatus.PartiallyAndClosed, TradeTools.OrderStatusToBook(step, order));

        order.Status = CryptoOrderStatus.Canceled;
        order.QuantityFilled = 0m;
        Assert.AreEqual(CryptoOrderStatus.Canceled, TradeTools.OrderStatusToBook(step, order), "our cancel, nothing filled");
    }


    /// <summary>
    /// Binance reports a cancelled order that filled in part as Canceled with the filled quantity.
    /// The report comes after the cancel, while the position waits with its entry part closed: what
    /// filled is booked, the position trades with it and the part opens again.
    /// </summary>
    [TestMethod]
    public async Task APartialFillReportedAfterOurCancel_IsBooked_AndThePositionTrades()
    {
        var (db, symbol) = Setup();
        var (position, part, step) = WaitingEntry(db, symbol);
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.RealTrading;
        CancelledByUs(step, DateTime.UtcNow.AddSeconds(-20));
        part.CloseTime = step.CloseTime;
        AddOrder(position, step, CryptoOrderStatus.Canceled, 0.4m);

        await TradeTools.CalculatePositionResultsViaOrders(db, position, forceCalculation: true);

        Assert.AreEqual(CryptoPositionStatus.Trading, position.Status);
        Assert.AreEqual(0.4m, step.QuantityFilled);
        Assert.IsTrue(step.Status.IsFilled(), $"booked as a fill, not {step.Status}");
        Assert.AreEqual(0.4m, position.Quantity);
        Assert.AreEqual(40m, position.Invested);
        Assert.IsNull(part.CloseTime, "the entry part goes on with what it bought");
        Assert.IsNull(position.CloseTime);
    }


    /// <summary>
    /// The first report of an order that fills in steps makes the position trading
    /// (ThreadCheckPosition) before anything is booked: a partial fill is booked once the order is
    /// done. Until then the position holds nothing on paper, and that must not close it as Ready.
    /// </summary>
    [TestMethod]
    public async Task AnEntryStillFillingInSteps_DoesNotCloseThePosition()
    {
        var (db, symbol) = Setup();
        var (position, _, step) = WaitingEntry(db, symbol);
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.RealTrading;
        position.Status = CryptoPositionStatus.Trading;
        AddOrder(position, step, CryptoOrderStatus.PartiallyFilled, 0.3m);

        await TradeTools.CalculatePositionResultsViaOrders(db, position, forceCalculation: true);

        Assert.AreEqual(CryptoOrderStatus.PartiallyFilled, step.Status);
        Assert.AreEqual(CryptoPositionStatus.Trading, position.Status, "not Ready while the entry is still filling");
        Assert.IsNull(position.CloseTime);
    }


    [TestMethod]
    public void TheReportOfOurCancel_GetsAGrace_AndOnlyWithRealTrading()
    {
        var (db, symbol) = Setup();
        var (position, _, step) = WaitingEntry(db, symbol);
        DateTime now = DateTime.UtcNow;
        CancelledByUs(step, now.AddMinutes(-1));

        Assert.IsFalse(TradeTools.AwaitsCancelReport(position, now), "paper: a paper order fills whole or not at all");

        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.RealTrading;
        Assert.IsFalse(TradeTools.AwaitsCancelReport(position, now), "never in the emulator");

        GlobalData.IsEmulatorMode = false;
        Assert.IsTrue(TradeTools.AwaitsCancelReport(position, now));
        Assert.IsTrue(TradeTools.EntryUndecided(position, now), "nothing booked yet");
        Assert.IsFalse(TradeTools.AwaitsCancelReport(position, now.AddMinutes(-1) + TradeTools.CancelReportGrace), "the grace is up");

        step.QuantityFilled = 0.4m;
        Assert.IsFalse(TradeTools.AwaitsCancelReport(position, now), "the fill came in");
    }


    /// <summary>
    /// Real trading: after the cancel the position waits with its entry part closed. Within the
    /// grace it stays; after it, with nothing reported, it closes as the timeout it would have been
    /// at once.
    /// </summary>
    [TestMethod]
    public async Task Real_ACancelledEntryWithoutReport_ClosesAsATimeoutAfterTheGrace()
    {
        var (db, symbol) = Setup();
        var (position, part, step) = WaitingEntry(db, symbol);
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.RealTrading;
        GlobalData.IsEmulatorMode = false;
        CancelledByUs(step, DateTime.UtcNow.AddMinutes(-1));
        part.CloseTime = step.CloseTime;

        using (PositionMonitor monitor = new(symbol, Candle(low: 101m)))
            await monitor.CancelOrdersIfClosedOrTimeoutOrReposition(position);
        Assert.AreEqual(CryptoPositionStatus.Waiting, position.Status, "the report may still come");
        Assert.IsTrue(PositionTools.IsListed(GlobalData.ActiveExchange!, position), "and has to find the position");

        step.CloseTime = DateTime.UtcNow - TradeTools.CancelReportGrace - TimeSpan.FromMinutes(1);
        using (PositionMonitor monitor = new(symbol, Candle(low: 101m)))
            await monitor.CancelOrdersIfClosedOrTimeoutOrReposition(position);
        Assert.AreEqual(CryptoPositionStatus.Timeout, position.Status);
        Assert.IsNotNull(position.CloseTime);
        Assert.IsFalse(PositionTools.IsListed(GlobalData.ActiveExchange!, position));
    }


    /// <summary>
    /// Within the grace any forced recalculation comes along: the report itself (also one that says
    /// nothing filled), or a check of the position. The repair in CalculateProfitAndBreakEvenPrice
    /// for a position whose entry orders are all cancelled set it to Timeout there, without a close
    /// time, and the position left the list before the report could find it.
    /// </summary>
    [TestMethod]
    public async Task Real_ARecalculationWithinTheGrace_LeavesThePositionWaiting()
    {
        var (db, symbol) = Setup();
        var (position, part, step) = WaitingEntry(db, symbol);
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.RealTrading;
        GlobalData.IsEmulatorMode = false;
        CancelledByUs(step, DateTime.UtcNow.AddMinutes(-1));
        part.CloseTime = step.CloseTime;
        AddOrder(position, step, CryptoOrderStatus.Canceled, 0m);

        await TradeTools.CalculatePositionResultsViaOrders(db, position, forceCalculation: true);
        Assert.AreEqual(CryptoPositionStatus.Waiting, position.Status);

        // The same for a position the first report of a fill in steps had made trading
        position.Status = CryptoPositionStatus.Trading;
        await TradeTools.CalculatePositionResultsViaOrders(db, position, forceCalculation: true);
        Assert.AreEqual(CryptoPositionStatus.Trading, position.Status);
    }


    /// <summary>
    /// A partly filled entry (as an exchange report leaves the step) loses only its remainder. The
    /// part closes like any entry that timed out: left open with nothing booked yet, HandleEntryPart
    /// would see no entry order and place a second one. The fill opens it again once it is booked.
    /// Paper trading here only to keep the cancel local; the step status is the exchange's.
    /// </summary>
    [TestMethod]
    public async Task APartlyFilledEntry_LosesOnlyItsRemainder_AndGetsNoSecondEntryOrder()
    {
        var (db, symbol) = Setup();
        var (position, part, step) = WaitingEntry(db, symbol, strategy: "stobb");
        position.Status = CryptoPositionStatus.Trading;
        position.ExitRequested = true;
        step.Status = CryptoOrderStatus.PartiallyFilled;

        using PositionMonitor monitor = new(symbol, Candle(low: 101m));
        await monitor.CancelOrdersIfClosedOrTimeoutOrReposition(position);

        Assert.AreEqual(CryptoOrderStatus.Timeout, step.Status, "the remainder is cancelled");
        Assert.IsTrue(step.CancelInProgress);
        Assert.IsNotNull(part.CloseTime);
        Assert.AreEqual(CryptoPositionStatus.Trading, position.Status, "what filled stays");
        Assert.IsNull(position.CloseTime);

        await monitor.HandlePosition(position);
        Assert.AreEqual(1, part.StepList.Count, "no second entry order");
    }


    /// <summary>The same remainder when the strategy withdrew a standing entry.</summary>
    [TestMethod]
    public async Task APartlyFilledStandingEntry_TheStrategyWithdrew_LosesItsRemainder()
    {
        var (db, symbol) = Setup();
        var (position, part, step) = WaitingEntry(db, symbol);
        position.EntryWithdrawn = true;
        step.Status = CryptoOrderStatus.PartiallyFilled;

        using PositionMonitor monitor = new(symbol, Candle(low: 101m));
        await monitor.CancelOrdersIfClosedOrTimeoutOrReposition(position);

        Assert.AreEqual(CryptoOrderStatus.Timeout, step.Status);
        Assert.IsNotNull(part.CloseTime);
    }
}
