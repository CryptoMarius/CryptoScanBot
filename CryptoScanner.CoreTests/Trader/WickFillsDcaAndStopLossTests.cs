using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;

using Dapper.Contrib.Extensions;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// One candle that fills every open DCA order AND the stop loss of the exit order.
/// <para>
/// CAKEUSDC.PERP on HyperLiquid, 01-09-2026 19:51 UTC: a wick of -7.9% filled DCA 2 at 1.7906 and
/// the stop at 1.719 in the same minute. The stop order only covered the quantity from before that
/// DCA, so the exit part was closed while the 33.6 CAKE of the DCA had no exit order at all. The
/// position then sat Trading for four days without take profit or stop loss (position 73).
/// </para>
/// <para>
/// These tests run the real trader end to end - paper fills, the position check that follows a
/// fill, the DCA placement and HandlePosition - and only check the invariant that matters: after
/// the wick every coin the position still holds is covered by an exit order with a stop, and the
/// next candle through that stop closes the position completely. Two paths are covered because the
/// live scanner and the emulator differ in WHEN the position check runs (see the test names).
/// </para>
/// </summary>
[TestClass]
[DoNotParallelize]
public class WickFillsDcaAndStopLossTests : TestBase
{
    private const decimal EntryPrice = 100m;
    private const decimal EntryQuantity = 1m;
    private const decimal StartCapital = 10_000m;

    // Saved settings, restored after every test
    private CryptoTradeVia _savedTradeVia;
    private List<CryptoTpEntry> _savedTpList = [];
    private List<CryptoDcaEntry> _savedDcaList = [];
    private decimal _savedStopLoss;
    private decimal _savedStopLossLimit;
    private bool _savedMoveSlToBreakEven;
    private bool _savedAddDustToTp;
    private CryptoOrderType _savedDcaOrderType;
    private decimal _savedMaxPositionDurationDays;
    private ThreadCheckFinishedPosition? _savedThread;


    [TestInitialize]
    public void SaveSettings()
    {
        InitTestSession();
        var t = GlobalData.Settings.Trading;
        _savedTradeVia = t.TradeVia;
        _savedTpList = t.TpList;
        _savedDcaList = t.DcaList;
        _savedStopLoss = t.StopLossPercentage;
        _savedStopLossLimit = t.StopLossLimitPercentage;
        _savedMoveSlToBreakEven = t.MoveSlToBreakEven;
        _savedAddDustToTp = t.AddDustToTp;
        _savedDcaOrderType = t.DcaOrderType;
        _savedMaxPositionDurationDays = t.MaxPositionDurationDays;
        _savedThread = GlobalData.ThreadCheckPosition;
    }

    [TestCleanup]
    public void RestoreSettings()
    {
        var t = GlobalData.Settings.Trading;
        t.TradeVia = _savedTradeVia;
        t.TpList = _savedTpList;
        t.DcaList = _savedDcaList;
        t.StopLossPercentage = _savedStopLoss;
        t.StopLossLimitPercentage = _savedStopLossLimit;
        t.MoveSlToBreakEven = _savedMoveSlToBreakEven;
        t.AddDustToTp = _savedAddDustToTp;
        t.DcaOrderType = _savedDcaOrderType;
        t.MaxPositionDurationDays = _savedMaxPositionDurationDays;
        GlobalData.ThreadCheckPosition = _savedThread;
    }


    // ── Arrange ─────────────────────────────────────────────────────────────

    /// <summary>
    /// The HyperLiquid settings of position 73: take profit 7.5% in one level, DCA 200% at 2% and
    /// 400% at 4% (more levels when asked), stop loss 4% with a 5% limit, no profit lock.
    /// </summary>
    private static void ApplyTradingSettings(int dcaLevels)
    {
        var t = GlobalData.Settings.Trading;
        t.TradeVia = CryptoTradeVia.PaperTrade;
        t.TpList = [new CryptoTpEntry { Factor = 100m, Percentage = 7.5m }];
        t.DcaList = [];
        for (int level = 1; level <= dcaLevels; level++)
            t.DcaList.Add(new CryptoDcaEntry { Factor = 200m * level, Percentage = 2m * level });
        t.StopLossPercentage = 4m;
        t.StopLossLimitPercentage = 5m;
        t.MoveSlToBreakEven = false;
        t.AddDustToTp = false;
        t.DcaOrderType = CryptoOrderType.Limit;
        t.MaxPositionDurationDays = 0;
    }


    private static (CryptoDatabase database, CryptoSymbol symbol) SetupTestEnvironment(int dcaLevels)
    {
        ApplyTradingSettings(dcaLevels);

        CryptoDatabase database = new();
        database.Open();
        DeleteAllPositionRelatedStuff(database);
        CryptoSymbol symbol = CreateTestSymbol(database);
        symbol.LastPrice = EntryPrice;

        // The position check that follows every fill. Live it runs on its own thread after a delay,
        // in emulator mode (which the test session is) it runs synchronously inside the fill.
        GlobalData.ThreadCheckPosition ??= new ThreadCheckFinishedPosition();

        // Money to pay for the entry and the DCA's (the paper balances really constrain the trader)
        CryptoAsset assetQuote = new() { Name = symbol.Quote, Total = StartCapital, Free = StartCapital, Locked = 0 };
        GlobalData.ActiveExchange!.Data.AssetList.TryAdd(assetQuote.Name, assetQuote);
        assetQuote = GlobalData.ActiveExchange!.Data.AssetList[assetQuote.Name];
        assetQuote.Total = StartCapital;
        assetQuote.Free = StartCapital;
        assetQuote.Locked = 0;
        database.Connection.Insert(assetQuote);

        return (database, symbol);
    }


    /// <summary>
    /// Start of the test's own minute window, at a fixed point in time so every run produces the same
    /// candles (see PaperTradingIntrabarTests.AlignedWindow for the reasoning).
    /// </summary>
    private static CandleTime Window(int windowNumber)
    {
        CandleTime start = CandleTime.AlignFromDateTime(new DateTime(2021, 6, 1, 0, 0, 0, DateTimeKind.Utc), 60);
        return start + (uint)windowNumber * 60;
    }


    /// <summary>
    /// A long position with its market entry order still open, exactly as the trader leaves it before
    /// the first candle: Waiting, one entry part, one buy step.
    /// </summary>
    private static CryptoPosition CreateWaitingPosition(CryptoDatabase database, CryptoSymbol symbol, DateTime createTime)
    {
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        CryptoPosition position = PositionTools.CreatePosition(symbol, "dbr", CryptoTradeSide.Long, "WickTest", symbolInterval, createTime);
        position.EntryPrice = EntryPrice;
        position.EntryAmount = EntryPrice * EntryQuantity; // the quote value of the entry, the DCA factors work on it
        database.Connection.Insert(position);
        PositionTools.AddPosition(position);

        CryptoPositionPart part = PositionTools.ExtendPosition(database, position, CryptoPartPurpose.Entry,
            symbolInterval.Interval, position.Strategy, EntryPrice, createTime);
        TradeParams tradeParams = CreateTradeParams(database, createTime, CryptoOrderSide.Buy, CryptoOrderType.Market, EntryPrice, EntryQuantity);
        CryptoPositionStep step = PositionTools.CreatePositionStep(position, part, tradeParams);
        database.Connection.Insert<CryptoPositionStep>(step);
        PositionTools.AddPositionPartStep(part, step);
        PaperAssets.Change(GlobalData.ActiveExchange!, symbol, position.Side, CryptoOrderSide.Buy,
            step.Status, tradeParams.Quantity, tradeParams.QuoteQuantity, "WickTest entry");
        return position;
    }


    private static CryptoCandle AddCandle(CryptoSymbol symbol, CandleTime openTime, decimal open, decimal high, decimal low, decimal close)
    {
        CryptoCandle candle = new()
        {
            TickDecimals = 4,
            OpenTime = openTime,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = 1000m,
        };
        // PositionOpenAsUsual takes the LAST 1m candle of the symbol for the position check
        symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m).CandleList.Add(openTime, candle);
        symbol.LastPrice = close;
        return candle;
    }


    /// <summary>
    /// Feeds the candle to paper trading with the position check running synchronously inside every
    /// fill - the emulator's way, and the order of events the scanner ends up with as well, only later.
    /// </summary>
    private static async Task ProcessCandleWithCascade(CryptoDatabase database, CryptoSymbol symbol, CryptoCandle candle)
    {
        await PaperTrading.PaperTradingCheckOrders(database, GlobalData.ActiveExchange!, symbol, candle);
    }


    /// <summary>
    /// Feeds the candle to paper trading the way the LIVE scanner experiences it: the fills are
    /// booked first (the position check is queued on another thread with a ten second delay), so
    /// every order that was open when the candle started is checked against the whole candle. Then
    /// the position check runs once for the position, as that thread would.
    /// </summary>
    private static async Task ProcessCandleLikeTheLiveScanner(CryptoDatabase database, CryptoSymbol symbol, CryptoPosition position, CryptoCandle candle)
    {
        ThreadCheckFinishedPosition thread = GlobalData.ThreadCheckPosition!;
        GlobalData.ThreadCheckPosition = null;
        try
        {
            await PaperTrading.PaperTradingCheckOrders(database, GlobalData.ActiveExchange!, symbol, candle);
        }
        finally
        {
            GlobalData.ThreadCheckPosition = thread;
        }
        await thread.AddToQueue(position, null, CryptoOrderStatus.Filled);
    }


    // ── Helpers to read the position ────────────────────────────────────────

    private static List<CryptoPositionStep> OpenSteps(CryptoPosition position, CryptoOrderSide side)
    {
        return position.PartList.Values
            .SelectMany(p => p.StepList.Values)
            .Where(s => s.Side == side && s.Status == CryptoOrderStatus.New)
            .ToList();
    }

    private static List<CryptoPositionStep> FilledSteps(CryptoPosition position, CryptoOrderSide side)
    {
        return position.PartList.Values
            .SelectMany(p => p.StepList.Values)
            .Where(s => s.Side == side && s.Status.IsFilled())
            .ToList();
    }

    /// <summary>
    /// The invariant this whole file is about: an open position has exactly one exit order (one TP
    /// level configured), it carries a stop, and it covers everything the position holds.
    /// </summary>
    private static CryptoPositionStep AssertEveryCoinHasAnExitOrder(CryptoPosition position, string when)
    {
        Assert.AreEqual(CryptoPositionStatus.Trading, position.Status, $"{when}: position should still be trading");
        Assert.IsTrue(position.Quantity > 0, $"{when}: position should still hold coins");

        List<CryptoPositionStep> exits = OpenSteps(position, CryptoOrderSide.Sell);
        Assert.AreEqual(1, exits.Count, $"{when}: exactly one exit order expected for the one TP level");

        CryptoPositionStep exit = exits[0];
        Assert.IsTrue(exit.StopPrice.HasValue, $"{when}: the exit order should carry the stop loss");
        Assert.IsTrue(exit.Price > exit.StopPrice, $"{when}: take profit above the stop for a long");
        Assert.IsTrue(Math.Abs(exit.Quantity + exit.RemainingDust - position.Quantity) < position.Symbol.QuantityTickSize,
            $"{when}: the exit order ({exit.Quantity} + dust {exit.RemainingDust}) should cover the whole position ({position.Quantity})");
        return exit;
    }

    private static void AssertPositionClosedCompletely(CryptoPosition position, string when)
    {
        Assert.AreEqual(CryptoPositionStatus.Ready, position.Status, $"{when}: position should be closed");
        Assert.IsTrue(position.CloseTime.HasValue, $"{when}: close time should be set");
        Assert.IsTrue(position.Quantity - position.RemainingDust < position.Symbol.QuantityMinimum,
            $"{when}: nothing sellable should be left, quantity={position.Quantity} dust={position.RemainingDust}");
        Assert.AreEqual(0, OpenSteps(position, CryptoOrderSide.Buy).Count, $"{when}: no open DCA orders");
        Assert.AreEqual(0, OpenSteps(position, CryptoOrderSide.Sell).Count, $"{when}: no open exit orders");
    }


    /// <summary>
    /// Opens the position on a flat candle: the entry fills and the trader places the DCA's and the
    /// exit order itself. Returns the exit order as placed for the entry quantity only.
    /// </summary>
    private static async Task<CryptoPositionStep> OpenPositionOnFlatCandle(CryptoDatabase database, CryptoSymbol symbol,
        CryptoPosition position, CandleTime window, int dcaLevels)
    {
        CryptoCandle flat = AddCandle(symbol, window, EntryPrice, EntryPrice, EntryPrice, EntryPrice);
        await ProcessCandleWithCascade(database, symbol, flat);

        Assert.AreEqual(1, FilledSteps(position, CryptoOrderSide.Buy).Count, "entry should be filled");
        Assert.AreEqual(dcaLevels, OpenSteps(position, CryptoOrderSide.Buy).Count, $"{dcaLevels} DCA orders expected after the entry");
        return AssertEveryCoinHasAnExitOrder(position, "after the entry");
    }


    /// <summary>
    /// The wick: opens at the entry price, drops below the stop loss (which sits 4% under the lowest
    /// DCA) and closes just under the entry. Every DCA and the stop are inside this one candle.
    /// </summary>
    private static CryptoCandle WickCandle(CryptoSymbol symbol, CandleTime openTime, decimal stopPrice)
        => AddCandle(symbol, openTime, EntryPrice, EntryPrice, stopPrice * 0.98m, EntryPrice * 0.99m);


    // ═══════════════════════════════════════════════════════════════════════
    //  The live scanner: fills first, the position check later
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The CAKE case as the live scanner saw it. All DCA's fill and the stop of the exit order fills
    /// for the entry quantity, in the same candle, before the position check gets a turn. Afterwards
    /// the DCA quantity must have an exit order again - it did not, that was the bug.
    /// </summary>
    [TestMethod]
    public async Task LiveScanner_WickFillsTwoDcasAndStopLoss_RemainderGetsAnExitOrder()
    {
        var (database, symbol) = SetupTestEnvironment(dcaLevels: 2);
        CandleTime window = Window(1);
        CryptoPosition position = CreateWaitingPosition(database, symbol, window.ToDateTime().AddHours(-1));
        CryptoPositionStep exitBefore = await OpenPositionOnFlatCandle(database, symbol, position, window, dcaLevels: 2);
        decimal quantityBefore = position.Quantity;

        CryptoCandle wick = WickCandle(symbol, window + 1, exitBefore.StopPrice!.Value);
        await ProcessCandleLikeTheLiveScanner(database, symbol, position, wick);

        // What the candle did: two DCA fills and one stop fill for the old exit quantity
        Assert.AreEqual(3, FilledSteps(position, CryptoOrderSide.Buy).Count, "entry and both DCA's should be filled");
        Assert.AreEqual(1, FilledSteps(position, CryptoOrderSide.Sell).Count, "the stop should have filled once");
        Assert.AreEqual(exitBefore.StopPrice, FilledSteps(position, CryptoOrderSide.Sell)[0].AveragePrice, "the stop fills at the stop price");
        Assert.AreEqual(0, OpenSteps(position, CryptoOrderSide.Buy).Count, "no DCA left open");
        Assert.IsTrue(position.Quantity > quantityBefore, "the DCA's bought more than the stop sold");

        // The invariant: the coins from the DCA's are covered again
        CryptoPositionStep exitAfter = AssertEveryCoinHasAnExitOrder(position, "after the wick");
        Assert.AreNotEqual(exitBefore.Id, exitAfter.Id, "a NEW exit order is expected, the old one was filled");
        Assert.IsTrue(exitAfter.StopPrice!.Value < EntryPrice, "the stop belongs below the entry");
    }


    /// <summary>The follow-up: the next candle through the new stop has to close the position completely.</summary>
    [TestMethod]
    public async Task LiveScanner_AfterTheWick_NextCandleThroughTheStopClosesThePosition()
    {
        var (database, symbol) = SetupTestEnvironment(dcaLevels: 2);
        CandleTime window = Window(2);
        CryptoPosition position = CreateWaitingPosition(database, symbol, window.ToDateTime().AddHours(-1));
        CryptoPositionStep exitBefore = await OpenPositionOnFlatCandle(database, symbol, position, window, dcaLevels: 2);

        CryptoCandle wick = WickCandle(symbol, window + 1, exitBefore.StopPrice!.Value);
        await ProcessCandleLikeTheLiveScanner(database, symbol, position, wick);
        CryptoPositionStep exitAfter = AssertEveryCoinHasAnExitOrder(position, "after the wick");

        // Price keeps falling through the new stop
        decimal stop = exitAfter.StopPrice!.Value;
        CryptoCandle drop = AddCandle(symbol, window + 2, stop * 1.01m, stop * 1.01m, stop * 0.97m, stop * 0.98m);
        await ProcessCandleLikeTheLiveScanner(database, symbol, position, drop);

        AssertPositionClosedCompletely(position, "after the drop");
        Assert.AreEqual(2, FilledSteps(position, CryptoOrderSide.Sell).Count, "two stop fills in total: the entry quantity and the DCA quantity");
    }


    /// <summary>Same wick, three DCA levels: every level fills, the stop fills, the rest gets an exit order.</summary>
    [TestMethod]
    public async Task LiveScanner_WickFillsThreeDcasAndStopLoss_RemainderGetsAnExitOrder()
    {
        var (database, symbol) = SetupTestEnvironment(dcaLevels: 3);
        CandleTime window = Window(3);
        CryptoPosition position = CreateWaitingPosition(database, symbol, window.ToDateTime().AddHours(-1));
        CryptoPositionStep exitBefore = await OpenPositionOnFlatCandle(database, symbol, position, window, dcaLevels: 3);

        CryptoCandle wick = WickCandle(symbol, window + 1, exitBefore.StopPrice!.Value);
        await ProcessCandleLikeTheLiveScanner(database, symbol, position, wick);

        Assert.AreEqual(4, FilledSteps(position, CryptoOrderSide.Buy).Count, "entry and three DCA's should be filled");
        Assert.AreEqual(1, FilledSteps(position, CryptoOrderSide.Sell).Count, "the stop should have filled once");
        AssertEveryCoinHasAnExitOrder(position, "after the wick");

        CryptoPositionStep exitAfter = OpenSteps(position, CryptoOrderSide.Sell)[0];
        decimal stop = exitAfter.StopPrice!.Value;
        CryptoCandle drop = AddCandle(symbol, window + 2, stop * 1.01m, stop * 1.01m, stop * 0.97m, stop * 0.98m);
        await ProcessCandleLikeTheLiveScanner(database, symbol, position, drop);
        AssertPositionClosedCompletely(position, "after the drop");
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The emulator: the position check runs inside every fill
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The same wick in the emulator. Here every DCA fill immediately replaces the exit order with one
    /// for the new quantity, so the stop that fills (if it fills at all on this candle) is never the
    /// stale one. Whatever the sequence, the invariant has to hold afterwards.
    /// </summary>
    [TestMethod]
    public async Task Emulator_WickFillsTwoDcasAndStopLoss_EveryCoinKeepsAnExitOrder()
    {
        var (database, symbol) = SetupTestEnvironment(dcaLevels: 2);
        CandleTime window = Window(4);
        CryptoPosition position = CreateWaitingPosition(database, symbol, window.ToDateTime().AddHours(-1));
        CryptoPositionStep exitBefore = await OpenPositionOnFlatCandle(database, symbol, position, window, dcaLevels: 2);

        CryptoCandle wick = WickCandle(symbol, window + 1, exitBefore.StopPrice!.Value);
        await ProcessCandleWithCascade(database, symbol, wick);

        Assert.AreEqual(3, FilledSteps(position, CryptoOrderSide.Buy).Count, "entry and both DCA's should be filled");
        Assert.AreEqual(0, OpenSteps(position, CryptoOrderSide.Buy).Count, "no DCA left open");
        if (position.Status == CryptoPositionStatus.Trading)
            AssertEveryCoinHasAnExitOrder(position, "after the wick");
        else
            AssertPositionClosedCompletely(position, "after the wick");
    }


    [TestMethod]
    public async Task Emulator_AfterTheWick_NextCandleThroughTheStopClosesThePosition()
    {
        var (database, symbol) = SetupTestEnvironment(dcaLevels: 2);
        CandleTime window = Window(5);
        CryptoPosition position = CreateWaitingPosition(database, symbol, window.ToDateTime().AddHours(-1));
        CryptoPositionStep exitBefore = await OpenPositionOnFlatCandle(database, symbol, position, window, dcaLevels: 2);

        CryptoCandle wick = WickCandle(symbol, window + 1, exitBefore.StopPrice!.Value);
        await ProcessCandleWithCascade(database, symbol, wick);

        if (position.Status == CryptoPositionStatus.Trading)
        {
            CryptoPositionStep exitAfter = AssertEveryCoinHasAnExitOrder(position, "after the wick");
            decimal stop = exitAfter.StopPrice!.Value;
            CryptoCandle drop = AddCandle(symbol, window + 2, stop * 1.01m, stop * 1.01m, stop * 0.97m, stop * 0.98m);
            await ProcessCandleWithCascade(database, symbol, drop);
        }

        AssertPositionClosedCompletely(position, "at the end");
    }
}
