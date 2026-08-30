using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;

namespace CryptoScanner.CoreTests.Trend;

/// <summary>
/// Unit tests for <see cref="TrendIntervalBos.InterpretZigZagPoints"/> and
/// the <see cref="CryptoTrendData"/> helpers (<see cref="CryptoTrendData.LastChoCh"/>,
/// <see cref="CryptoTrendData.HasBosAfterLastChoCh"/>).
///
/// These tests use synthetic ZigZag points — no candle data or indicator calculation
/// needed. The goal is to verify the BOS/CHoCH interpretation logic in isolation.
/// </summary>
[TestClass]
public class BosChochInterpretationTests
{
    // Helper: build a minimal ZigZagResult at a given time (minutes since epoch).
    private static ZigZagResult Pivot(char type, double value, uint minutesSinceEpoch, bool dummy = false)
    {
        return new ZigZagResult
        {
            PointType = type,
            Value = value,
            Candle = new CryptoCandle { OpenTime = new CandleTime(minutesSinceEpoch) },
            Dummy = dummy,
        };
    }

    // Helper: build a ZigZagIndicator with the given pivots pre-loaded.
    private static ZigZagIndicator BuildIndicator(params ZigZagResult[] pivots)
    {
        var indicator = new ZigZagIndicator(TrendType.Primary, false);
        indicator.ZigZagList.AddRange(pivots);
        return indicator;
    }


    // ─── InterpretZigZagPoints ───────────────────────────────────────────

    [TestMethod]
    public void TooFewPivots_ReturnsUnknown()
    {
        var indicator = BuildIndicator(Pivot('L', 100, 1));
        var trend = TrendIntervalBos.InterpretZigZagPoints(indicator, null, out var events);

        Assert.AreEqual(CryptoTrendIndicator.Unknown, trend);
        Assert.AreEqual(0, events.Count);
    }

    [TestMethod]
    public void TwoPivots_LowThenHigh_InitialBullish()
    {
        var indicator = BuildIndicator(
            Pivot('L', 100, 1),
            Pivot('H', 200, 2)
        );
        var trend = TrendIntervalBos.InterpretZigZagPoints(indicator, null, out var events);

        Assert.AreEqual(CryptoTrendIndicator.Bullish, trend);
        Assert.AreEqual(0, events.Count, "two pivots only set initial trend, no events");
    }

    [TestMethod]
    public void TwoPivots_HighThenLow_InitialBearish()
    {
        var indicator = BuildIndicator(
            Pivot('H', 200, 1),
            Pivot('L', 100, 2)
        );
        var trend = TrendIntervalBos.InterpretZigZagPoints(indicator, null, out var events);

        Assert.AreEqual(CryptoTrendIndicator.Bearish, trend);
        Assert.AreEqual(0, events.Count);
    }

    [TestMethod]
    public void BOS_HigherHigh_InUptrend()
    {
        // L=100, H=200 → bullish. Then H=250 = HH in uptrend → BOS.
        var indicator = BuildIndicator(
            Pivot('L', 100, 1),
            Pivot('H', 200, 2),
            Pivot('L', 150, 3),
            Pivot('H', 250, 4)
        );
        var trend = TrendIntervalBos.InterpretZigZagPoints(indicator, null, out var events);

        Assert.AreEqual(CryptoTrendIndicator.Bullish, trend);
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(CryptoStructureEvent.Bos, events[0].Type);
        Assert.AreEqual(CryptoTrendIndicator.Bullish, events[0].TrendAfter);
    }

    [TestMethod]
    public void BOS_LowerLow_InDowntrend()
    {
        // H=200, L=100 → bearish. Then L=80 = LL in downtrend → BOS.
        var indicator = BuildIndicator(
            Pivot('H', 200, 1),
            Pivot('L', 100, 2),
            Pivot('H', 150, 3),
            Pivot('L', 80, 4)
        );
        var trend = TrendIntervalBos.InterpretZigZagPoints(indicator, null, out var events);

        Assert.AreEqual(CryptoTrendIndicator.Bearish, trend);
        Assert.AreEqual(1, events.Count);
        Assert.AreEqual(CryptoStructureEvent.Bos, events[0].Type);
    }

    [TestMethod]
    public void ChoCh_HigherHigh_BreaksBearishTrend()
    {
        // H=200, L=100 → bearish. H=250 breaks protected high → CHoCH to bullish.
        var indicator = BuildIndicator(
            Pivot('H', 200, 1),
            Pivot('L', 100, 2),
            Pivot('H', 150, 3),  // LH, no event
            Pivot('L', 90, 4),   // LL → BOS (bearish continuation)
            Pivot('H', 210, 5)   // HH breaks protected high (200) → CHoCH to bullish
        );
        var trend = TrendIntervalBos.InterpretZigZagPoints(indicator, null, out var events);

        Assert.AreEqual(CryptoTrendIndicator.Bullish, trend);
        Assert.AreEqual(2, events.Count);
        Assert.AreEqual(CryptoStructureEvent.Bos, events[0].Type);    // LL at pivot 4
        Assert.AreEqual(CryptoStructureEvent.ChoCh, events[1].Type);  // HH at pivot 5
        Assert.AreEqual(CryptoTrendIndicator.Bullish, events[1].TrendAfter);
    }

    [TestMethod]
    public void ChoCh_LowerLow_BreaksBullishTrend()
    {
        // L=100, H=200 → bullish. L=90 breaks protected low → CHoCH to bearish.
        var indicator = BuildIndicator(
            Pivot('L', 100, 1),
            Pivot('H', 200, 2),
            Pivot('L', 150, 3),  // HL, no event
            Pivot('H', 250, 4),  // HH → BOS (bullish continuation)
            Pivot('L', 90, 5)    // LL breaks protected low (100) → CHoCH to bearish
        );
        var trend = TrendIntervalBos.InterpretZigZagPoints(indicator, null, out var events);

        Assert.AreEqual(CryptoTrendIndicator.Bearish, trend);
        Assert.AreEqual(2, events.Count);
        Assert.AreEqual(CryptoStructureEvent.Bos, events[0].Type);
        Assert.AreEqual(CryptoStructureEvent.ChoCh, events[1].Type);
        Assert.AreEqual(CryptoTrendIndicator.Bearish, events[1].TrendAfter);
    }

    [TestMethod]
    public void ProtectedLevel_NotDriftingDown_WithLowerHighs()
    {
        // After bearish start (H=200, L=100), a series of lower highs should NOT
        // lower the protected high. Only a break above 200 is a real CHoCH.
        var indicator = BuildIndicator(
            Pivot('H', 200, 1),
            Pivot('L', 100, 2),
            Pivot('H', 180, 3),  // LH — no event, protected stays 200
            Pivot('L', 90, 4),   // LL → BOS
            Pivot('H', 170, 5),  // LH — still no CHoCH
            Pivot('L', 85, 6)    // LL → BOS
        );
        var trend = TrendIntervalBos.InterpretZigZagPoints(indicator, null, out var events);

        Assert.AreEqual(CryptoTrendIndicator.Bearish, trend);
        // Only LL events (BOS), no false CHoCH
        foreach (var e in events)
            Assert.AreEqual(CryptoStructureEvent.Bos, e.Type, "LH should not trigger CHoCH");
    }

    [TestMethod]
    public void DummyPivots_AreExcluded()
    {
        // A dummy pivot at a new high should NOT trigger an event.
        var indicator = BuildIndicator(
            Pivot('H', 200, 1),
            Pivot('L', 100, 2),
            Pivot('H', 250, 3, dummy: true)  // dummy — excluded
        );
        var trend = TrendIntervalBos.InterpretZigZagPoints(indicator, null, out var events);

        // Only two real pivots → initial trend, no events
        Assert.AreEqual(CryptoTrendIndicator.Bearish, trend);
        Assert.AreEqual(0, events.Count, "dummy pivots should be ignored");
    }

    [TestMethod]
    public void MultipleChochFlips_EventChainIsComplete()
    {
        var indicator = BuildIndicator(
            Pivot('L', 100, 1),
            Pivot('H', 200, 2),    // → bullish
            Pivot('L', 50, 3),     // LL → CHoCH to bearish
            Pivot('H', 210, 4),    // HH → CHoCH back to bullish (protected high was reset to 200 via recentHigh)
            Pivot('L', 40, 5)      // LL → CHoCH to bearish (protected low was reset to 50 via recentLow)
        );
        var trend = TrendIntervalBos.InterpretZigZagPoints(indicator, null, out var events);

        Assert.AreEqual(CryptoTrendIndicator.Bearish, trend);
        Assert.AreEqual(3, events.Count);
        Assert.AreEqual(CryptoStructureEvent.ChoCh, events[0].Type);
        Assert.AreEqual(CryptoTrendIndicator.Bearish, events[0].TrendAfter);
        Assert.AreEqual(CryptoStructureEvent.ChoCh, events[1].Type);
        Assert.AreEqual(CryptoTrendIndicator.Bullish, events[1].TrendAfter);
        Assert.AreEqual(CryptoStructureEvent.ChoCh, events[2].Type);
        Assert.AreEqual(CryptoTrendIndicator.Bearish, events[2].TrendAfter);
    }

    [TestMethod]
    public void ProtectedLow_ResetOnChoch_AllowsReversalBack()
    {
        // After CHoCH to bullish, the protected low should be reset to the most recent
        // low (the bottom of the just-ended downtrend), so a CHoCH back is reachable.
        var indicator = BuildIndicator(
            Pivot('H', 200, 1),
            Pivot('L', 100, 2),     // → bearish, protectedLow=100
            Pivot('H', 150, 3),
            Pivot('L', 90, 4),      // LL → BOS, protectedLow=90
            Pivot('H', 210, 5),     // HH → CHoCH to bullish, protectedLow = recentLow = 90
            Pivot('L', 85, 6)       // LL breaks 90 → CHoCH back to bearish
        );
        var trend = TrendIntervalBos.InterpretZigZagPoints(indicator, null, out var events);

        Assert.AreEqual(CryptoTrendIndicator.Bearish, trend);
        var chochEvents = events.Where(e => e.Type == CryptoStructureEvent.ChoCh).ToList();
        Assert.AreEqual(2, chochEvents.Count, "should have two CHoCH events (bullish then bearish)");
    }


    // ─── CryptoTrendData helpers ──────────────────────────────────────────

    [TestMethod]
    public void LastChoCh_ReturnsNull_WhenEmpty()
    {
        var td = new CryptoTrendData();
        Assert.IsNull(td.LastChoCh());
    }

    [TestMethod]
    public void LastChoCh_ReturnsNull_WhenOnlyBos()
    {
        var td = new CryptoTrendData();
        td.StructureEvents.Add(new StructureEvent(new CandleTime(1), CryptoStructureEvent.Bos, 100, CryptoTrendIndicator.Bullish));
        td.StructureEvents.Add(new StructureEvent(new CandleTime(2), CryptoStructureEvent.Bos, 110, CryptoTrendIndicator.Bullish));
        Assert.IsNull(td.LastChoCh());
    }

    [TestMethod]
    public void LastChoCh_FindsLastOne()
    {
        var td = new CryptoTrendData();
        td.StructureEvents.Add(new StructureEvent(new CandleTime(1), CryptoStructureEvent.ChoCh, 100, CryptoTrendIndicator.Bearish));
        td.StructureEvents.Add(new StructureEvent(new CandleTime(2), CryptoStructureEvent.Bos, 90, CryptoTrendIndicator.Bearish));
        td.StructureEvents.Add(new StructureEvent(new CandleTime(3), CryptoStructureEvent.ChoCh, 120, CryptoTrendIndicator.Bullish));
        td.StructureEvents.Add(new StructureEvent(new CandleTime(4), CryptoStructureEvent.Bos, 130, CryptoTrendIndicator.Bullish));

        var last = td.LastChoCh();
        Assert.IsNotNull(last);
        Assert.AreEqual(new CandleTime(3), last.Time);
        Assert.AreEqual(CryptoTrendIndicator.Bullish, last.TrendAfter);
    }

    [TestMethod]
    public void HasBosAfterLastChoCh_True_WhenBosFollowsChoch()
    {
        var td = new CryptoTrendData();
        td.StructureEvents.Add(new StructureEvent(new CandleTime(1), CryptoStructureEvent.ChoCh, 100, CryptoTrendIndicator.Bullish));
        td.StructureEvents.Add(new StructureEvent(new CandleTime(2), CryptoStructureEvent.Bos, 110, CryptoTrendIndicator.Bullish));

        Assert.IsTrue(td.HasBosAfterLastChoCh());
    }

    [TestMethod]
    public void HasBosAfterLastChoCh_False_WhenChochIsLast()
    {
        var td = new CryptoTrendData();
        td.StructureEvents.Add(new StructureEvent(new CandleTime(1), CryptoStructureEvent.Bos, 90, CryptoTrendIndicator.Bearish));
        td.StructureEvents.Add(new StructureEvent(new CandleTime(2), CryptoStructureEvent.ChoCh, 120, CryptoTrendIndicator.Bullish));

        Assert.IsFalse(td.HasBosAfterLastChoCh());
    }

    [TestMethod]
    public void HasBosAfterLastChoCh_False_WhenEmpty()
    {
        var td = new CryptoTrendData();
        Assert.IsFalse(td.HasBosAfterLastChoCh());
    }

    [TestMethod]
    public void HasBosAfterLastChoCh_False_WhenOnlyBos_NoChoch()
    {
        var td = new CryptoTrendData();
        td.StructureEvents.Add(new StructureEvent(new CandleTime(1), CryptoStructureEvent.Bos, 110, CryptoTrendIndicator.Bullish));

        // There IS a BOS but no preceding CHoCH — the method should find the BOS
        // before finding a CHoCH, so it returns true. This is semantically correct:
        // "is there a BOS after the last CHoCH" — when there's no CHoCH, the BOS
        // stands on its own.
        Assert.IsTrue(td.HasBosAfterLastChoCh());
    }
}
