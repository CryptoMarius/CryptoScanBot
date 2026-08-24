using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.CoreTests.Zones;

/// <summary>
/// The DLZ zones never had the realtime invalidation the FVG zones got when the touch/weakening
/// rules were introduced: they could only be invalidated inside a full recalculation, and that only
/// runs when price leaves the widened trigger range. What filled the hole was the signal class,
/// which closed a zone outright on its first touch - so TouchCount, ReachedMidpoint and MaxTouches never
/// applied to it. These tests pin down that a zone now survives its first test and closes on the
/// rule the settings describe.
/// </summary>
[TestClass]
public class ZoneDlzRealtimeInvalidationTests : TestBase
{
    private static readonly DateTime BaseTime = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);


    private static (CryptoSymbol symbol, CryptoInterval interval, CryptoZone zone, CryptoSymbolInterval symbolInterval)
        SetUpDemandZone(CryptoDatabase database)
    {
        // Closing a zone queues it for persistence, same as ZoneFvg's realtime path does.
        GlobalData.ThreadSaveObjects ??= new ThreadSaveObjects();

        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];
        CryptoSymbolInterval symbolInterval = symbol.Data.Get(interval.IntervalPeriod);
        // GlobalData is static and CreateTestSymbol hands back the same symbol for every test in the
        // run, so without this each test inherits the zones and candles of the previous one.
        symbolInterval.Dlz.Zones.Reset();
        symbolInterval.Dlz.ProcessedCandleMarker = null;
        symbolInterval.CandleList.Clear();

        // Demand (Long) zone: top=95, bottom=90
        CryptoZone zone = ZoneDlzTests.CreateTestZone(symbol, interval, CryptoTradeSide.Long, 95m, 90m, BaseTime);
        symbolInterval.Dlz.Zones.Add(zone);
        return (symbol, interval, zone, symbolInterval);
    }


    /// <summary>A wick into the zone is a test, not a break - that is the whole point of MaxTouches.</summary>
    [TestMethod]
    public void InvalidateRealtime_WickIntoZoneCountsAsTouchAndKeepsTheZoneOpen()
    {
        InitTestSession();
        ZoneDlzTests.ConfigureSettingsForTest();   // MaxTouches = 2

        using CryptoDatabase database = new();
        database.Open();
        var (symbol, interval, zone, symbolInterval) = SetUpDemandZone(database);

        // Low pierces the top (95) but the body closes well above the bottom (90)
        ZoneDlzTests.AddCandleToInterval(symbolInterval, interval, BaseTime.AddHours(1), 97m, 98m, 94m, 96m);
        ZoneDlz.InvalidateRealtime(symbol, interval, CandleTime.AlignFromDateTime(BaseTime.AddHours(2), interval.Duration));

        Assert.AreEqual(1, zone.TouchCount, "The wick has to be counted as a test of the zone");
        Assert.IsNull(zone.CloseTime, "One touch is not a break while MaxTouches is 2");
        Assert.AreEqual(1, symbolInterval.Dlz.Zones.LongOpen.Count, "The zone stays tradeable");
        Assert.AreEqual(0, symbolInterval.Dlz.Zones.LongClosed.Count);
    }


    /// <summary>The second test exhausts the zone, and it has to land in the CLOSED list.</summary>
    [TestMethod]
    public void InvalidateRealtime_SecondTouchReachesMaxTouchesAndMovesTheZoneToClosed()
    {
        InitTestSession();
        ZoneDlzTests.ConfigureSettingsForTest();   // MaxTouches = 2

        using CryptoDatabase database = new();
        database.Open();
        var (symbol, interval, zone, symbolInterval) = SetUpDemandZone(database);

        // Two VISITS, with a candle in between on which price leaves the zone again - since
        // 24-08-2026 the counting is per visit, so two candles of one visit are one touch.
        ZoneDlzTests.AddCandleToInterval(symbolInterval, interval, BaseTime.AddHours(1), 97m, 98m, 94m, 96m);
        ZoneDlz.InvalidateRealtime(symbol, interval, CandleTime.AlignFromDateTime(BaseTime.AddHours(2), interval.Duration));

        ZoneDlzTests.AddCandleToInterval(symbolInterval, interval, BaseTime.AddHours(2), 97m, 99m, 96m, 98m);
        ZoneDlz.InvalidateRealtime(symbol, interval, CandleTime.AlignFromDateTime(BaseTime.AddHours(3), interval.Duration));

        ZoneDlzTests.AddCandleToInterval(symbolInterval, interval, BaseTime.AddHours(3), 97m, 98m, 93m, 96m);
        ZoneDlz.InvalidateRealtime(symbol, interval, CandleTime.AlignFromDateTime(BaseTime.AddHours(4), interval.Duration));

        Assert.AreEqual(2, zone.TouchCount);
        Assert.IsNotNull(zone.CloseTime, "MaxTouches reached, so the zone is exhausted");
        Assert.AreEqual(0, symbolInterval.Dlz.Zones.LongOpen.Count);
        Assert.AreEqual(1, symbolInterval.Dlz.Zones.LongClosed.Count,
            "Moved to the closed list, not dropped - the charts draw that list too");
    }


    /// <summary>A body close through the floor is a real break, whatever the touch count says.</summary>
    [TestMethod]
    public void InvalidateRealtime_BodyCloseThroughTheFloorBreaksTheZoneOnTheFirstCandle()
    {
        InitTestSession();
        ZoneDlzTests.ConfigureSettingsForTest();

        using CryptoDatabase database = new();
        database.Open();
        var (symbol, interval, zone, symbolInterval) = SetUpDemandZone(database);

        // Closes below the bottom (90)
        ZoneDlzTests.AddCandleToInterval(symbolInterval, interval, BaseTime.AddHours(1), 92m, 93m, 88m, 89m);
        ZoneDlz.InvalidateRealtime(symbol, interval, CandleTime.AlignFromDateTime(BaseTime.AddHours(2), interval.Duration));

        Assert.IsNotNull(zone.CloseTime);
        Assert.AreEqual(0, symbolInterval.Dlz.Zones.LongOpen.Count);
        Assert.AreEqual(1, symbolInterval.Dlz.Zones.LongClosed.Count);
    }


    /// <summary>
    /// The cursor has to follow, otherwise the incremental broken-check walks the same candle again
    /// and counts the same wick a second time - ApplyToCandle does not remember what it has seen.
    /// </summary>
    [TestMethod]
    public void InvalidateRealtime_AdvancesTheProcessedCandleMarkerButOnlyWhenItWasSet()
    {
        InitTestSession();
        ZoneDlzTests.ConfigureSettingsForTest();

        using CryptoDatabase database = new();
        database.Open();
        var (symbol, interval, _, symbolInterval) = SetUpDemandZone(database);

        CandleTime candleOpenTime = CandleTime.AlignFromDateTime(BaseTime.AddHours(1), interval.Duration);
        ZoneDlzTests.AddCandleToInterval(symbolInterval, interval, BaseTime.AddHours(1), 97m, 98m, 94m, 96m);

        // Null means the first full scan still has to happen; it must not be told to skip this candle.
        symbolInterval.Dlz.ProcessedCandleMarker = null;
        ZoneDlz.InvalidateRealtime(symbol, interval, CandleTime.AlignFromDateTime(BaseTime.AddHours(2), interval.Duration));
        Assert.IsNull(symbolInterval.Dlz.ProcessedCandleMarker, "A null marker stays null");

        symbolInterval.Dlz.ProcessedCandleMarker = CandleTime.AlignFromDateTime(BaseTime, interval.Duration);
        ZoneDlz.InvalidateRealtime(symbol, interval, CandleTime.AlignFromDateTime(BaseTime.AddHours(2), interval.Duration));
        Assert.AreEqual(candleOpenTime, symbolInterval.Dlz.ProcessedCandleMarker,
            "A marker that was set follows the candle just applied");
    }
}
