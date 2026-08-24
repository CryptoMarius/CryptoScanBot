using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.CoreTests.Zones;

/// <summary>
/// A candle that is not in memory used to be indistinguishable from a candle in which nothing
/// happened: every zone walk reads its candles with <c>TryGetValue</c> and a miss fell through the
/// <c>if</c>. These tests pin down that the miss is now counted, that a hole long enough to matter is
/// separated from the single candle an exchange simply never published, and that the counting does
/// not change what the walk itself decides.
/// </summary>
[TestClass]
public class ZoneCandleGapsTests : TestBase
{
    private static readonly CandleTime Start = CandleTime.FromDateTime(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));


    [TestMethod]
    public void CandleGapWalk_CountsPresentAndMissing()
    {
        CandleGapWalk walk = new();
        walk.Hit();
        walk.Miss(Start);
        walk.Hit();

        Assert.AreEqual(2, walk.Present);
        Assert.AreEqual(1, walk.Missing);
        Assert.AreEqual(1, walk.LongestGap);
        Assert.AreEqual(Start, walk.FirstMissing, "The FIRST missing key is the one worth reporting");
    }


    [TestMethod]
    public void CandleGapWalk_LongestGapIsTheLongestRunNotTheTotal()
    {
        CandleGapWalk walk = new();
        // Two separate holes of two, not one hole of four.
        walk.Miss(Start);
        walk.Miss(Start + 60u);
        walk.Hit();
        walk.Miss(Start + 180u);
        walk.Miss(Start + 240u);

        Assert.AreEqual(4, walk.Missing);
        Assert.AreEqual(2, walk.LongestGap);
        Assert.IsFalse(walk.Interrupted, "Two in a row is within the tolerance, however many holes there are");
    }


    [TestMethod]
    public void CandleGapWalk_ToleratedGapSeparatesQuietCandlesFromAnInterruption()
    {
        CandleGapWalk quiet = new();
        for (int i = 0; i < ZoneCandleGaps.ToleratedGap; i++)
            quiet.Miss(Start + (uint)(i * 60));
        Assert.AreEqual(ZoneCandleGaps.ToleratedGap, quiet.LongestGap);
        Assert.IsFalse(quiet.Interrupted, $"{ZoneCandleGaps.ToleratedGap} in a row is still an exchange that published nothing");

        CandleGapWalk interrupted = new();
        for (int i = 0; i < ZoneCandleGaps.ToleratedGap + 1; i++)
            interrupted.Miss(Start + (uint)(i * 60));
        Assert.AreEqual(ZoneCandleGaps.ToleratedGap + 1, interrupted.LongestGap);
        Assert.IsTrue(interrupted.Interrupted, "One more is an interruption, and that is what changes which zones survive");
    }


    [TestMethod]
    public void CandleGapWalk_NothingMissingIsNotAGap()
    {
        CandleGapWalk walk = new();
        walk.Hit();
        walk.Hit();

        Assert.AreEqual(0, walk.Missing);
        Assert.AreEqual(0, walk.LongestGap);
        Assert.IsFalse(walk.Interrupted);
    }


    /// <summary>
    /// The point of the whole exercise: a zone that WAS broken during an interruption must not come
    /// out of the walk as still open. The walk cannot invent the candles, so this pins down the half
    /// it can do - the hole is visible in the counters instead of being swallowed.
    /// </summary>
    [TestMethod]
    public void CheckAndMarkBrokenZones_HoleInTheHistoryIsCountedNotSwallowed()
    {
        InitTestSession();
        ZoneDlzTests.ConfigureSettingsForTest();

        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        // Demand (Long) zone: top=95, bottom=90
        var zone = ZoneDlzTests.CreateTestZone(symbol, interval, CryptoTradeSide.Long, 95m, 90m,
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        CryptoSymbolIntervalZones zones = new();
        zones.Add(zone);

        // Only the candle far after the zone is present; everything between it and the zone is
        // missing - exactly the shape of an interruption. The candle that IS there does not break
        // the zone, so without counting, this walk would report a clean "nothing happened".
        CryptoCandleList candles = [];
        CandleTime lastTime = zone.OpenTime + (10 * interval.Duration);
        candles.TryAdd(lastTime, new CryptoCandle
        {
            OpenTime = lastTime,
            Open = 100m,
            High = 101m,
            Low = 99m,
            Close = 100m,
            Volume = 1000m,
        });

        PipelineProfiler.Reset();
        PipelineProfiler.Enabled = true;
        ZoneCandleGaps.Reset();
        try
        {
            ZoneDlz.CheckAndMarkBrokenZones(interval, candles, zones, symbol: symbol);

            Assert.AreEqual(1, zones.LongOpen.Count, "The candle present does not break the zone");
            Assert.IsTrue(PipelineProfiler.ZoneGapCandles > 0, "The missing candles have to be counted");
            Assert.AreEqual(1, PipelineProfiler.ZoneGapInterrupted,
                "A stretch this long is an interruption, not a quiet candle");
        }
        finally
        {
            PipelineProfiler.Enabled = false;
        }
    }


    /// <summary>
    /// Counting must not cost the walk its verdict: the same break is still found, and a complete
    /// history still reports no gap at all.
    /// </summary>
    [TestMethod]
    public void CheckAndMarkBrokenZones_CompleteHistoryStillBreaksAndReportsNoGap()
    {
        InitTestSession();
        ZoneDlzTests.ConfigureSettingsForTest();

        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval1h];

        var zone = ZoneDlzTests.CreateTestZone(symbol, interval, CryptoTradeSide.Long, 95m, 90m,
            new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        CryptoSymbolIntervalZones zones = new();
        zones.Add(zone);

        // The walk starts on the zone's OWN candle, so a complete history has to include it - it is
        // walked but never applied (the break check skips the candle the zone was born on).
        CryptoCandleList candles = [];
        candles.TryAdd(zone.OpenTime, new CryptoCandle
        {
            OpenTime = zone.OpenTime,
            Open = 96m,
            High = 97m,
            Low = 94m,
            Close = 96m,
            Volume = 1000m,
        });
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

        PipelineProfiler.Reset();
        PipelineProfiler.Enabled = true;
        ZoneCandleGaps.Reset();
        try
        {
            ZoneDlz.CheckAndMarkBrokenZones(interval, candles, zones, symbol: symbol);

            Assert.AreEqual(0, zones.LongOpen.Count, "The break is still found");
            Assert.AreEqual(1, zones.LongClosed.Count);
            Assert.AreEqual(0, PipelineProfiler.ZoneGapWalks, "A complete history has nothing to report");
        }
        finally
        {
            PipelineProfiler.Enabled = false;
        }
    }
}
