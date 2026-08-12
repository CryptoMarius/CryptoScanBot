using CryptoScanner.Core.Model;
using CryptoScanner.Emulator.Engine;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The replay splits a run into chunks and reloads the candles per chunk. Every bug this file
/// guards against was found by comparing full runs on a 1m, 5m and 15m base interval — runs of
/// twenty minutes or more each. The arithmetic is pure, so these checks cost milliseconds.
///
/// The costly one was: candles were loaded up to the OPEN time of the last base candle instead of
/// its CLOSE time, so on a 5m run the final four 1m candles of every chunk were missing. Nothing
/// crashed; the newest 1m candle was simply four minutes stale, and order timestamps derived from
/// it landed before the position they belonged to — which let them fill against candles from
/// before the order existed.
/// </summary>
[TestClass]
public class ReplayChunkTests
{
    private const uint Week = 7 * 24 * 60;

    private static CandleTime T(uint minutes) => new(minutes);

    /// <summary>Walks the whole replay the way ReplayRunner does and returns every chunk.</summary>
    private static List<ReplayChunk> WalkChunks(CandleTime from, CandleTime replayTo, uint chunkMinutes, uint baseDuration)
    {
        List<ReplayChunk> chunks = [];
        CandleTime cursor = from;
        while (cursor < replayTo && chunks.Count < 1000)
        {
            ReplayChunk chunk = ReplayChunk.Resolve(cursor, replayTo, chunkMinutes, baseDuration);
            chunks.Add(chunk);
            cursor = chunkMinutes > 0 ? chunk.NextFrom : replayTo;
        }
        return chunks;
    }


    [TestMethod]
    [DataRow(1u)]
    [DataRow(5u)]
    [DataRow(15u)]
    public void LoadWindowReachesTheCloseOfTheLastBaseCandle(uint baseDuration)
    {
        ReplayChunk chunk = ReplayChunk.Resolve(T(0), T(4 * Week), Week, baseDuration);

        // The replay runs while openTime <= LastBaseOpen, so the clock reaches LastBaseOpen + base.
        Assert.AreEqual(chunk.LastBaseOpen + baseDuration, chunk.End,
            "End must be the CLOSE time of the last base candle");
        Assert.AreEqual(chunk.End, chunk.LoadTo,
            "candles have to be loaded up to that close time, not to the last candle's open time");
    }


    [TestMethod]
    [DataRow(1u)]
    [DataRow(5u)]
    [DataRow(15u)]
    public void EveryMinuteOfTheChunkIsCoveredByTheOneMinuteLoadWindow(uint baseDuration)
    {
        ReplayChunk chunk = ReplayChunk.Resolve(T(0), T(4 * Week), Week, baseDuration);

        // Every 1m candle the replay hands over closes at or before the chunk's end, so its open
        // time runs up to End - 1. That is the candle whose absence made order timestamps stale.
        CandleTime lastOneMinuteOpen = chunk.End - 1;
        Assert.IsTrue(chunk.LoadFrom(1) <= T(0), "the 1m window must start at or before the chunk start");
        Assert.IsTrue(lastOneMinuteOpen <= chunk.LoadTo,
            $"1m candle {lastOneMinuteOpen.Minutes} closes inside the chunk but falls outside the load window "
            + $"[{chunk.LoadFrom(1).Minutes}, {chunk.LoadTo.Minutes}]");
    }


    [TestMethod]
    [DataRow(5u)]
    [DataRow(15u)]
    public void FinerIntervalsAreCoveredToTheChunkEnd(uint baseDuration)
    {
        ReplayChunk chunk = ReplayChunk.Resolve(T(0), T(4 * Week), Week, baseDuration);

        // Intervals FINER than the base interval are the ones that kept running inside the final
        // base candle - exactly the set whose candles went missing.
        foreach (uint interval in new uint[] { 1, 2, 3, 5, 10, 15 })
        {
            if (interval >= baseDuration)
                continue;
            CandleTime lastOpen = chunk.End - interval;
            Assert.IsTrue(lastOpen <= chunk.LoadTo,
                $"base {baseDuration}m: the last {interval}m candle ({lastOpen.Minutes}) is not loaded");
        }
    }


    [TestMethod]
    [DataRow(1u)]
    [DataRow(5u)]
    [DataRow(15u)]
    public void ChunksJoinWithoutGapOrOverlap(uint baseDuration)
    {
        List<ReplayChunk> chunks = WalkChunks(T(0), T(4 * Week), Week, baseDuration);
        Assert.IsTrue(chunks.Count > 1, "this range should produce several chunks");

        for (int i = 1; i < chunks.Count; i++)
        {
            // The first base candle of a chunk must open exactly where the previous one's clock
            // stopped: no minute replayed twice, none skipped.
            Assert.AreEqual(chunks[i - 1].End, chunks[i].From,
                $"chunk {i} starts at {chunks[i].From.Minutes} but chunk {i - 1} ended at {chunks[i - 1].End.Minutes}");
        }
    }


    [TestMethod]
    [DataRow(1u)]
    [DataRow(5u)]
    [DataRow(15u)]
    public void ChunkBoundariesDoNotDriftWithTheBaseInterval(uint baseDuration)
    {
        List<ReplayChunk> chunks = WalkChunks(T(0), T(8 * Week), Week, baseDuration);

        // Every boundary lands on a whole number of chunks from the start, whatever the base
        // interval. Drift here is what once moved the boundary across the candle boundaries of the
        // higher intervals, at a different moment per base interval.
        for (int i = 0; i < chunks.Count; i++)
        {
            uint expected = (uint)i * Week;
            Assert.AreEqual(expected, chunks[i].From.Minutes,
                $"base {baseDuration}m: chunk {i} starts at {chunks[i].From.Minutes}, expected {expected}");
        }
    }


    [TestMethod]
    public void BoundariesAreIdenticalAcrossBaseIntervals()
    {
        List<CandleTime> reference = WalkChunks(T(0), T(8 * Week), Week, 1).Select(c => c.From).ToList();

        foreach (uint baseDuration in new uint[] { 2, 3, 5, 10, 15, 30 })
        {
            List<CandleTime> actual = WalkChunks(T(0), T(8 * Week), Week, baseDuration).Select(c => c.From).ToList();
            CollectionAssert.AreEqual(reference, actual,
                $"base {baseDuration}m produces different chunk starts than a 1m run");
        }
    }


    [TestMethod]
    public void StraddlingCandleOfACoarserIntervalIsLoaded()
    {
        // A chunk starting mid-day: the daily candle opened before it and closes inside it. Loading
        // from the chunk start would drop that candle for good - the previous chunk ended before
        // its close time, so it was never handed over there either.
        CandleTime start = T(Week + 13 * 60);
        ReplayChunk chunk = ReplayChunk.Resolve(start, T(8 * Week), Week, 5);

        Assert.IsTrue(chunk.LoadFrom(24 * 60) <= start, "the daily window must reach back over the chunk start");
        Assert.AreEqual(0u, chunk.LoadFrom(24 * 60).Minutes % (24 * 60), "and land on a day boundary");
    }


    [TestMethod]
    [DataRow(1u)]
    [DataRow(5u)]
    [DataRow(15u)]
    public void ReplayStopsExactlyOnItsEndDate(uint baseDuration)
    {
        // A range that is not a whole number of chunks. The last base candle has to CLOSE on
        // replayTo, not open there - opening there would run the replay a base interval past its
        // own end date, and by a different amount for every base interval.
        CandleTime replayTo = T(Week + 3 * 24 * 60);
        List<ReplayChunk> chunks = WalkChunks(T(0), replayTo, Week, baseDuration);

        Assert.AreEqual(replayTo, chunks[^1].End, "the replay must end exactly on its end date");
        Assert.AreEqual(replayTo - baseDuration, chunks[^1].LastBaseOpen,
            "so the last base candle opens one interval before it");
    }


    /// <summary>
    /// Replays the chunk walk together with what SymbolReplay.AdvanceTo does — cursor per interval,
    /// reset to the chunk's LoadFrom, handing over a candle once its close time is reached — and
    /// reports which candle open times end up in the CandleList.
    /// </summary>
    private static List<CandleTime> Delivered(CandleTime from, CandleTime replayTo, uint chunkMinutes,
        uint baseDuration, uint intervalDuration)
    {
        HashSet<CandleTime> inList = [];
        List<CandleTime> order = [];

        foreach (ReplayChunk chunk in WalkChunks(from, replayTo, chunkMinutes, baseDuration))
        {
            CandleTime cursor = chunk.LoadFrom(intervalDuration);
            for (CandleTime openTime = chunk.From; openTime <= chunk.LastBaseOpen; openTime += baseDuration)
            {
                CandleTime clock = openTime + baseDuration;
                while (cursor + intervalDuration <= clock)
                {
                    // Only candles inside the chunk's load window exist in memory at this point.
                    if (cursor >= chunk.LoadFrom(intervalDuration) && cursor <= chunk.LoadTo && inList.Add(cursor))
                        order.Add(cursor);
                    cursor += intervalDuration;
                }
            }
        }
        return order;
    }


    [TestMethod]
    [DataRow(1u)]
    [DataRow(5u)]
    [DataRow(15u)]
    public void WeeklyCandlesSurviveAWeeklyChunk(uint baseDuration)
    {
        // The chunk is exactly one week and so is the candle, which is the awkward case: at most one
        // weekly candle closes per chunk and it always straddles the boundary. Start mid-week so the
        // chunk boundaries and the Monday boundaries do NOT line up.
        CandleTime from = T(2 * Week + 3 * 24 * 60 + 23 * 60);   // a Wednesday 23:00
        CandleTime replayTo = from + 6 * Week;

        List<CandleTime> delivered = Delivered(from, replayTo, Week, baseDuration, Week);

        Assert.IsTrue(delivered.Count >= 5, $"expected roughly one weekly candle per chunk, got {delivered.Count}");
        foreach (CandleTime candle in delivered)
        {
            Assert.AreEqual(0u, candle.Minutes % Week, "weekly candles open on a week boundary");
            Assert.IsTrue(candle + Week > from, "a candle closing before the replay starts is not ours");
        }
        CollectionAssert.AllItemsAreUnique(delivered, "no weekly candle may be handed over twice");
    }


    [TestMethod]
    [DataRow(1u)]
    [DataRow(2u)]
    [DataRow(3u)]
    [DataRow(5u)]
    [DataRow(10u)]
    [DataRow(15u)]
    [DataRow(30u)]
    [DataRow(60u)]
    [DataRow(24u * 60)]
    [DataRow(Week)]
    public void EveryIntervalIsHandedOverExactlyOnceAndNeverEarly(uint intervalDuration)
    {
        CandleTime from = T(2 * Week + 3 * 24 * 60 + 23 * 60);
        CandleTime replayTo = from + 4 * Week;

        foreach (uint baseDuration in new uint[] { 1, 5, 15 })
        {
            List<CandleTime> delivered = Delivered(from, replayTo, Week, baseDuration, intervalDuration);
            CollectionAssert.AllItemsAreUnique(delivered,
                $"interval {intervalDuration}m on a {baseDuration}m base: a candle was handed over twice");

            for (int i = 1; i < delivered.Count; i++)
            {
                Assert.AreEqual(delivered[i - 1] + intervalDuration, delivered[i],
                    $"interval {intervalDuration}m on a {baseDuration}m base: gap between "
                    + $"{delivered[i - 1].Minutes} and {delivered[i].Minutes}");
            }
        }
    }


    [TestMethod]
    public void AllBaseIntervalsSeeTheSameCandles()
    {
        CandleTime from = T(2 * Week + 3 * 24 * 60 + 23 * 60);
        CandleTime replayTo = from + 4 * Week;

        // The whole point of the exercise: what the engine gets to see must not depend on the base
        // interval. Checked for a fine interval, a coarse one and the weekly edge case.
        foreach (uint intervalDuration in new uint[] { 1, 15, 24 * 60, Week })
        {
            List<CandleTime> reference = Delivered(from, replayTo, Week, 1, intervalDuration);
            foreach (uint baseDuration in new uint[] { 5, 15 })
            {
                List<CandleTime> actual = Delivered(from, replayTo, Week, baseDuration, intervalDuration);
                CollectionAssert.AreEqual(reference, actual,
                    $"interval {intervalDuration}m: a {baseDuration}m base sees different candles than a 1m base");
            }
        }
    }


    [TestMethod]
    public void ChunkSizeNeedNotBeAMultipleOfTheCoarsestInterval()
    {
        // Chunks are configured in days, so they are NOT tied to the weekly interval. A weekly
        // candle then closes in only some of the chunks - the cursor simply keeps pointing at it
        // until the chunk that contains its close time comes along.
        CandleTime from = T(2 * Week + 3 * 24 * 60 + 23 * 60);
        CandleTime replayTo = from + 6 * Week;

        List<CandleTime> reference = Delivered(from, replayTo, Week, 5, Week);

        foreach (uint chunkDays in new uint[] { 1, 2, 3, 5, 10, 14 })
        {
            List<CandleTime> actual = Delivered(from, replayTo, chunkDays * 24 * 60, 5, Week);
            CollectionAssert.AreEqual(reference, actual,
                $"a {chunkDays}-day chunk delivers different weekly candles than a 7-day one");
        }
    }


    [TestMethod]
    public void ReplayStartNeedNotBeAlignedToAnyInterval()
    {
        // The run starts wherever the user says. Offsetting the start by odd amounts must not change
        // which candles of a coarse interval get handed over, only where the series begins.
        CandleTime baseStart = T(4 * Week);
        CandleTime replayTo = baseStart + 6 * Week;

        foreach (uint interval in new uint[] { 15, 24 * 60, Week })
        {
            foreach (uint offset in new uint[] { 0, 1, 7, 23 * 60, 3 * 24 * 60 + 17 })
            {
                CandleTime from = baseStart + offset;
                foreach (uint baseDuration in new uint[] { 1, 5, 15 })
                {
                    List<CandleTime> delivered = Delivered(from, replayTo, Week, baseDuration, interval);

                    // Whatever the offset, the series is gapless and no candle arrives before it closed.
                    for (int i = 1; i < delivered.Count; i++)
                    {
                        Assert.AreEqual(delivered[i - 1] + interval, delivered[i],
                            $"interval {interval}m, offset {offset}m, base {baseDuration}m: gap in the series");
                    }
                    Assert.IsTrue(delivered.Count > 0, "something should be delivered");
                    Assert.IsTrue(delivered[0] + interval > from,
                        "the first candle handed over must be one that closes inside the replay");
                }
            }
        }
    }


    [TestMethod]
    public void ShortReplayIsASingleChunk()
    {
        // Shorter than one chunk: no splitting, and the whole range is replayed in one pass.
        CandleTime replayTo = T(3 * 24 * 60);
        List<ReplayChunk> chunks = WalkChunks(T(0), replayTo, Week, 5);

        Assert.AreEqual(1, chunks.Count);
        Assert.AreEqual(replayTo, chunks[0].End);
    }
}
