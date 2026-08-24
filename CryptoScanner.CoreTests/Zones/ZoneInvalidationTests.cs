using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.CoreTests.Zones;

/// <summary>
/// The one rule set every zone kind runs on. Four questions per candle - broken, entered, left, used
/// up - and one setting that differs per kind: how far price has to come in before it counts.
/// <para>
/// These tests state the rule in the smallest possible terms: one zone and a handful of candles,
/// no zone engine around it. Everything that used to be a second implementation should be provable
/// here and nowhere else.
/// </para>
/// </summary>
[TestClass]
public class ZoneInvalidationTests : TestBase
{
    private static readonly DateTime BaseTime = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static CandleTime Start => CandleTime.AlignFromDateTime(BaseTime, 60);

    private static CryptoInterval Hour => GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

    private static ZoneInvalidation.ZoneTouchRules Rules(int maxTouches, CryptoZoneTouchLevel level,
        bool closeAtMidpoint = false)
        => new(maxTouches, level, closeAtMidpoint);

    /// <summary>A zone needs a symbol and an exchange, so a session and a database it is.</summary>
    private static CryptoDatabase NewDatabase()
    {
        InitTestSession();
        CryptoDatabase database = new();
        database.Open();
        return database;
    }

    /// <summary>Demand zone 90..95, so the midpoint sits at 92.5.</summary>
    private static CryptoZone Demand(CryptoSymbol symbol)
        => ZoneDlzTests.CreateTestZone(symbol, Hour, CryptoTradeSide.Long, 95m, 90m, BaseTime);

    /// <summary>Supply zone 90..95, same band, mirrored.</summary>
    private static CryptoZone Supply(CryptoSymbol symbol)
        => ZoneDlzTests.CreateTestZone(symbol, Hour, CryptoTradeSide.Short, 95m, 90m, BaseTime);

    private static CryptoCandle Candle(int hoursAfterStart, decimal open, decimal high, decimal low, decimal close)
        => new()
        {
            OpenTime = Start + (uint)(hoursAfterStart * 60),
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = 1000m,
        };


    // ────────────────────────────────────────────────────────────────────────
    // 1. Broken — a body close through the far side
    // ────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Broken_DemandZone_ClosesOnBodyThroughTheFloor()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        bool closed = ZoneInvalidation.ApplyToCandle(zone, Candle(1, 94m, 96m, 88m, 89m), Hour,
            Rules(2, CryptoZoneTouchLevel.Edge));

        Assert.IsTrue(closed);
        Assert.AreEqual(Start + 120u, zone.CloseTime, "Stamped at the END of the candle that broke it");
    }


    [TestMethod]
    public void Broken_SupplyZone_ClosesOnBodyThroughTheCeiling()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Supply(CreateTestSymbol(database));
        bool closed = ZoneInvalidation.ApplyToCandle(zone, Candle(1, 91m, 97m, 90m, 96m), Hour,
            Rules(2, CryptoZoneTouchLevel.Midpoint));

        Assert.IsTrue(closed);
        Assert.IsNotNull(zone.CloseTime);
    }


    [TestMethod]
    public void Broken_WickThroughTheFarSideIsNotABreak()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        // Low 85 is far below the floor, but the body closes back inside the zone.
        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 94m, 96m, 85m, 93m), Hour,
            Rules(0, CryptoZoneTouchLevel.Edge));

        Assert.IsNull(zone.CloseTime, "A wick through is a test, only a body close is a break");
        Assert.AreEqual(1, zone.TouchCount);
    }


    [TestMethod]
    public void Broken_AlreadyClosedZoneIsLeftAlone()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        zone.CloseTime = Start + 60u;
        int touchesBefore = zone.TouchCount;

        bool closed = ZoneInvalidation.ApplyToCandle(zone, Candle(2, 94m, 96m, 91m, 93m), Hour,
            Rules(2, CryptoZoneTouchLevel.Edge));

        Assert.IsTrue(closed);
        Assert.AreEqual(touchesBefore, zone.TouchCount, "A closed zone does not keep collecting visits");
    }


    // ────────────────────────────────────────────────────────────────────────
    // 2 + 3. One visit counts once, however long it lasts
    // ────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Visit_ThreeCandlesInsideTheZoneCountAsOneVisit()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        var rules = Rules(0, CryptoZoneTouchLevel.Edge);   // never used up, so we can keep counting

        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 94m, 96m), Hour, rules);
        ZoneInvalidation.ApplyToCandle(zone, Candle(2, 96m, 96m, 93m, 95m), Hour, rules);
        ZoneInvalidation.ApplyToCandle(zone, Candle(3, 95m, 96m, 94m, 96m), Hour, rules);

        Assert.AreEqual(1, zone.TouchCount, "This is the whole point: one visit, not three candles");
        Assert.IsTrue(zone.VisitCounted);
    }


    [TestMethod]
    public void Visit_LeavingAndComingBackIsASecondVisit()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        var rules = Rules(0, CryptoZoneTouchLevel.Edge);

        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 94m, 96m), Hour, rules);  // in
        Assert.AreEqual(1, zone.TouchCount);

        ZoneInvalidation.ApplyToCandle(zone, Candle(2, 97m, 99m, 96m, 98m), Hour, rules);  // out
        Assert.AreEqual(1, zone.TouchCount, "Leaving is not a visit");
        // No assertion on VisitCounted here on purpose: the end of a visit is worked out from when
        // price was last seen inside, not flipped by the exit candle - see CryptoZone.LastInsideCandle.
        // The flag is cleared on the next candle that asks, which is what the count below proves.

        ZoneInvalidation.ApplyToCandle(zone, Candle(3, 98m, 98m, 94m, 97m), Hour, rules);  // in again
        Assert.AreEqual(2, zone.TouchCount);
    }


    [TestMethod]
    public void Visit_SupplyZoneMirrorsTheDemandZone()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Supply(CreateTestSymbol(database));
        var rules = Rules(0, CryptoZoneTouchLevel.Edge);

        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 88m, 91m, 87m, 89m), Hour, rules);  // in
        ZoneInvalidation.ApplyToCandle(zone, Candle(2, 89m, 92m, 88m, 89m), Hour, rules);  // still in
        Assert.AreEqual(1, zone.TouchCount);

        ZoneInvalidation.ApplyToCandle(zone, Candle(3, 88m, 89m, 85m, 86m), Hour, rules);  // out
        ZoneInvalidation.ApplyToCandle(zone, Candle(4, 86m, 91m, 86m, 88m), Hour, rules);  // in again
        Assert.AreEqual(2, zone.TouchCount);
    }


    /// <summary>
    /// With the midpoint as touch level, a pull-back that stays inside the edge is still the same
    /// visit - otherwise one long test would be counted as several.
    /// </summary>
    [TestMethod]
    public void Visit_MidpointLevel_PullBackInsideTheEdgeIsStillTheSameVisit()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        var rules = Rules(0, CryptoZoneTouchLevel.Midpoint);

        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 92m, 94m), Hour, rules);   // to 92, past 92.5
        Assert.AreEqual(1, zone.TouchCount);

        ZoneInvalidation.ApplyToCandle(zone, Candle(2, 94m, 95m, 94m, 94m), Hour, rules);   // back to 94, still inside
        ZoneInvalidation.ApplyToCandle(zone, Candle(3, 94m, 95m, 92m, 93m), Hour, rules);   // down to 92 again
        Assert.AreEqual(1, zone.TouchCount, "Price never left the zone, so this is one visit");
    }


    // ────────────────────────────────────────────────────────────────────────
    // The touch level: the only difference between the zone kinds
    // ────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void TouchLevel_Edge_CountsAWickThatOnlyClipsTheTop()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        // Low 94.5 is inside the zone but well above the midpoint of 92.5.
        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 94.5m, 96m), Hour,
            Rules(0, CryptoZoneTouchLevel.Edge));

        Assert.AreEqual(1, zone.TouchCount);
        Assert.IsFalse(zone.ReachedMidpoint, "It never got halfway in");
    }


    [TestMethod]
    public void TouchLevel_Midpoint_DoesNotCountAWickThatOnlyClipsTheTop()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 94.5m, 96m), Hour,
            Rules(0, CryptoZoneTouchLevel.Midpoint));

        Assert.AreEqual(0, zone.TouchCount, "Not deep enough to be a test on this setting");
        Assert.IsFalse(zone.VisitCounted);
    }


    [TestMethod]
    public void TouchLevel_Midpoint_CountsOnceReachedHalfway()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 92m, 94m), Hour,
            Rules(0, CryptoZoneTouchLevel.Midpoint));

        Assert.AreEqual(1, zone.TouchCount);
        Assert.IsTrue(zone.ReachedMidpoint);
    }


    [TestMethod]
    public void ReachedMidpoint_IsSetEvenWhenTheTouchLevelIsTheEdge()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        var rules = Rules(0, CryptoZoneTouchLevel.Edge);

        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 94m, 96m), Hour, rules);
        Assert.IsFalse(zone.ReachedMidpoint, "Only into the zone, not halfway");

        ZoneInvalidation.ApplyToCandle(zone, Candle(2, 96m, 96m, 91m, 95m), Hour, rules);
        Assert.IsTrue(zone.ReachedMidpoint, "How deep price came is tracked regardless of the setting");
        Assert.AreEqual(1, zone.TouchCount, "...and it is not a second visit");
    }


    // ────────────────────────────────────────────────────────────────────────
    // 4. Used up
    // ────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void UsedUp_ClosesOnTheVisitThatReachesMaxTouches()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        var rules = Rules(2, CryptoZoneTouchLevel.Edge);

        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 94m, 96m), Hour, rules);  // visit 1
        Assert.IsNull(zone.CloseTime, "One visit of two allowed");

        ZoneInvalidation.ApplyToCandle(zone, Candle(2, 97m, 99m, 96m, 98m), Hour, rules);  // out
        ZoneInvalidation.ApplyToCandle(zone, Candle(3, 98m, 98m, 94m, 97m), Hour, rules);  // visit 2

        Assert.AreEqual(2, zone.TouchCount);
        Assert.IsNotNull(zone.CloseTime, "Used up");
    }


    [TestMethod]
    public void UsedUp_MaxTouchesZeroMeansNeverUsedUp()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        var rules = Rules(0, CryptoZoneTouchLevel.Edge);

        for (int i = 1; i <= 9; i += 2)
        {
            ZoneInvalidation.ApplyToCandle(zone, Candle(i, 96m, 97m, 94m, 96m), Hour, rules);      // in
            ZoneInvalidation.ApplyToCandle(zone, Candle(i + 1, 97m, 99m, 96m, 98m), Hour, rules);  // out
        }

        Assert.AreEqual(5, zone.TouchCount);
        Assert.IsNull(zone.CloseTime, "0 means only a break can close it");
    }


    [TestMethod]
    public void UsedUp_MaxTouchesOneClosesOnTheFirstVisit()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 94m, 96m), Hour,
            Rules(1, CryptoZoneTouchLevel.Edge));

        Assert.AreEqual(1, zone.TouchCount);
        Assert.IsNotNull(zone.CloseTime);
    }


    // ────────────────────────────────────────────────────────────────────────
    // Bounds
    // ────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Bounds_CandlesBeforeTheZoneAreIgnored()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        zone.OpenTime = Start + 300u;

        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 88m, 89m), Hour,
            Rules(2, CryptoZoneTouchLevel.Edge));

        Assert.IsNull(zone.CloseTime, "A zone cannot be broken before it existed");
        Assert.AreEqual(0, zone.TouchCount);
    }


    [TestMethod]
    public void Bounds_TouchCountingFromKeepsTheOwnCandlesOut()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Supply(CreateTestSymbol(database));
        // The order blocks set this to their impulse candle so the base and the impulse's own wick
        // do not count as a test of the level they created.
        zone.TouchCountingFrom = Start + 180u;

        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 88m, 94m, 87m, 89m), Hour,
            Rules(2, CryptoZoneTouchLevel.Midpoint));
        Assert.AreEqual(0, zone.TouchCount, "Before the counting start");

        ZoneInvalidation.ApplyToCandle(zone, Candle(4, 88m, 94m, 87m, 89m), Hour,
            Rules(2, CryptoZoneTouchLevel.Midpoint));
        Assert.AreEqual(1, zone.TouchCount, "After it");
    }


    [TestMethod]
    public void Bounds_ExactlyOnTheEdgeCounts()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        // Low exactly on the top of the zone.
        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 95m, 96m), Hour,
            Rules(0, CryptoZoneTouchLevel.Edge));

        Assert.AreEqual(1, zone.TouchCount, "Touching the edge is arriving at the level");
    }


    [TestMethod]
    public void Bounds_ExactlyOnTheMidpointCounts()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 92.5m, 94m), Hour,
            Rules(0, CryptoZoneTouchLevel.Midpoint));

        Assert.AreEqual(1, zone.TouchCount);
        Assert.IsTrue(zone.ReachedMidpoint);
    }


    /// <summary>
    /// A break beats everything: it is checked before the visit bookkeeping, so a candle that dips in
    /// AND closes through does not first spend a visit.
    /// </summary>
    [TestMethod]
    public void Bounds_ACandleThatEntersAndBreaksIsOnlyABreak()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 96m, 88m, 89m), Hour,
            Rules(2, CryptoZoneTouchLevel.Edge));

        Assert.IsNotNull(zone.CloseTime);
        Assert.AreEqual(0, zone.TouchCount, "It broke, it did not test");
    }

    // ────────────────────────────────────────────────────────────────────────
    // 5. Halfway in — the optional rule
    // ────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Halfway_OffByDefault_AMidpointTouchOnlyCountsAsAVisit()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 91m, 94m), Hour,
            Rules(0, CryptoZoneTouchLevel.Edge));

        Assert.IsTrue(zone.ReachedMidpoint);
        Assert.IsNull(zone.CloseTime, "Without the setting the zone survives being eaten into");
    }


    [TestMethod]
    public void Halfway_ClosesTheZoneOnTheFirstVisitThatReachesTheMiddle()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        bool closed = ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 91m, 94m), Hour,
            Rules(0, CryptoZoneTouchLevel.Edge, closeAtMidpoint: true));

        Assert.IsTrue(closed);
        Assert.AreEqual(1, zone.TouchCount, "It is still a visit, it is just the last one");
        Assert.IsNotNull(zone.CloseTime);
    }


    [TestMethod]
    public void Halfway_AWickThatStopsShortOfTheMiddleLeavesTheZoneAlone()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        // Low 94 is inside the zone, midpoint is 92.5.
        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 94m, 96m), Hour,
            Rules(0, CryptoZoneTouchLevel.Edge, closeAtMidpoint: true));

        Assert.IsFalse(zone.ReachedMidpoint);
        Assert.IsNull(zone.CloseTime);
    }


    [TestMethod]
    public void Halfway_AlsoFiresOnACandleThatOnlyContinuesAVisit()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Demand(CreateTestSymbol(database));
        var rules = Rules(0, CryptoZoneTouchLevel.Edge, closeAtMidpoint: true);

        ZoneInvalidation.ApplyToCandle(zone, Candle(1, 96m, 97m, 94m, 96m), Hour, rules);   // in, not halfway
        Assert.IsNull(zone.CloseTime);

        ZoneInvalidation.ApplyToCandle(zone, Candle(2, 96m, 96m, 91m, 95m), Hour, rules);   // deeper, same visit
        Assert.IsNotNull(zone.CloseTime, "How deep price came does not wait for a new visit");
        Assert.AreEqual(1, zone.TouchCount);
    }


    [TestMethod]
    public void Halfway_SupplyZoneMirrorsIt()
    {
        using CryptoDatabase database = NewDatabase();
        var zone = Supply(CreateTestSymbol(database));
        bool closed = ZoneInvalidation.ApplyToCandle(zone, Candle(1, 88m, 94m, 87m, 89m), Hour,
            Rules(0, CryptoZoneTouchLevel.Edge, closeAtMidpoint: true));

        Assert.IsTrue(closed);
        Assert.IsTrue(zone.ReachedMidpoint);
    }
}
