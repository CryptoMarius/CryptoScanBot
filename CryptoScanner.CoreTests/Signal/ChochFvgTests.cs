using CryptoScanner.Analyzers.ChochFvg;
using CryptoScanner.Analyzers.ChochFvg.Signal;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// Change of character into a fair value gap. The rules are fed eleven candles worked out by hand,
/// with swing strength 1 and a lookback of 8:
/// <para>
/// Swing high 13 (candle 1), lower high 12 (candle 4), lower low 7 (candle 6). Candles 7..9 make a
/// bullish gap 9.5..10 (middle 9.75) with the middle candle's low at 9. Candle 10 is the first close
/// above 12: the change of character. The leg runs 7..13, so the 50..61.8 zone is 9.292..10.
/// Entry 9.75, stop 9, target 9.75 + 4 x 0.75 = 12.75.
/// </para>
/// </summary>
[TestClass]
public class ChochFvgTests
{
    private static List<ChochFvgBar> LongCase() =>
    [
        new(10.5m, 11m, 10m, 10.5m),      // 0
        new(11.5m, 13m, 11m, 12m),        // 1 swing high 13
        new(11.5m, 12m, 10m, 10.5m),      // 2
        new(10.5m, 11m, 9m, 10m),         // 3 lowest low between the two swing highs: 9
        new(10.5m, 12m, 10m, 11m),        // 4 lower high 12
        new(10.5m, 11m, 8m, 8.5m),        // 5
        new(8.5m, 10m, 7m, 7.5m),         // 6 lower low 7
        new(7.5m, 9.5m, 8m, 9m),          // 7 gap bottom 9.5
        new(9m, 11m, 9m, 10.8m),          // 8 middle candle of the gap, low 9
        new(10.8m, 11.5m, 10m, 11m),      // 9 gap top 10
        new(11m, 13m, 11m, 12.5m),        // 10 first close above 12
    ];

    private static bool Find(IReadOnlyList<ChochFvgBar> bars, bool isLong, out ChochFvgSetup? setup, out string reason)
        => ChochFvgRules.TryFindSetup(bars, isLong, swingStrength: 1, lookbackCandles: 8,
            fibZoneStart: 50m, fibZoneEnd: 61.8m, riskReward: 4m, out setup, out reason);

    /// <summary>The same case upside down around 30, for the short.</summary>
    private static List<ChochFvgBar> Mirror(List<ChochFvgBar> bars)
        => [.. bars.Select(b => new ChochFvgBar(30 - b.Open, 30 - b.Low, 30 - b.High, 30 - b.Close))];


    [TestMethod]
    public void Long_ChangeOfCharacterWithGapInZone_GivesLimitStopAndTarget()
    {
        Assert.IsTrue(Find(LongCase(), true, out ChochFvgSetup? setup, out string reason), reason);
        Assert.AreEqual(9.75m, setup!.Entry);
        Assert.AreEqual(9m, setup.Stop);
        Assert.AreEqual(12.75m, setup.Target);
        Assert.AreEqual(7m, setup.LegStart);
        Assert.AreEqual(13m, setup.LegEnd);
        Assert.AreEqual(12m, setup.BrokenSwing);
        Assert.AreEqual(8, setup.GapIndex - 1, "the middle candle of the gap is candle 8");
        // 0.75 / 9.75 = 7.69%, the target four times that
        Assert.AreEqual(100m * 0.75m / 9.75m, setup.StopPercentage);
        Assert.AreEqual(4m * setup.StopPercentage, setup.TargetPercentage);
    }

    [TestMethod]
    public void Short_MirrorImage_GivesMirroredPrices()
    {
        Assert.IsTrue(Find(Mirror(LongCase()), false, out ChochFvgSetup? setup, out string reason), reason);
        Assert.AreEqual(20.25m, setup!.Entry);
        Assert.AreEqual(21m, setup.Stop);
        Assert.AreEqual(17.25m, setup.Target);
        Assert.AreEqual(18m, setup.BrokenSwing);
    }

    [TestMethod]
    public void Long_OnTheShortSide_GivesNothing()
    {
        Assert.IsFalse(Find(LongCase(), false, out _, out _));
    }

    [TestMethod]
    public void Long_EarlierCloseAboveTheLowerHigh_IsNotTheChangeOfCharacter()
    {
        var bars = LongCase();
        bars[9] = new ChochFvgBar(10.8m, 12.5m, 10m, 12.2m);
        Assert.IsFalse(Find(bars, true, out _, out string reason));
        Assert.AreEqual("the change of character came earlier", reason);
    }

    [TestMethod]
    public void Long_GapMiddleAlreadyTraded_GivesNothing()
    {
        var bars = LongCase();
        bars[10] = new ChochFvgBar(11m, 13m, 9.7m, 12.5m);
        Assert.IsFalse(Find(bars, true, out _, out string reason));
        Assert.AreEqual("no untraded gap with its middle in the fib zone", reason);
    }

    [TestMethod]
    public void Long_GapOutsideTheFibZone_GivesNothing()
    {
        // A higher leg end (16) moves the zone to 10.438..11.5, above the gap middle 9.75
        var bars = LongCase();
        bars[10] = new ChochFvgBar(11m, 16m, 11m, 12.5m);
        Assert.IsFalse(Find(bars, true, out _, out string reason));
        Assert.AreEqual("no untraded gap with its middle in the fib zone", reason);
    }

    [TestMethod]
    public void Long_WithoutLowerHigh_IsNoDowntrend()
    {
        var bars = LongCase();
        bars[4] = new ChochFvgBar(10.5m, 13.5m, 10m, 11m);
        Assert.IsFalse(Find(bars, true, out _, out _));
    }

    [TestMethod]
    public void Long_StopUnderTheMinimum_IsDropped()
    {
        // The stop of the case is 0.75 / 9.75 = 7.69%: a minimum of 8% drops it, 7% keeps it
        Assert.IsFalse(ChochFvgRules.TryFindSetup(LongCase(), true, 1, 8, 50m, 61.8m, 4m, out ChochFvgSetup? setup, out string reason, 8m));
        Assert.IsNull(setup);
        StringAssert.Contains(reason, "under the minimum");
        Assert.IsTrue(ChochFvgRules.TryFindSetup(LongCase(), true, 1, 8, 50m, 61.8m, 4m, out _, out _, 7m));
    }

    [TestMethod]
    public void SessionWindow_FollowsNewYorkSummerAndWinterTime()
    {
        // Summer (EDT, UTC-4): 14:45 UTC is 10:45 in New York, 13:00 UTC is 09:00
        Assert.IsTrue(ChochFvgBase.InSessionWindow(new DateTime(2026, 7, 15, 14, 45, 0, DateTimeKind.Utc), "09:30", "11:30", out _));
        Assert.IsFalse(ChochFvgBase.InSessionWindow(new DateTime(2026, 7, 15, 13, 0, 0, DateTimeKind.Utc), "09:30", "11:30", out _));
        // Winter (EST, UTC-5): 14:45 UTC is 09:45, 16:30 UTC is 11:30 (the end is not inside)
        Assert.IsTrue(ChochFvgBase.InSessionWindow(new DateTime(2026, 1, 15, 14, 45, 0, DateTimeKind.Utc), "09:30", "11:30", out _));
        Assert.IsFalse(ChochFvgBase.InSessionWindow(new DateTime(2026, 1, 15, 16, 30, 0, DateTimeKind.Utc), "09:30", "11:30", out _));
        Assert.IsFalse(ChochFvgBase.InSessionWindow(new DateTime(2026, 1, 15, 14, 45, 0, DateTimeKind.Utc), "9.30", "11:30", out string reason));
        StringAssert.Contains(reason, "HH:mm");
    }
}
