using CryptoScanner.Analyzers.XsMom;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// Cross-sectional momentum: the ranking (XsMomRanking.Select) on a small, hand-made market, the
/// rebalance moment, and the funding amount of a position (FundingTools.Amount).
/// </summary>
[TestClass]
public class XsMomTests
{
    private const uint Day = 1440;

    // Monday 2026-10-05 00:00 UTC
    private static readonly CandleTime Monday = CandleTime.FromDateTime(new DateTime(2026, 10, 5, 0, 0, 0, DateTimeKind.Utc));


    private static XsMomSettings Settings(int lookback = 2, int top = 100, int perSide = 1, decimal keep = 1m)
        => new() { LookbackDays = lookback, TopByVolume = top, VolumeDays = 2, CoinsPerSide = perSide, KeepWithinFactor = keep };


    /// <summary>
    /// A coin with a daily bar per close in <paramref name="closes"/>, the last one on the day before
    /// <paramref name="rebalance"/>, every bar with the same base volume.
    /// </summary>
    private static SortedList<uint, XsMomDailyBar> Bars(CandleTime rebalance, decimal volume, params decimal[] closes)
    {
        SortedList<uint, XsMomDailyBar> bars = [];
        for (int i = 0; i < closes.Length; i++)
        {
            uint openTime = rebalance.Minutes - (uint)(closes.Length - i) * Day;
            bars[openTime] = new XsMomDailyBar(closes[i], volume * closes[i]);
        }
        return bars;
    }


    [TestMethod]
    public void TheStrongestGoesLongAndTheWeakestShort()
    {
        // Returns over 2 days: A +20%, B 0%, C -10%
        Dictionary<string, SortedList<uint, XsMomDailyBar>> data = new()
        {
            ["A"] = Bars(Monday, 1000, 10m, 11m, 12m),
            ["B"] = Bars(Monday, 1000, 10m, 10m, 10m),
            ["C"] = Bars(Monday, 1000, 10m, 9.5m, 9m),
        };

        XsMomSelection selection = XsMomRanking.Select(data, Monday, Settings(), null);

        Assert.AreEqual(3, selection.RankedCount);
        Assert.AreEqual(2, selection.Picks.Count);
        Assert.IsTrue(selection.TryGet("A", out XsMomPick a));
        Assert.AreEqual(CryptoTradeSide.Long, a.Side);
        Assert.AreEqual(20m, a.ReturnPercentage);
        Assert.IsTrue(selection.TryGet("C", out XsMomPick c));
        Assert.AreEqual(CryptoTradeSide.Short, c.Side);
        Assert.AreEqual(0, c.Rank);
        Assert.IsFalse(selection.TryGet("B", out _));
    }


    /// <summary>
    /// The bar of the rebalance day itself has not closed at the rebalance moment. Whatever it says,
    /// the selection must not change.
    /// </summary>
    [TestMethod]
    public void ABarThatClosesAfterTheRebalanceMomentIsNotRead()
    {
        Dictionary<string, SortedList<uint, XsMomDailyBar>> data = new()
        {
            ["A"] = Bars(Monday, 1000, 10m, 11m, 12m),
            ["B"] = Bars(Monday, 1000, 10m, 10m, 10m),
            ["C"] = Bars(Monday, 1000, 10m, 9.5m, 9m),
        };
        XsMomSelection before = XsMomRanking.Select(data, Monday, Settings(), null);

        // The future: C explodes and A crashes on the rebalance day, and a huge volume
        data["A"][Monday.Minutes] = new XsMomDailyBar(1m, 1e12m);
        data["C"][Monday.Minutes] = new XsMomDailyBar(100m, 1e12m);
        XsMomSelection after = XsMomRanking.Select(data, Monday, Settings(), null);

        CollectionAssert.AreEquivalent(before.Picks.ToList(), after.Picks.ToList());
    }


    [TestMethod]
    public void OnlyTheTopByVolumeTakesPart()
    {
        // D has the best return but the smallest volume; with a top of 3 it is out
        Dictionary<string, SortedList<uint, XsMomDailyBar>> data = new()
        {
            ["A"] = Bars(Monday, 1000, 10m, 11m, 12m),
            ["B"] = Bars(Monday, 1000, 10m, 10m, 10m),
            ["C"] = Bars(Monday, 1000, 10m, 9.5m, 9m),
            ["D"] = Bars(Monday, 1, 10m, 15m, 20m),
        };

        XsMomSelection selection = XsMomRanking.Select(data, Monday, Settings(top: 3), null);

        Assert.AreEqual(3, selection.RankedCount);
        Assert.IsFalse(selection.TryGet("D", out _));
        Assert.IsTrue(selection.TryGet("A", out XsMomPick a) && a.Side == CryptoTradeSide.Long);
    }


    [TestMethod]
    public void ACoinWithoutACloseOnTheLookbackDayIsLeftOut()
    {
        Dictionary<string, SortedList<uint, XsMomDailyBar>> data = new()
        {
            ["A"] = Bars(Monday, 1000, 10m, 11m, 12m),
            ["B"] = Bars(Monday, 1000, 10m, 10m, 10m),
            // Listed yesterday: only one bar
            ["NEW"] = Bars(Monday, 1000, 50m),
        };

        XsMomSelection selection = XsMomRanking.Select(data, Monday, Settings(), null);

        Assert.AreEqual(2, selection.RankedCount);
        Assert.IsFalse(selection.TryGet("NEW", out _));
    }


    /// <summary>
    /// Keep within 2 x 1 coin: the long of last week (B) is now second strongest, so it stays and the
    /// new strongest (A) does not come in on top of it (one coin per side).
    /// </summary>
    [TestMethod]
    public void AHeldCoinWithinTheKeepZoneStays()
    {
        Dictionary<string, SortedList<uint, XsMomDailyBar>> data = new()
        {
            ["A"] = Bars(Monday, 1000, 10m, 11m, 12m),   // +20%
            ["B"] = Bars(Monday, 1000, 10m, 10.5m, 11m), // +10%
            ["C"] = Bars(Monday, 1000, 10m, 10m, 10m),   //   0%
            ["D"] = Bars(Monday, 1000, 10m, 9.5m, 9m),   // -10%
        };
        XsMomSelection previous = new()
        {
            RebalanceTime = Monday - 7 * Day,
            RankedCount = 4,
            Picks = new() { ["B"] = new XsMomPick(CryptoTradeSide.Long, 3, 4, 5m) },
        };

        XsMomSelection kept = XsMomRanking.Select(data, Monday, Settings(keep: 2m), previous);
        XsMomSelection plain = XsMomRanking.Select(data, Monday, Settings(keep: 1m), previous);

        Assert.IsTrue(kept.TryGet("B", out XsMomPick b) && b.Side == CryptoTradeSide.Long);
        Assert.IsFalse(kept.TryGet("A", out _));
        Assert.IsTrue(plain.TryGet("A", out XsMomPick a) && a.Side == CryptoTradeSide.Long);
        Assert.IsFalse(plain.TryGet("B", out _));
    }


    [TestMethod]
    public void TheRebalanceMomentIsMidnightOnTheRebalanceDay()
    {
        XsMomSettings settings = new();
        Assert.IsTrue(XsMomRanking.IsRebalanceMoment(Monday, settings));
        Assert.IsFalse(XsMomRanking.IsRebalanceMoment(Monday + 60u, settings), "01:00 is not midnight");
        Assert.IsFalse(XsMomRanking.IsRebalanceMoment(Monday + Day, settings), "Tuesday");

        settings.RebalanceDay = DayOfWeek.Tuesday;
        Assert.IsTrue(XsMomRanking.IsRebalanceMoment(Monday + Day, settings));

        // Every two weeks: one of two consecutive Mondays
        settings.RebalanceDay = DayOfWeek.Monday;
        settings.RebalanceEveryWeeks = 2;
        bool first = XsMomRanking.IsRebalanceMoment(Monday, settings);
        bool second = XsMomRanking.IsRebalanceMoment(Monday + 7 * Day, settings);
        Assert.AreNotEqual(first, second);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Funding
    // ═══════════════════════════════════════════════════════════════════════

    private static CryptoPosition Position(CryptoTradeSide side, decimal quantity)
    {
        var exchange = new Exchange { Id = 1, Name = "Binance Perpetual" };
        var symbol = new CryptoSymbol
        {
            Id = 1,
            Status = 1,
            Base = "SOL",
            Quote = "USDT",
            Name = "SOLUSDT.PERP",
            Exchange = exchange,
            ExchangeName = "SOLUSDT",
            QuoteData = new CryptoQuoteData { Name = "USDT" },
            LastPrice = 150m,
        };
        return new CryptoPosition
        {
            Symbol = symbol,
            Exchange = exchange,
            Side = side,
            Interval = new CryptoInterval { Id = 15, Name = "1d", IntervalPeriod = CryptoIntervalPeriod.interval1d, Duration = 1440 },
            Quantity = quantity,
            Status = CryptoPositionStatus.Trading,
        };
    }


    /// <summary>
    /// 2 SOL at a mark price of 100 with a rate of 0.01% (0.0001): 200 x 0.0001 = 0.02. A positive
    /// rate: the long pays it, the short receives it.
    /// </summary>
    [TestMethod]
    public void APositiveRateIsPaidByTheLongAndReceivedByTheShort()
    {
        CryptoFundingRate rate = new(new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc), 0.0001m, 100m);

        Assert.AreEqual(-0.02m, FundingTools.Amount(Position(CryptoTradeSide.Long, 2m), rate));
        Assert.AreEqual(0.02m, FundingTools.Amount(Position(CryptoTradeSide.Short, 2m), rate));
    }


    [TestMethod]
    public void ANegativeRateIsReceivedByTheLong()
    {
        CryptoFundingRate rate = new(new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc), -0.0005m, 100m);

        Assert.AreEqual(0.1m, FundingTools.Amount(Position(CryptoTradeSide.Long, 2m), rate));
        Assert.AreEqual(-0.1m, FundingTools.Amount(Position(CryptoTradeSide.Short, 2m), rate));
    }


    [TestMethod]
    public void WithoutAMarkPriceTheLastPriceIsUsedAndANothingOpenPaysNothing()
    {
        CryptoFundingRate rate = new(new DateTime(2026, 10, 5, 8, 0, 0, DateTimeKind.Utc), 0.0001m, 0m);

        // 2 x 150 (last price) x 0.0001 = 0.03
        Assert.AreEqual(-0.03m, FundingTools.Amount(Position(CryptoTradeSide.Long, 2m), rate));
        Assert.AreEqual(0m, FundingTools.Amount(Position(CryptoTradeSide.Long, 0m), rate));
    }
}
