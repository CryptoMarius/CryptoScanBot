using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Trading.MaxPositionDurationDays: the deadline after which a position stops waiting for its
/// profit target and leaves at whatever the market offers.
/// <para>
/// The rule itself is arithmetic and hard to get wrong. The gate is not: CandleCanMovePosition
/// decides whether a candle is worth looking at, and it answers "no" for every candle that reaches
/// no trigger price - which is exactly the quiet candle on which a deadline expires. Miss that and
/// the setting looks implemented, passes a reading of the code, and never fires. That is what the
/// last two tests here are for.
/// </para>
/// </summary>
[TestClass]
public class MaxPositionDurationTests
{
    private static readonly DateTime Opened = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private static CryptoPosition MakePosition(DateTime createTime)
    {
        var exchange = new Exchange { Id = 1, Name = "TestExchange", FeeRate = 0.1m };
        var symbol = new CryptoSymbol
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
        };
        return new CryptoPosition
        {
            Id = 1,
            CreateTime = createTime,
            Exchange = exchange,
            ExchangeId = exchange.Id,
            Symbol = symbol,
            SymbolId = 1,
            Interval = GlobalData.IntervalListPeriod.Count > 0
                ? GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m]
                : new CryptoInterval { Id = 6, Name = "15m", Duration = 900 },
            IntervalId = 6,
            Side = CryptoTradeSide.Long,
            Status = CryptoPositionStatus.Trading,
            HasOrdersAndTradesLoaded = true,
        };
    }

    private decimal _saved;

    [TestInitialize]
    public void SaveSettings() => _saved = GlobalData.Settings.Trading.MaxPositionDurationDays;

    [TestCleanup]
    public void RestoreSettings() => GlobalData.Settings.Trading.MaxPositionDurationDays = _saved;


    // ═══════════════════════════════════════════════════════════════════════
    //  The rule
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Zero is the default and has to mean "no deadline at all", because every scanner and every
    /// run that predates this setting carries that zero.
    /// </summary>
    [TestMethod]
    public void Zero_NeverExpires_NoMatterHowOldThePositionIs()
    {
        GlobalData.Settings.Trading.MaxPositionDurationDays = 0m;
        var position = MakePosition(Opened);

        Assert.IsFalse(PositionMonitor.IsPastMaxDuration(position, Opened.AddYears(1)));
    }


    [TestMethod]
    public void JustBeforeTheDeadline_IsNotPast()
    {
        GlobalData.Settings.Trading.MaxPositionDurationDays = 30m;
        var position = MakePosition(Opened);

        Assert.IsFalse(PositionMonitor.IsPastMaxDuration(position, Opened.AddDays(30).AddMinutes(-1)));
    }


    [TestMethod]
    public void OnTheDeadline_IsPast()
    {
        GlobalData.Settings.Trading.MaxPositionDurationDays = 30m;
        var position = MakePosition(Opened);

        Assert.IsTrue(PositionMonitor.IsPastMaxDuration(position, Opened.AddDays(30)));
        Assert.IsTrue(PositionMonitor.IsPastMaxDuration(position, Opened.AddDays(64.6)));
    }


    /// <summary>
    /// Decimal days, so a deadline shorter than a day can be measured as well - the median position
    /// on the runs that prompted this closes in about 30 hours, so whole days are a coarse grid.
    /// </summary>
    [TestMethod]
    public void FractionalDays_AreHonoured()
    {
        GlobalData.Settings.Trading.MaxPositionDurationDays = 0.5m;
        var position = MakePosition(Opened);

        Assert.IsFalse(PositionMonitor.IsPastMaxDuration(position, Opened.AddHours(11)));
        Assert.IsTrue(PositionMonitor.IsPastMaxDuration(position, Opened.AddHours(12)));
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  The gate — the part that actually breaks
    // ═══════════════════════════════════════════════════════════════════════

    private static CryptoCandle QuietCandle(DateTime closeTime) => new()
    {
        OpenTime = CandleTime.FromDateTime(closeTime),
        Open = 100m,
        High = 101m,
        Low = 99m,
        Close = 100m,
    };

    /// <summary>
    /// A candle that stays inside the fence and a position that is still within its deadline: the
    /// gate says no, and that is the optimization doing its job. Here to prove the test below is
    /// not passing because the gate returns true for everything.
    /// </summary>
    [TestMethod]
    public void QuietCandle_BeforeTheDeadline_IsStillSkipped()
    {
        GlobalData.Settings.Trading.MaxPositionDurationDays = 30m;
        var position = MakePosition(Opened);
        position.TriggerPriceTop = 110m;
        position.TriggerPriceBottom = 90m;

        DateTime now = Opened.AddDays(5);
        Assert.IsFalse(PositionMonitor.CandleCanMovePosition(
            position, QuietCandle(now), CandleTime.FromDateTime(now)));
    }


    /// <summary>
    /// Same fence, same untouched trigger prices, only the deadline has passed. The gate has to let
    /// this candle through or the position never leaves.
    /// </summary>
    [TestMethod]
    public void QuietCandle_PastTheDeadline_IsNotSkipped()
    {
        GlobalData.Settings.Trading.MaxPositionDurationDays = 30m;
        var position = MakePosition(Opened);
        position.TriggerPriceTop = 110m;
        position.TriggerPriceBottom = 90m;

        DateTime now = Opened.AddDays(31);
        Assert.IsTrue(PositionMonitor.CandleCanMovePosition(
            position, QuietCandle(now), CandleTime.FromDateTime(now)));
    }


    /// <summary>
    /// The SECOND gate. CandleCanMovePosition only decides whether the replay descends into the
    /// minute candles; ShouldRunHandlePosition decides whether the orders are actually recomputed
    /// once it has. Both have to know about the deadline.
    /// <para>
    /// Teaching only the first one was the original mistake, and it failed while looking like it
    /// worked: on runs 436-438 a "7 day" limit still left positions running 36.8 days, and a
    /// "30 day" limit changed nothing at all - the exit order was only ever repriced on a candle
    /// that reached a trigger price, which the walked-away positions never do.
    /// </para>
    /// </summary>
    [TestMethod]
    public void SecondGate_PastTheDeadline_RepricesEvenWhenThePriceIsInsideTheFence()
    {
        GlobalData.Settings.Trading.MaxPositionDurationDays = 30m;
        var position = MakePosition(Opened);
        position.TriggerPriceTop = 110m;
        position.TriggerPriceBottom = 90m;

        // The candle sits well inside the fence, so on price alone this is a skip.
        Assert.IsFalse(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 105m, candleLow: 95m),
            "inside the price fence and inside the deadline: skip");
        Assert.IsFalse(PositionMonitor.ShouldRunHandlePosition(position, 105m, 95m, Opened.AddDays(5)),
            "same candle, well inside the deadline: still a skip");
        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, 105m, 95m, Opened.AddDays(31)),
            "same candle, deadline passed: handle it now");
    }


    [TestMethod]
    public void SecondGate_WithTheSettingOff_IsUnchangedAtAnyAge()
    {
        GlobalData.Settings.Trading.MaxPositionDurationDays = 0m;
        var position = MakePosition(Opened);
        position.TriggerPriceTop = 110m;
        position.TriggerPriceBottom = 90m;

        Assert.IsFalse(PositionMonitor.ShouldRunHandlePosition(position, 105m, 95m, Opened.AddDays(365)));
        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, 115m, 95m, Opened.AddDays(365)),
            "the price fence keeps working as it did");
    }


    /// <summary>
    /// And with the setting off the gate behaves exactly as it did before this feature existed,
    /// however long the position has been open.
    /// </summary>
    [TestMethod]
    public void QuietCandle_WithTheSettingOff_IsSkippedAtAnyAge()
    {
        GlobalData.Settings.Trading.MaxPositionDurationDays = 0m;
        var position = MakePosition(Opened);
        position.TriggerPriceTop = 110m;
        position.TriggerPriceBottom = 90m;

        DateTime now = Opened.AddDays(365);
        Assert.IsFalse(PositionMonitor.CandleCanMovePosition(
            position, QuietCandle(now), CandleTime.FromDateTime(now)));
    }
}
