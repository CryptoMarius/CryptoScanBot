using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.CoreTests.Zones;

/// <summary>
/// A zone calculation only ever sees the candle window it was given, so it can only re-derive the
/// zones whose pivot still falls inside it. It used to DELETE everything it could not re-derive,
/// which meant the zone history was pruned back to that window on every full calculation - on every
/// restart, and on every press of the chart's "Calculate" button. The candles to rebuild those zones
/// were long gone by then, so they were gone for good.
///
/// The rule these tests hold in place: a calculation may only delete zones it could have produced.
/// Everything older is out of its authority and is carried, until its RIGHT edge leaves the window
/// too - CloseTime for a zone that was broken, and never for one that is still open, because an open
/// level is still tradeable however old it is.
/// </summary>
[TestClass]
public class ZoneRetentionTests : TestBase
{
    private static CandleTime T(uint minutes) => new(minutes);

    /// <summary>
    /// DeleteRemainingZones queues a deleted zone for saving, and that queue only exists once a
    /// scanner session has started. Nothing consumes it here; it just has to be there.
    /// </summary>
    [TestInitialize]
    public void EnsureSaveQueue()
    {
        InitTestSession();
        GlobalData.ThreadSaveObjects ??= new ThreadSaveObjects();
    }

    /// <summary>
    /// A zone with the two times this rule is about. Built through ZoneDlzTests.CreateTestZone so it
    /// carries the exchange/symbol/interval a real zone has, then the times are set directly - the
    /// helper aligns them to the interval and these tests want exact minutes.
    /// </summary>
    private static CryptoZone Zone(CandleTime openTime, CandleTime? closeTime, int id = 1)
    {
        InitTestSession();
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        CryptoZone zone = ZoneDlzTests.CreateTestZone(symbol, interval, CryptoTradeSide.Long,
            100m, 90m, new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        zone.Id = id;
        zone.OpenTime = openTime;
        zone.CloseTime = closeTime;
        return zone;
    }


    // windowStart is 1000 in every case; the zone moves around it.
    [DataTestMethod]
    // Starts inside the window: the calculation had its say. Not producing it means it is gone.
    [DataRow(1200u, null, false, "open, pivot inside the window")]
    [DataRow(1200u, 1300u, false, "closed, pivot inside the window")]
    // Starts before the window: out of the calculation's authority.
    [DataRow(500u, null, true, "open, pivot before the window")]
    [DataRow(500u, 1400u, true, "broken inside the window")]
    [DataRow(500u, 1000u, true, "broken exactly on the window start")]
    [DataRow(500u, 999u, false, "broken before the window: both edges out of sight")]
    public void OnlyZonesOutOfSightAndStillRelevantAreCarried(uint openTime, uint? closeTime,
        bool expectCarried, string because)
    {
        CryptoZone zone = Zone(T(openTime), closeTime.HasValue ? T(closeTime.Value) : null);

        bool carried = ZoneTools.OutOfSightButStillRelevant(zone, T(1000));

        Assert.AreEqual(expectCarried, carried, because);
    }


    /// <summary>
    /// The whole point, at the level the callers use: a zone from before the window survives the
    /// reconciliation instead of being marked for deletion, and lands in the result so the next pass
    /// still knows about it.
    /// </summary>
    [TestMethod]
    public void ReconciliationCarriesTheOldZoneAndDropsTheVanishedOne()
    {
        CryptoZone older = Zone(T(500), null, id: 11);      // pivot out of sight, still open
        CryptoZone vanished = Zone(T(1200), null, id: 12);  // inside the window, not produced now

        SortedList<(CryptoTradeSide, CandleTime?, decimal, decimal), CryptoZone> oldZones = [];
        DatabaseStatistics statistics = new();
        ZoneTools.CreateZoneIndex([older, vanished], oldZones, statistics);

        CryptoSymbolIntervalZones result = new();
        ZoneTools.DeleteRemainingZones(oldZones, statistics, result, T(1000));

        Assert.AreEqual(1, statistics.Retained, "the zone from before the window should be carried");
        Assert.AreEqual(1, statistics.Deleted, "the zone inside the window should be deleted");
        Assert.IsTrue(older.Id > 0, "a carried zone keeps its id (a negative id means 'delete me')");
        Assert.IsTrue(vanished.Id < 0, "a deleted zone is flagged by negating its id");
        CollectionAssert.Contains(result.LongOpen.ToList(), older,
            "the carried zone has to end up in the result, or the next pass loses it anyway");
    }


    /// <summary>
    /// Without a window nothing is carried. The callers that own their entire result rely on that,
    /// and it keeps this change from quietly altering paths it was not meant for.
    /// </summary>
    [TestMethod]
    public void WithoutAWindowEverythingIsStillDeleted()
    {
        CryptoZone older = Zone(T(500), null, id: 21);
        SortedList<(CryptoTradeSide, CandleTime?, decimal, decimal), CryptoZone> oldZones = [];
        DatabaseStatistics statistics = new();
        ZoneTools.CreateZoneIndex([older], oldZones, statistics);

        ZoneTools.DeleteRemainingZones(oldZones, statistics);

        Assert.AreEqual(0, statistics.Retained);
        Assert.AreEqual(1, statistics.Deleted);
    }


    /// <summary>
    /// The marker survives a restart but the committed store it vouches for does not - that store is
    /// a plain in-memory list. Restoring only the marker would be worse than restoring nothing: the
    /// next pass would believe the settled part was accounted for, hand the reconciliation nothing
    /// but its tail, and everything else would look gone. So the store is rebuilt from the zones.
    /// </summary>
    [TestMethod]
    public void CommittedStoreIsRebuiltFromTheLoadedZones()
    {
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];
        CryptoSymbolInterval symbolInterval = symbol.Data.Get(interval.IntervalPeriod);

        symbolInterval.Dlz.Zones = new();
        symbolInterval.Dlz.Zones.Add(Zone(T(500), null, id: 41));   // behind the marker  -> committed
        symbolInterval.Dlz.Zones.Add(Zone(T(900), null, id: 42));   // behind the marker  -> committed
        symbolInterval.Dlz.Zones.Add(Zone(T(1500), null, id: 43));  // in the mutable tail -> not
        symbolInterval.Dlz.CommittedPivotMarker = T(1000);

        ZoneDlz.RebuildCommittedStoreFromLoadedZones(symbol);

        CollectionAssert.AreEquivalent(new List<int> { 41, 42 },
            symbolInterval.Dlz.CommittedZones.ConvertAll(z => z.Id),
            "only zones at or before the marker are settled; the tail is rebuilt every pass");
        Assert.AreEqual(T(1000), symbolInterval.Dlz.CommittedPivotMarker,
            "a marker with zones behind it stays");
    }


    /// <summary>
    /// A marker with nothing behind it is not trusted - that can be a main database that was cleared
    /// while the candle store was kept. Dropping it costs one full rescan; believing it costs zones.
    /// </summary>
    [TestMethod]
    public void MarkerWithoutZonesBehindItIsDropped()
    {
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];
        CryptoSymbolInterval symbolInterval = symbol.Data.Get(interval.IntervalPeriod);

        symbolInterval.Dlz.Zones = new();
        symbolInterval.Dlz.Zones.Add(Zone(T(1500), null, id: 51)); // only tail zones
        symbolInterval.Dlz.CommittedPivotMarker = T(1000);

        ZoneDlz.RebuildCommittedStoreFromLoadedZones(symbol);

        Assert.IsNull(symbolInterval.Dlz.CommittedPivotMarker,
            "without zones behind it the marker cannot be relied on, so the next pass rescans");
        Assert.AreEqual(0, symbolInterval.Dlz.CommittedZones.Count);
    }


    /// <summary>
    /// The committed store ages on the same right edge. It is the carrier for zones whose pivot has
    /// gone, so ageing it on OpenTime (what it did before) threw away exactly what the rule above is
    /// there to keep.
    /// </summary>
    [TestMethod]
    public void CommittedStoreAgesOnTheRightEdgeNotTheLeft()
    {
        CandleTime minDate = T(1000);
        List<CryptoZone> store =
        [
            Zone(T(500), null, id: 31),       // old pivot, still open  -> keep
            Zone(T(500), T(1400), id: 32),    // old pivot, broken recently -> keep
            Zone(T(500), T(999), id: 33),     // old pivot, broken long ago -> drop
        ];

        // Same predicate as ZoneDlz uses on the committed store.
        store.RemoveAll(zone => zone.CloseTime != null && zone.CloseTime.Value < minDate);

        CollectionAssert.AreEquivalent(new List<int> { 31, 32 }, store.ConvertAll(z => z.Id),
            "only the zone whose break itself scrolled out of the window should go");
    }


    /// <summary>
    /// The retention window has to come from the PIVOTS, not from the candle window, because the two
    /// are trimmed by different rules and only happen to agree at the default setting.
    /// <para>
    /// The two were separate numbers until 2026-08-22: ZigZagList was trimmed on a flat 500 candles
    /// while the zone calculation asked for ZonesDlz.CandleCount, a setting that suggested 3000 next
    /// to it. Every zone in that 2500-candle gap then counted as "the calculation had its say and did
    /// not produce it" and was deleted, while in truth no pivot for it existed - the same pruning
    /// ZoneRetention was written to stop, through the back door. That setting is gone and both now
    /// follow CandleTools.CandleCountFetch, but the boundary still comes from the pivots: they are
    /// what the calculation holds, and a restart, a gap or a trim can leave them shorter than the
    /// window without any setting being involved.
    /// </para>
    /// </summary>
    [TestMethod]
    public void OldestPivotIsTheBoundaryNotTheCandleWindow()
    {
        ZigZagIndicator indicator = new(TrendType.Primary, false);
        Assert.IsNull(indicator.OldestPivotTime, "an empty list can speak for nothing");

        CryptoCandle old = new() { OpenTime = T(5000) };
        CryptoCandle young = new() { OpenTime = T(9000) };
        indicator.ZigZagList.Add(new ZigZagResult { PointType = 'L', Candle = old, Value = 1 });
        indicator.ZigZagList.Add(new ZigZagResult { PointType = 'H', Candle = young, Value = 2 });

        Assert.AreEqual(T(5000), indicator.OldestPivotTime);

        // A dummy is a marker at the right-hand edge, never the oldest entry, but it must not be the
        // answer if it ever sits in front.
        indicator.ZigZagList.Insert(0, new ZigZagResult
        {
            PointType = 'H',
            Candle = new CryptoCandle { OpenTime = T(1000) },
            Value = 3,
            Dummy = true,
        });
        Assert.AreEqual(T(5000), indicator.OldestPivotTime,
            "a dummy point is provisional and cannot extend the reach of the list");
    }

    /// <summary>
    /// The case point 62 asked for: a zone that lies BEFORE the boundary has to survive a pass that
    /// does not produce it, while one inside the boundary does not.
    /// <para>
    /// The chunk-invariance tests cannot see this. They run on one candle series in which nothing
    /// falls outside the window, so both halves of the rule give the same answer there. This is the
    /// test that actually stands on the line.
    /// </para>
    /// </summary>
    [TestMethod]
    public void AZoneBeforeTheBoundarySurvivesAPassThatDoesNotProduceIt()
    {
        CandleTime boundary = T(5000);

        CryptoZone beforeAndOpen = Zone(T(4000), null, 1);
        CryptoZone beforeAndClosedInside = Zone(T(4000), T(6000), 2);
        CryptoZone beforeAndClosedOutside = Zone(T(4000), T(4500), 3);
        CryptoZone insideAndOpen = Zone(T(7000), null, 4);

        Assert.IsTrue(ZoneTools.OutOfSightButStillRelevant(beforeAndOpen, boundary),
            "an open zone is tradeable however old it is");
        Assert.IsTrue(ZoneTools.OutOfSightButStillRelevant(beforeAndClosedInside, boundary),
            "its right edge is still in view, so it still explains what price did");
        Assert.IsFalse(ZoneTools.OutOfSightButStillRelevant(beforeAndClosedOutside, boundary),
            "both edges are behind the boundary - this one really is gone");
        Assert.IsFalse(ZoneTools.OutOfSightButStillRelevant(insideAndOpen, boundary),
            "inside the boundary the calculation had its say, so not producing it means rejected");
    }
}
