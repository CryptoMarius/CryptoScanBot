using CryptoScanner.Core.Trader;
using CryptoScanner.Emulator.Engine;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The deepest capital drawdown of a run (open point 47): the largest fall from a running high, in
/// percent of that high, over the daily capital totals.
/// </summary>
[TestClass]
public class MaxDrawdownTests
{
    private static List<AssetSnapshotTools.AssetSnapshotDay> Series(params decimal[] values)
        => [.. values.Select((v, i) => new AssetSnapshotTools.AssetSnapshotDay { Date = new DateTime(2026, 1, 1).AddDays(i), Value = v })];

    [TestMethod]
    public void ASeriesThatOnlyRises_HasNoDrawdown()
        => Assert.AreEqual(0m, EmulatorDb.MaxDrawdownPercentage(Series(100m, 110m, 125m)));

    [TestMethod]
    public void TheDeepestFallFromAnEarlierHighCounts_NotTheFirst()
        // 120 to 90 is 25%, 130 to 65 is 50%: the deeper one is the answer
        => Assert.AreEqual(50m, EmulatorDb.MaxDrawdownPercentage(Series(100m, 120m, 90m, 130m, 65m)));

    [TestMethod]
    public void AFallBeforeTheFirstHigh_IsMeasuredFromThatHigh()
        // The high is the first day here, so the fall is 100 to 80: 20%
        => Assert.AreEqual(20m, EmulatorDb.MaxDrawdownPercentage(Series(100m, 80m, 95m)));

    [TestMethod]
    public void AnEmptySeries_GivesZero()
        => Assert.AreEqual(0m, EmulatorDb.MaxDrawdownPercentage(Series()));
}
