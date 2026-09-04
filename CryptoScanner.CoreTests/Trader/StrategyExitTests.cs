using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// A strategy asking for the exit (SignalCreateBase.IsExitSignal sets CryptoPosition.ExitRequested)
/// takes the same door a position past its maximum duration takes. The rule is the strategy's; what
/// can break is the plumbing, and it breaks the same way the deadline did: two gates decide whether a
/// candle is worth looking at, both answer "no" for a candle that reaches no trigger price, and the
/// candle after a cross back is exactly such a candle. Teach only one of them and the exit looks
/// implemented and never fires (see MaxPositionDurationTests for the history).
/// </summary>
[TestClass]
public class StrategyExitTests
{
    private static readonly DateTime Opened = new(2026, 3, 1, 12, 0, 0, DateTimeKind.Utc);

    private static CryptoPosition MakePosition()
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
            CreateTime = Opened,
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
            TriggerPriceTop = 110m,
            TriggerPriceBottom = 90m,
        };
    }

    private static CryptoCandle QuietCandle(DateTime closeTime) => new()
    {
        OpenTime = CandleTime.FromDateTime(closeTime),
        Open = 100m,
        High = 101m,
        Low = 99m,
        Close = 100m,
    };

    private decimal _savedMaxDuration;

    // The deadline is the other reason a quiet candle gets through; off, so it cannot be the reason here.
    [TestInitialize]
    public void SaveSettings()
    {
        _savedMaxDuration = GlobalData.Settings.Trading.MaxPositionDurationDays;
        GlobalData.Settings.Trading.MaxPositionDurationDays = 0m;
    }

    [TestCleanup]
    public void RestoreSettings() => GlobalData.Settings.Trading.MaxPositionDurationDays = _savedMaxDuration;


    /// <summary>
    /// A fresh position has not been asked to leave, and a quiet candle inside the fence is skipped.
    /// Here to prove the tests below do not pass because the gates let everything through.
    /// </summary>
    [TestMethod]
    public void WithoutTheRequest_AQuietCandleIsStillSkipped()
    {
        var position = MakePosition();
        Assert.IsFalse(position.ExitRequested, "a new position has not asked to leave");

        DateTime now = Opened.AddHours(1);
        Assert.IsFalse(PositionMonitor.CandleCanMovePosition(position, QuietCandle(now), CandleTime.FromDateTime(now)));
        Assert.IsFalse(PositionMonitor.ShouldRunHandlePosition(position, 105m, 95m, now));
    }


    /// <summary>
    /// The first gate: whether the replay descends into the minute candles at all.
    /// </summary>
    [TestMethod]
    public void FirstGate_WithTheRequest_LetsAQuietCandleThrough()
    {
        var position = MakePosition();
        position.ExitRequested = true;

        DateTime now = Opened.AddHours(1);
        Assert.IsTrue(PositionMonitor.CandleCanMovePosition(position, QuietCandle(now), CandleTime.FromDateTime(now)));
    }


    /// <summary>
    /// The second gate: whether the orders are recomputed once the candle is looked at. Without this
    /// one the take profit is never repriced through the last price and the position sits there.
    /// </summary>
    [TestMethod]
    public void SecondGate_WithTheRequest_RepricesEvenInsideTheFence()
    {
        var position = MakePosition();
        position.ExitRequested = true;

        Assert.IsFalse(PositionMonitor.ShouldRunHandlePosition(position, candleHigh: 105m, candleLow: 95m),
            "the price-only overload knows nothing about the request, which is fine: the replay never calls it alone");
        Assert.IsTrue(PositionMonitor.ShouldRunHandlePosition(position, 105m, 95m, Opened.AddHours(1)),
            "the overload the monitor uses has to say yes");
    }


    /// <summary>
    /// The price fence keeps working as it did: a candle that reaches a trigger price goes through
    /// with or without the request.
    /// </summary>
    [TestMethod]
    public void ACandleThatReachesATrigger_GoesThroughEitherWay()
    {
        var position = MakePosition();
        DateTime now = Opened.AddHours(1);
        CryptoCandle reaching = QuietCandle(now);
        reaching.High = 115m;

        Assert.IsTrue(PositionMonitor.CandleCanMovePosition(position, reaching, CandleTime.FromDateTime(now)));
        position.ExitRequested = true;
        Assert.IsTrue(PositionMonitor.CandleCanMovePosition(position, reaching, CandleTime.FromDateTime(now)));
    }
}
