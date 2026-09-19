using CryptoScanner.Analyzers.Mac;
using CryptoScanner.Core.Settings.Strategy;
using CryptoScanner.Core.Signal;

namespace CryptoScanner.CoreTests.Settings;

/// <summary>
/// The interval list moved from the three zone strategies to the base class, so that every strategy
/// has one. Two things had to survive that move: the zone strategies keep the intervals they were
/// born with, and every other strategy starts empty - because empty is what means "the same as the
/// side", and a strategy that started out with a list would quietly stop running on the rest.
/// </summary>
[TestClass]
public class StrategyIntervalListTests
{
    [TestMethod]
    public void TheZoneStrategiesKeepTheirOwnIntervals()
    {
        CollectionAssert.AreEquivalent(new List<string> { "1h" },
            new SettingsSignalStrategyDlz().IntervalList, "dlz");

        CollectionAssert.AreEquivalent(new List<string> { "1h", "4h", "1d" },
            new SettingsSignalStrategyFvg().IntervalList, "fvg");

        CollectionAssert.AreEquivalent(new List<string> { "1h" },
            new SettingsSignalStrategySmc().IntervalList, "smc");
    }


    /// <summary>
    /// A strategy with a list of its own runs on exactly that list, whether or not the side happens
    /// to tick those intervals. That is allowed because the candles do not depend on the ticks:
    /// CandleTools builds and fetches every interval regardless. Only the indicator preparation
    /// followed the ticks, and that reads this same list now.
    /// </summary>
    [TestMethod]
    public void AStrategyWithItsOwnListDoesNotHaveToStayInsideTheSide()
    {
        var side = new List<string> { "5m", "15m", "1h" };

        // No plugin by this name, so there are no settings and the side decides.
        var unknown = new AlgorithmDefinition
        {
            Name = "not-a-strategy",
            AnalyzeLongType = typeof(object),
            AnalyzeShortType = typeof(object),
        };
        CollectionAssert.AreEqual(side, SignalExecute.IntervalsFor(unknown, side),
            "without settings of its own a strategy follows the side");
    }


    [TestMethod]
    public void EveryOtherStrategyStartsWithoutOne()
    {
        Assert.AreEqual(0, new MacSettings().IntervalList.Count,
            "an empty list is what makes a strategy follow the side it is ticked on");
        Assert.AreEqual(0, new SettingsSignalStrategyBase().IntervalList.Count);
    }
}
