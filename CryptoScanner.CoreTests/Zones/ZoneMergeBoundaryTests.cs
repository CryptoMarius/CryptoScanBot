using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.CoreTests.Zones;

/// <summary>
/// An incremental calculation that still reconciles every zone it has is not incremental where it
/// counts. Until 2026-08-22 the incremental branch handed the merge its whole committed store plus
/// the tail, so the cost of a recalculation scaled with how many zones EXIST rather than with how
/// many changed - on a seven-month replay that is hundreds of zones indexed, compared and offered to
/// the database on every single pass.
/// <para>
/// <see cref="ZoneDlz.SplitOnMergeBoundary"/> is what fixed that: zones resting on a settled pivot
/// are carried across untouched, and only the tail is reconciled. These tests pin that split,
/// because nothing else does - ZoneDlzIncrementalTests deliberately leaves the merge out, so the
/// change is invisible there.
/// </para>
/// </summary>
[TestClass]
public class ZoneMergeBoundaryTests : TestBase
{
    private static CandleTime T(uint minutes) => new(minutes);

    /// <summary>A zone with a given open time, built like a real one so the merge key is valid.</summary>
    private static CryptoZone Zone(CandleTime openTime, CryptoTradeSide side, decimal top, decimal bottom,
        int id)
    {
        InitTestSession();
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        CryptoZone zone = ZoneDlzTests.CreateTestZone(symbol, interval, side, top, bottom,
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));
        zone.Id = id;
        zone.OpenTime = openTime;
        return zone;
    }

    /// <summary>
    /// Older zones go straight through, recent ones go to the index. And "straight through" has to
    /// mean the SAME object: hand the merge a fresh copy and it sees TouchCount 0 and no CloseTime,
    /// reads that as a modification, and writes the zone back to the database - throwing away the
    /// touches the live zone had collected.
    /// </summary>
    [TestMethod]
    public void ZonesBeforeTheBoundaryAreCarriedAndTheRestIsIndexed()
    {
        CryptoSymbolIntervalZones live = new();
        CryptoZone settledLong = Zone(T(1000), CryptoTradeSide.Long, 110m, 100m, 1);
        CryptoZone settledShort = Zone(T(2000), CryptoTradeSide.Short, 210m, 200m, 2);
        CryptoZone tailLong = Zone(T(9000), CryptoTradeSide.Long, 310m, 300m, 3);
        CryptoZone tailShort = Zone(T(9500), CryptoTradeSide.Short, 410m, 400m, 4);
        foreach (CryptoZone zone in new[] { settledLong, settledShort, tailLong, tailShort })
            live.Add(zone);

        CryptoSymbolIntervalZones carried = new();
        SortedList<(CryptoTradeSide, CandleTime?, decimal, decimal), CryptoZone> index = [];
        DatabaseStatistics statistics = new();

        ZoneDlz.SplitOnMergeBoundary(live, T(5000), carried, index, statistics);

        List<int> carriedIds = [.. carried.LongOpen.Concat(carried.ShortOpen)
            .Concat(carried.LongClosed).Concat(carried.ShortClosed).Select(z => z.Id)];
        CollectionAssert.AreEquivalent(new List<int> { 1, 2 }, carriedIds,
            "only the zones before the boundary belong in the carry set");

        CollectionAssert.AreEquivalent(new List<int> { 3, 4 }, index.Values.Select(z => z.Id).ToList(),
            "only the zones at or after the boundary belong in the index");

        Assert.AreSame(settledLong, carried.LongOpen.Single(z => z.Id == 1),
            "a carried zone has to keep its own object, or the merge would overwrite its touches");
    }

    /// <summary>
    /// The boundary is inclusive at the tail side: a zone exactly ON it was re-derivable this pass,
    /// so it has to be reconciled and not carried. Getting this the wrong way round would leave a
    /// zone in the carry set that the calculation just disproved.
    /// </summary>
    [TestMethod]
    public void AZoneExactlyOnTheBoundaryIsReconciled()
    {
        CryptoSymbolIntervalZones live = new();
        live.Add(Zone(T(5000), CryptoTradeSide.Long, 110m, 100m, 7));

        CryptoSymbolIntervalZones carried = new();
        SortedList<(CryptoTradeSide, CandleTime?, decimal, decimal), CryptoZone> index = [];
        ZoneDlz.SplitOnMergeBoundary(live, T(5000), carried, index, new DatabaseStatistics());

        Assert.AreEqual(0, carried.LongOpen.Count + carried.ShortOpen.Count
                         + carried.LongClosed.Count + carried.ShortClosed.Count);
        Assert.AreEqual(1, index.Count);
    }

    /// <summary>
    /// Nothing may go missing in the split - every live zone ends up in exactly one of the two.
    /// </summary>
    [TestMethod]
    public void EveryZoneEndsUpInExactlyOnePlace()
    {
        CryptoSymbolIntervalZones live = new();
        for (uint i = 1; i <= 10; i++)
            live.Add(Zone(T(i * 1000), i % 2 == 0 ? CryptoTradeSide.Long : CryptoTradeSide.Short,
                100m + i, 90m + i, (int)i));

        CryptoSymbolIntervalZones carried = new();
        SortedList<(CryptoTradeSide, CandleTime?, decimal, decimal), CryptoZone> index = [];
        ZoneDlz.SplitOnMergeBoundary(live, T(4000), carried, index, new DatabaseStatistics());

        int carriedCount = carried.LongOpen.Count + carried.ShortOpen.Count
                         + carried.LongClosed.Count + carried.ShortClosed.Count;
        Assert.AreEqual(10, carriedCount + index.Count, "a zone was lost or counted twice");
        Assert.AreEqual(3, carriedCount, "1000, 2000 and 3000 lie before the boundary");
    }
}
