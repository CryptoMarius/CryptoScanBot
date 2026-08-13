using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.CoreTests.Zones;

/// <summary>
/// Tests for DLZ (Dominant Liquidity Zone) calculation using fabricated zigzag data.
/// The fabricated pivot scenario and helper methods are designed to be reusable for
/// FVG and SMC zone tests later.
/// </summary>
[TestClass]
public class ZoneDlzTests : TestBase
{
    private static readonly DateTime BaseTime = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // Pivot definitions: (Type, Value, Open, High, Low, Close, HoursOffset)
    //
    // The scenario creates a zigzag: H100 -> L90 -> H110 -> L85 -> H105 -> L80 -> H115
    //
    // Expected dominant pivots after CalculateDlzAsync:
    //   #1 (L90)  - triplet (H100, L90, H110): H110 > H100  -> demand zone
    //   #2 (H110) - triplet (L90, H110, L85):  L85  < L90   -> supply zone
    //   #4 (H105) - triplet (L85, H105, L80):  L80  < L85   -> supply zone
    //   #5 (L80)  - triplet (H105, L80, H115): H115 > H105  -> demand zone
    //
    // Non-dominant:
    //   #3 (L85)  - triplet (H110, L85, H105): H105 < H110, not a higher high
    internal static readonly (char Type, decimal Value, decimal Open, decimal High, decimal Low, decimal Close, int Hours)[] Pivots =
    [
        ('H', 100m,  98m, 100m,  97m,  99m,  0),  // #0
        ('L',  90m,  95m,  96m,  90m,  93m,  2),  // #1 -> demand
        ('H', 110m, 105m, 110m, 104m, 108m,  4),  // #2 -> supply
        ('L',  85m,  88m,  89m,  85m,  87m,  6),  // #3 -> NOT dominant
        ('H', 105m, 100m, 105m,  99m, 103m,  8),  // #4 -> supply
        ('L',  80m,  83m,  84m,  80m,  82m, 10),  // #5 -> demand
        ('H', 115m, 112m, 115m, 111m, 114m, 12),  // #6 (trigger for #5)
    ];

    internal static void ConfigureSettingsForTest()
    {
        var dlzSettings = GlobalData.Settings.Signal.ZonesDlz;
        dlzSettings.ZoomLowerTimeFrames = false;
        dlzSettings.MinimumZoomedPercentage = 0;
        dlzSettings.MaximumZoomedPercentage = 0;
        dlzSettings.ZonesApplyUnzoomed = false;
        dlzSettings.ZoneStartApply = false;
        dlzSettings.MaxTouches = 2;
    }

    internal static CryptoCandle AddCandleToInterval(CryptoSymbolInterval symbolInterval,
        CryptoInterval interval, DateTime time, decimal open, decimal high, decimal low, decimal close)
    {
        CryptoCandle candle = new()
        {
            OpenTime = CandleTime.AlignFromDateTime(time, interval.Duration),
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = 1000m,
        };
        symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);
        return candle;
    }

    internal static (CryptoSymbol symbol, CryptoInterval interval, ZigZagIndicator indicator)
        BuildPivotTestData(int pivotCount = -1)
    {
        InitTestSession();
        ConfigureSettingsForTest();

        using CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        int count = pivotCount < 0 ? Pivots.Length : pivotCount;
        ZigZagIndicator indicator = new(TrendType.Primary, false);

        for (int i = 0; i < count; i++)
        {
            var p = Pivots[i];
            DateTime time = BaseTime.AddHours(p.Hours);

            CryptoCandle candle = AddCandleToInterval(symbolInterval, interval, time,
                p.Open, p.High, p.Low, p.Close);

            ZigZagResult pivot = new()
            {
                PointType = p.Type,
                Value = (double)p.Value,
                Candle = candle,
            };
            indicator.ZigZagList.Add(pivot);
        }

        return (symbol, interval, indicator);
    }

    internal static CryptoZone CreateTestZone(CryptoSymbol symbol, CryptoInterval interval,
        CryptoTradeSide side, decimal top, decimal bottom, DateTime openTime)
    {
        return new CryptoZone
        {
            Kind = CryptoZoneKind.DominantLevel,
            Strength = CryptoZoneStrength.Strong,
            ExchangeId = symbol.Exchange.Id,
            Exchange = symbol.Exchange,
            SymbolId = symbol.Id,
            Symbol = symbol,
            IntervalId = interval.Id,
            Interval = interval,
            OpenTime = CandleTime.AlignFromDateTime(openTime, interval.Duration),
            Top = top,
            Bottom = bottom,
            Side = side,
            IsValid = true,
        };
    }

    // ────────────────────────────────────────────────────────────────────────
    // Full calculation: dominant pivot detection
    // ────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task FullCalculation_MarksDominantPivots()
    {
        var (symbol, interval, indicator) = BuildPivotTestData();
        SortedList<CryptoIntervalPeriod, bool> loaded = [];

        await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator, loaded);

        Assert.IsTrue(indicator.ZigZagList[1].Dominant, "Pivot #1 (L90) should be dominant demand");
        Assert.IsTrue(indicator.ZigZagList[1].IsValid, "Pivot #1 (L90) should be valid");

        Assert.IsTrue(indicator.ZigZagList[2].Dominant, "Pivot #2 (H110) should be dominant supply");
        Assert.IsTrue(indicator.ZigZagList[2].IsValid, "Pivot #2 (H110) should be valid");

        Assert.IsFalse(indicator.ZigZagList[3].Dominant, "Pivot #3 (L85) should NOT be dominant");

        Assert.IsTrue(indicator.ZigZagList[4].Dominant, "Pivot #4 (H105) should be dominant supply");
        Assert.IsTrue(indicator.ZigZagList[4].IsValid, "Pivot #4 (H105) should be valid");

        Assert.IsTrue(indicator.ZigZagList[5].Dominant, "Pivot #5 (L80) should be dominant demand");
        Assert.IsTrue(indicator.ZigZagList[5].IsValid, "Pivot #5 (L80) should be valid");
    }

    [TestMethod]
    public async Task FullCalculation_DemandZoneBoundaries()
    {
        var (symbol, interval, indicator) = BuildPivotTestData();
        SortedList<CryptoIntervalPeriod, bool> loaded = [];

        await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator, loaded);

        // Pivot #1 (L90): demand zone, MakeDominant passes top=Max(Open,Close), bottom=Low
        // Candle: O=95 H=96 L=90 C=93 -> top=Max(95,93)=95, bottom=90
        var pivot = indicator.ZigZagList[1];
        Assert.AreEqual(95m, pivot.Top, "Demand zone top = body top (Max(Open,Close))");
        Assert.AreEqual(90m, pivot.Bottom, "Demand zone bottom = wick low");
        Assert.IsTrue(pivot.Percentage > 0, "Percentage should be positive");
    }

    [TestMethod]
    public async Task FullCalculation_SupplyZoneBoundaries()
    {
        var (symbol, interval, indicator) = BuildPivotTestData();
        SortedList<CryptoIntervalPeriod, bool> loaded = [];

        await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator, loaded);

        // Pivot #2 (H110): supply zone, MakeDominant passes top=High, bottom=Min(Open,Close)
        // Candle: O=105 H=110 L=104 C=108 -> top=110, bottom=Min(105,108)=105
        var pivot = indicator.ZigZagList[2];
        Assert.AreEqual(110m, pivot.Top, "Supply zone top = wick high");
        Assert.AreEqual(105m, pivot.Bottom, "Supply zone bottom = body bottom (Min(Open,Close))");
        Assert.IsTrue(pivot.Percentage > 0, "Percentage should be positive");
    }

    [TestMethod]
    public async Task FullCalculation_NonDominantPivotStaysUnmarked()
    {
        var (symbol, interval, indicator) = BuildPivotTestData();
        SortedList<CryptoIntervalPeriod, bool> loaded = [];

        await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator, loaded);

        // Pivot #3 (L85): triplet (H110, L85, H105) - H105 < H110, not a higher high
        var pivot = indicator.ZigZagList[3];
        Assert.IsFalse(pivot.Dominant);
        Assert.AreEqual(0m, pivot.Top, "Non-dominant pivot should have no zone boundaries");
        Assert.AreEqual(0m, pivot.Bottom);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Zone creation from dominant pivots
    // ────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task CreateZonesFromZigZag_ProducesCorrectZones()
    {
        var (symbol, interval, indicator) = BuildPivotTestData();
        SortedList<CryptoIntervalPeriod, bool> loaded = [];

        await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator, loaded);

        List<CryptoZone> zones = [];
        ZoneDlz.CreateZonesFromZigZag(symbol, interval, indicator.ZigZagList, zones);

        Assert.AreEqual(4, zones.Count, "Should produce 4 zones from 4 dominant pivots");

        int longCount = zones.Count(z => z.Side == CryptoTradeSide.Long);
        int shortCount = zones.Count(z => z.Side == CryptoTradeSide.Short);
        Assert.AreEqual(2, longCount, "2 demand (Long) zones: L90 and L80");
        Assert.AreEqual(2, shortCount, "2 supply (Short) zones: H110 and H105");

        Assert.IsTrue(zones.All(z => z.Kind == CryptoZoneKind.DominantLevel));
    }

    [TestMethod]
    public async Task CreateZonesFromZigZag_ZoneBoundariesMatchPivots()
    {
        var (symbol, interval, indicator) = BuildPivotTestData();
        SortedList<CryptoIntervalPeriod, bool> loaded = [];

        await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator, loaded);

        List<CryptoZone> zones = [];
        ZoneDlz.CreateZonesFromZigZag(symbol, interval, indicator.ZigZagList, zones);

        // Find the demand zone from pivot #1 (L90)
        var demandZone = zones.First(z => z.Side == CryptoTradeSide.Long && z.Bottom == 90m);
        Assert.AreEqual(95m, demandZone.Top);
        Assert.AreEqual(90m, demandZone.Bottom);

        // Find the supply zone from pivot #2 (H110)
        var supplyZone = zones.First(z => z.Side == CryptoTradeSide.Short && z.Top == 110m);
        Assert.AreEqual(110m, supplyZone.Top);
        Assert.AreEqual(105m, supplyZone.Bottom);
    }

    [TestMethod]
    public async Task CreateZonesFromZigZag_AfterTimeFiltersOldPivots()
    {
        var (symbol, interval, indicator) = BuildPivotTestData();
        SortedList<CryptoIntervalPeriod, bool> loaded = [];

        await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator, loaded);

        // Filter: only zones after pivot #3's time (hour 6)
        CandleTime cutoff = CandleTime.AlignFromDateTime(BaseTime.AddHours(6), interval.Duration);
        List<CryptoZone> zones = [];
        ZoneDlz.CreateZonesFromZigZag(symbol, interval, indicator.ZigZagList, zones, afterTime: cutoff);

        // Only pivots #4 (H105 at h8) and #5 (L80 at h10) should pass the filter
        Assert.AreEqual(2, zones.Count, "Should only produce zones after cutoff");
        Assert.IsTrue(zones.All(z => z.OpenTime > cutoff), "All zones should be after cutoff");
    }

    // ────────────────────────────────────────────────────────────────────────
    // Incremental calculation
    // ────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public async Task IncrementalCalculation_ProcessesOnlyNewPivots()
    {
        // Phase 1: full calculation with pivots 0-4 (H100, L90, H110, L85, H105)
        var (symbol, interval, indicator) = BuildPivotTestData(pivotCount: 5);
        SortedList<CryptoIntervalPeriod, bool> loaded = [];

        await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator, loaded);

        Assert.IsTrue(indicator.ZigZagList[1].Dominant, "Phase 1: L90 dominant");
        Assert.IsTrue(indicator.ZigZagList[2].Dominant, "Phase 1: H110 dominant");
        Assert.IsFalse(indicator.ZigZagList[3].Dominant, "Phase 1: L85 not dominant");

        // Cursor = time of last pivot processed
        CandleTime cursor = indicator.ZigZagList[4].Candle.OpenTime;

        // Phase 2: add pivots #5 (L80) and #6 (H115)
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        for (int i = 5; i < Pivots.Length; i++)
        {
            var p = Pivots[i];
            DateTime time = BaseTime.AddHours(p.Hours);
            CryptoCandle candle = AddCandleToInterval(symbolInterval, interval, time,
                p.Open, p.High, p.Low, p.Close);
            indicator.ZigZagList.Add(new ZigZagResult
            {
                PointType = p.Type,
                Value = (double)p.Value,
                Candle = candle,
            });
        }

        await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator, loaded, processAfter: cursor);

        // Previous results unchanged
        Assert.IsTrue(indicator.ZigZagList[1].Dominant, "Phase 2: L90 still dominant");
        Assert.IsTrue(indicator.ZigZagList[2].Dominant, "Phase 2: H110 still dominant");
        Assert.IsFalse(indicator.ZigZagList[3].Dominant, "Phase 2: L85 still not dominant");

        // H105 at cursor boundary is re-evaluated as supply via (L85, H105, L80)
        Assert.IsTrue(indicator.ZigZagList[4].Dominant, "Phase 2: H105 should be dominant supply");

        // L80 becomes dominant demand via (H105, L80, H115)
        Assert.IsTrue(indicator.ZigZagList[5].Dominant, "Phase 2: L80 should be dominant demand");
        Assert.IsTrue(indicator.ZigZagList[5].IsValid, "Phase 2: L80 should be valid");
    }

    // Verifies that a pivot exactly at the cursor boundary IS re-evaluated when new
    // context arrives (the skip uses strict < so pivots at cursor time are re-processed).
    [TestMethod]
    public async Task IncrementalCalculation_PivotAtCursorBoundary_IsReevaluated()
    {
        // Phase 1: 3 pivots — only L90 becomes dominant (H110 is just a trigger)
        var (symbol, interval, indicator) = BuildPivotTestData(pivotCount: 3);
        SortedList<CryptoIntervalPeriod, bool> loaded = [];
        await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator, loaded);
        Assert.IsFalse(indicator.ZigZagList[2].Dominant, "Phase 1: H110 not yet dominant (no L after it)");

        CandleTime cursor = indicator.ZigZagList[2].Candle.OpenTime;

        // Phase 2: add L85 which makes the triplet (L90, H110, L85) -> H110 supply
        var p = Pivots[3];
        DateTime time = BaseTime.AddHours(p.Hours);
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);
        CryptoCandle candle = AddCandleToInterval(symbolInterval, interval, time,
            p.Open, p.High, p.Low, p.Close);
        indicator.ZigZagList.Add(new ZigZagResult
        {
            PointType = p.Type,
            Value = (double)p.Value,
            Candle = candle,
        });

        await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator, loaded, processAfter: cursor);

        // With strict < check, H110 at cursor time IS re-evaluated as supply
        Assert.IsTrue(indicator.ZigZagList[2].Dominant,
            "Pivot at cursor boundary should be re-evaluated with new context");
        Assert.IsTrue(indicator.ZigZagList[2].IsValid);
    }

    // ────────────────────────────────────────────────────────────────────────
    // Broken zone detection (CheckAndMarkBrokenZones)
    // ────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void CheckAndMarkBrokenZones_DemandZone_BodyBreakClosesZone()
    {
        InitTestSession();
        ConfigureSettingsForTest();

        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        // Demand (Long) zone: top=95, bottom=90
        var zone = CreateTestZone(symbol, interval, CryptoTradeSide.Long, 95m, 90m, BaseTime);

        CryptoSymbolIntervalZones zones = new();
        zones.Add(zone);
        Assert.AreEqual(1, zones.LongOpen.Count);

        // Candle that closes below zone bottom (close=89 < bottom=90)
        CryptoCandleList candles = [];
        CandleTime breakTime = zone.OpenTime + interval.Duration;
        candles.TryAdd(breakTime, new CryptoCandle
        {
            OpenTime = breakTime,
            Open = 92m,
            High = 93m,
            Low = 88m,
            Close = 89m,
            Volume = 1000m,
        });

        ZoneDlz.CheckAndMarkBrokenZones(interval, candles, zones);

        Assert.AreEqual(0, zones.LongOpen.Count, "Zone should be removed from open list");
        Assert.AreEqual(1, zones.LongClosed.Count, "Zone should be in closed list");
        Assert.IsNotNull(zone.CloseTime, "CloseTime should be set");
    }

    [TestMethod]
    public void CheckAndMarkBrokenZones_SupplyZone_BodyBreakClosesZone()
    {
        InitTestSession();
        ConfigureSettingsForTest();

        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        // Supply (Short) zone: top=110, bottom=105
        var zone = CreateTestZone(symbol, interval, CryptoTradeSide.Short, 110m, 105m, BaseTime);

        CryptoSymbolIntervalZones zones = new();
        zones.Add(zone);
        Assert.AreEqual(1, zones.ShortOpen.Count);

        // Candle that closes above zone top (close=111 > top=110)
        CryptoCandleList candles = [];
        CandleTime breakTime = zone.OpenTime + interval.Duration;
        candles.TryAdd(breakTime, new CryptoCandle
        {
            OpenTime = breakTime,
            Open = 108m,
            High = 112m,
            Low = 107m,
            Close = 111m,
            Volume = 1000m,
        });

        ZoneDlz.CheckAndMarkBrokenZones(interval, candles, zones);

        Assert.AreEqual(0, zones.ShortOpen.Count, "Supply zone should be broken");
        Assert.AreEqual(1, zones.ShortClosed.Count, "Supply zone should be in closed list");
        Assert.IsNotNull(zone.CloseTime);
    }

    [TestMethod]
    public void CheckAndMarkBrokenZones_WickTouchIncrementsTouchCount()
    {
        InitTestSession();
        ConfigureSettingsForTest();

        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        // Demand (Long) zone: top=95, bottom=90
        var zone = CreateTestZone(symbol, interval, CryptoTradeSide.Long, 95m, 90m, BaseTime);

        CryptoSymbolIntervalZones zones = new();
        zones.Add(zone);

        // Candle wicks into zone (low=93 < top=95) but body stays above bottom (close=97 > bottom=90)
        CryptoCandleList candles = [];
        CandleTime touchTime = zone.OpenTime + interval.Duration;
        candles.TryAdd(touchTime, new CryptoCandle
        {
            OpenTime = touchTime,
            Open = 96m,
            High = 98m,
            Low = 93m,
            Close = 97m,
            Volume = 1000m,
        });

        ZoneDlz.CheckAndMarkBrokenZones(interval, candles, zones);

        Assert.AreEqual(1, zones.LongOpen.Count, "Zone should remain open after wick touch");
        Assert.AreEqual(1, zone.TouchCount, "Touch count should be 1");
        Assert.IsNull(zone.CloseTime, "CloseTime should remain null");
    }

    [TestMethod]
    public void CheckAndMarkBrokenZones_MaxTouchesExhaustsZone()
    {
        InitTestSession();
        ConfigureSettingsForTest();
        GlobalData.Settings.Signal.ZonesDlz.MaxTouches = 2;

        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        var zone = CreateTestZone(symbol, interval, CryptoTradeSide.Long, 95m, 90m, BaseTime);

        CryptoSymbolIntervalZones zones = new();
        zones.Add(zone);

        // Two consecutive wick touches exhaust the zone
        CryptoCandleList candles = [];
        for (uint i = 1; i <= 2; i++)
        {
            CandleTime time = zone.OpenTime + i * interval.Duration;
            candles.TryAdd(time, new CryptoCandle
            {
                OpenTime = time,
                Open = 96m,
                High = 98m,
                Low = 93m,
                Close = 97m,
                Volume = 1000m,
            });
        }

        ZoneDlz.CheckAndMarkBrokenZones(interval, candles, zones);

        Assert.AreEqual(0, zones.LongOpen.Count, "Zone should be closed after max touches");
        Assert.AreEqual(1, zones.LongClosed.Count, "Zone should be in closed list");
        Assert.AreEqual(2, zone.TouchCount, "Touch count should equal MaxTouches");
    }

    [TestMethod]
    public void CheckAndMarkBrokenZones_CandleBeforeZoneIsIgnored()
    {
        InitTestSession();
        ConfigureSettingsForTest();

        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        var zone = CreateTestZone(symbol, interval, CryptoTradeSide.Long, 95m, 90m,
            BaseTime.AddHours(4));

        CryptoSymbolIntervalZones zones = new();
        zones.Add(zone);

        // Candle BEFORE the zone's open time should not affect it
        CryptoCandleList candles = [];
        CandleTime earlyTime = CandleTime.AlignFromDateTime(BaseTime, interval.Duration);
        candles.TryAdd(earlyTime, new CryptoCandle
        {
            OpenTime = earlyTime,
            Open = 92m,
            High = 93m,
            Low = 88m,
            Close = 89m,
            Volume = 1000m,
        });

        ZoneDlz.CheckAndMarkBrokenZones(interval, candles, zones);

        Assert.AreEqual(1, zones.LongOpen.Count, "Zone should remain open");
        Assert.AreEqual(0, zone.TouchCount);
    }

    [TestMethod]
    public void CheckAndMarkBrokenZones_MidpointTouchSetsMitigated()
    {
        InitTestSession();
        ConfigureSettingsForTest();
        GlobalData.Settings.Signal.ZonesDlz.MaxTouches = 0; // disable touch-based closure

        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        // Demand zone: top=100, bottom=90, midpoint=95
        var zone = CreateTestZone(symbol, interval, CryptoTradeSide.Long, 100m, 90m, BaseTime);

        CryptoSymbolIntervalZones zones = new();
        zones.Add(zone);

        // Candle wicks below midpoint (low=94 < midpoint=95) but body stays above bottom
        CryptoCandleList candles = [];
        CandleTime touchTime = zone.OpenTime + interval.Duration;
        candles.TryAdd(touchTime, new CryptoCandle
        {
            OpenTime = touchTime,
            Open = 101m,
            High = 102m,
            Low = 94m,
            Close = 101m,
            Volume = 1000m,
        });

        ZoneDlz.CheckAndMarkBrokenZones(interval, candles, zones);

        Assert.AreEqual(1, zones.LongOpen.Count, "Zone should remain open");
        Assert.IsTrue(zone.IsMitigated, "Zone should be marked as mitigated (wick past midpoint)");
    }

    // ────────────────────────────────────────────────────────────────────────
    // ZoneInvalidation (public API, directly testable)
    // ────────────────────────────────────────────────────────────────────────

    [TestMethod]
    public void ZoneInvalidation_LongZone_CloseAboveBottom_NotBroken()
    {
        InitTestSession();
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);

        var zone = CreateTestZone(symbol, interval, CryptoTradeSide.Long, 95m, 90m, BaseTime);

        CryptoCandle candle = new()
        {
            OpenTime = zone.OpenTime + interval.Duration,
            Open = 91m,
            High = 94m,
            Low = 89m,
            Close = 91m,
            Volume = 1000m,
        };

        bool broken = ZoneInvalidation.ApplyToCandle(zone, candle, interval, maxTouches: 0);

        Assert.IsFalse(broken, "Close=91 > Bottom=90 -> not broken");
        Assert.AreEqual(1, zone.TouchCount, "Wick into zone counts as a touch");
    }

    [TestMethod]
    public void ZoneInvalidation_LongZone_CloseBelowBottom_Broken()
    {
        InitTestSession();
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);

        var zone = CreateTestZone(symbol, interval, CryptoTradeSide.Long, 95m, 90m, BaseTime);

        CryptoCandle candle = new()
        {
            OpenTime = zone.OpenTime + interval.Duration,
            Open = 91m,
            High = 94m,
            Low = 88m,
            Close = 89m,
            Volume = 1000m,
        };

        bool broken = ZoneInvalidation.ApplyToCandle(zone, candle, interval, maxTouches: 0);

        Assert.IsTrue(broken, "Close=89 < Bottom=90 -> broken");
        Assert.IsNotNull(zone.CloseTime);
    }

    [TestMethod]
    public void ZoneInvalidation_ShortZone_CloseAboveTop_Broken()
    {
        InitTestSession();
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);

        var zone = CreateTestZone(symbol, interval, CryptoTradeSide.Short, 110m, 105m, BaseTime);

        CryptoCandle candle = new()
        {
            OpenTime = zone.OpenTime + interval.Duration,
            Open = 108m,
            High = 112m,
            Low = 107m,
            Close = 111m,
            Volume = 1000m,
        };

        bool broken = ZoneInvalidation.ApplyToCandle(zone, candle, interval, maxTouches: 0);

        Assert.IsTrue(broken, "Close=111 > Top=110 -> broken");
        Assert.IsNotNull(zone.CloseTime);
    }
}
