using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Model;

/// <summary>
/// The "already asked the exchange for this period" note on CryptoSymbolInterval. It exists because
/// the zone engine cannot tell from the candles themselves whether it still has to fetch: an exchange
/// that skips a minute without trades (Bitvavo) leaves a hole that never fills, so the check finds the
/// same hole on every recalculation and downloads the same history again.
///
/// Two rules make the note safe, and both are tested here:
///   - it describes ONE uninterrupted period, so two fetches that do not connect never add up to a
///     claim about the stretch in between;
///   - it can only get SHORTER when candles are removed, so "never fetched again" cannot happen.
/// </summary>
[TestClass]
public class SymbolIntervalHistoryAskedTests
{
    private static CryptoSymbolInterval CreateSymbolInterval()
    {
        return new CryptoSymbolInterval
        {
            Interval = new CryptoInterval { Id = 1, Name = "1m", Duration = 1 },
            IntervalPeriod = CryptoIntervalPeriod.interval1m,
        };
    }

    private static CandleTime Time(uint minutes) => new(minutes);


    [TestMethod]
    public void NothingRememberedMeansEverythingHasToBeAsked()
    {
        CryptoSymbolInterval symbolInterval = CreateSymbolInterval();

        Assert.IsFalse(symbolInterval.HistoryWasAsked(Time(100), Time(200)));
    }


    [TestMethod]
    public void OnlyAPeriodInsideTheRememberedOneCountsAsAsked()
    {
        CryptoSymbolInterval symbolInterval = CreateSymbolInterval();
        symbolInterval.RememberHistoryAsked(Time(100), Time(200));

        Assert.IsTrue(symbolInterval.HistoryWasAsked(Time(120), Time(180)), "inside");
        Assert.IsTrue(symbolInterval.HistoryWasAsked(Time(100), Time(200)), "exactly the same");
        Assert.IsFalse(symbolInterval.HistoryWasAsked(Time(90), Time(180)), "starts earlier");
        Assert.IsFalse(symbolInterval.HistoryWasAsked(Time(120), Time(210)), "ends later");
    }


    [TestMethod]
    public void TouchingPeriodsAreMergedIntoOne()
    {
        CryptoSymbolInterval symbolInterval = CreateSymbolInterval();
        symbolInterval.RememberHistoryAsked(Time(100), Time(200));
        symbolInterval.RememberHistoryAsked(Time(200), Time(300));

        Assert.IsTrue(symbolInterval.HistoryWasAsked(Time(100), Time(300)));
    }


    [TestMethod]
    public void PeriodsWithAGapBetweenThemAreNotMerged()
    {
        CryptoSymbolInterval symbolInterval = CreateSymbolInterval();
        symbolInterval.RememberHistoryAsked(Time(100), Time(200));
        symbolInterval.RememberHistoryAsked(Time(400), Time(500));

        // The stretch 200..400 was never requested, so the note must not cover it - and it must not
        // cover the older period either now that it describes the newer one.
        Assert.IsFalse(symbolInterval.HistoryWasAsked(Time(100), Time(500)), "across the gap");
        Assert.IsFalse(symbolInterval.HistoryWasAsked(Time(250), Time(300)), "inside the gap");
        Assert.IsFalse(symbolInterval.HistoryWasAsked(Time(100), Time(200)), "the older period");
        Assert.IsTrue(symbolInterval.HistoryWasAsked(Time(400), Time(500)), "the newer period");
    }


    [TestMethod]
    public void TheSearchStartsAfterWhatWasAlreadyAsked()
    {
        CryptoSymbolInterval symbolInterval = CreateSymbolInterval();
        symbolInterval.RememberHistoryAsked(Time(100), Time(200));

        // The hour boundary moves the window forward: only the tail beyond 200 is new.
        Assert.AreEqual(Time(200), symbolInterval.SkipHistoryAlreadyAsked(Time(160)), "inside the note");
        Assert.AreEqual(Time(200), symbolInterval.SkipHistoryAlreadyAsked(Time(100)), "at the start of the note");
        Assert.AreEqual(Time(90), symbolInterval.SkipHistoryAlreadyAsked(Time(90)), "starts before the note");
        Assert.AreEqual(Time(260), symbolInterval.SkipHistoryAlreadyAsked(Time(260)), "starts after the note");
    }


    [TestMethod]
    public void ASlidingWindowKeepsExtendingTheSameNote()
    {
        CryptoSymbolInterval symbolInterval = CreateSymbolInterval();

        // Three rounds an hour apart, each asking for the last 500 minutes. Round two and three only
        // have to look at their own last hour, and the note grows along instead of starting over.
        symbolInterval.RememberHistoryAsked(Time(1000), Time(1500));
        Assert.AreEqual(Time(1500), symbolInterval.SkipHistoryAlreadyAsked(Time(1060)));

        symbolInterval.RememberHistoryAsked(Time(1060), Time(1560));
        Assert.AreEqual(Time(1560), symbolInterval.SkipHistoryAlreadyAsked(Time(1120)));

        symbolInterval.RememberHistoryAsked(Time(1120), Time(1620));
        Assert.IsTrue(symbolInterval.HistoryWasAsked(Time(1000), Time(1620)), "still one uninterrupted period");
    }


    [TestMethod]
    public void RemovingCandlesInTheMiddleShortensTheNote()
    {
        CryptoSymbolInterval symbolInterval = CreateSymbolInterval();
        symbolInterval.RememberHistoryAsked(Time(100), Time(200));

        symbolInterval.ForgetHistoryUpTo(Time(150));

        Assert.IsFalse(symbolInterval.HistoryWasAsked(Time(100), Time(200)), "the removed part must be fetched again");
        Assert.IsTrue(symbolInterval.HistoryWasAsked(Time(151), Time(200)), "the part after the removal still stands");
    }


    [TestMethod]
    public void RemovingUpToTheEndClearsTheNote()
    {
        CryptoSymbolInterval symbolInterval = CreateSymbolInterval();
        symbolInterval.RememberHistoryAsked(Time(100), Time(200));

        symbolInterval.ForgetHistoryUpTo(Time(200));

        Assert.IsFalse(symbolInterval.HistoryWasAsked(Time(100), Time(200)));
        Assert.IsFalse(symbolInterval.HistoryAskedFrom.HasValue);
    }


    [TestMethod]
    public void RemovingBelowTheNoteLeavesItAlone()
    {
        CryptoSymbolInterval symbolInterval = CreateSymbolInterval();
        symbolInterval.RememberHistoryAsked(Time(100), Time(200));

        symbolInterval.ForgetHistoryUpTo(Time(50));

        Assert.IsTrue(symbolInterval.HistoryWasAsked(Time(100), Time(200)));
    }


    [TestMethod]
    public void RemovingCandlesForAnEmptyNoteChangesNothing()
    {
        CryptoSymbolInterval symbolInterval = CreateSymbolInterval();

        symbolInterval.ForgetHistoryUpTo(Time(150));

        Assert.IsFalse(symbolInterval.HistoryAskedFrom.HasValue);
        Assert.IsFalse(symbolInterval.HistoryAskedTo.HasValue);
    }
}
