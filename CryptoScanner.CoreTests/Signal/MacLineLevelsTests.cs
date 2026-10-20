using CryptoScanner.Analyzers.Mac.Signal;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// The DCAs on the cloud lines and the stop on the line beyond the last one (mac), and the DCA
/// ladder a signal hands over to its position.
/// </summary>
[TestClass]
public class MacLineLevelsTests
{
    [TestMethod]
    public void LineDistancesBehind_LongTakesTheLinesUnderTheEntry_NearToFar()
    {
        // Entry 100: lines at 98 and 95 are behind a long, 101 is not
        List<decimal> distances = MacPriceLevels.LineDistancesBehind(CryptoTradeSide.Long, 100m, [95.0, 101.0, 98.0, null]);

        CollectionAssert.AreEqual(new[] { 2m, 5m }, distances);
    }


    [TestMethod]
    public void LineDistancesBehind_ShortTakesTheLinesAboveTheEntry()
    {
        List<decimal> distances = MacPriceLevels.LineDistancesBehind(CryptoTradeSide.Short, 100m, [95.0, 101.0, 104.0]);

        CollectionAssert.AreEqual(new[] { 1m, 4m }, distances);
    }


    [TestMethod]
    public void PickDcaDistances_SkipsLinesTooCloseToTheEntryOrToEachOther()
    {
        // 0.3 is too close to the entry, 2.2 too close to 2.0
        List<decimal> picked = MacPriceLevels.PickDcaDistances([0.3m, 2.0m, 2.2m, 4.0m, 7.0m], 2, 0.5m);

        CollectionAssert.AreEqual(new[] { 2.0m, 4.0m }, picked);
    }


    [TestMethod]
    public void StopDistanceOnLineBeyond_TakesTheFirstLineBeyondTheDeepestDcaPlusTheMargin()
    {
        List<decimal> lines = [2.0m, 4.0m, 4.2m, 7.0m];

        // Deepest DCA at 4: 4.2 is within the gap, 7 is the stop line
        Assert.AreEqual(7.5m, MacPriceLevels.StopDistanceOnLineBeyond(lines, 4.0m, 0.5m, 0.5m));
        // Without DCAs the first line behind the entry
        Assert.AreEqual(2.5m, MacPriceLevels.StopDistanceOnLineBeyond(lines, 0m, 0.5m, 0.5m));
        // Nothing beyond the last line
        Assert.IsNull(MacPriceLevels.StopDistanceOnLineBeyond(lines, 7.0m, 0.5m, 0.5m));
    }


    [TestMethod]
    public void DcaLadder_RoundTripsAndRejectsGarbage()
    {
        List<CryptoDcaEntry> ladder =
        [
            new() { Percentage = 2.13m, Factor = 200m },
            new() { Percentage = 4.5m, Factor = 400m },
        ];

        string? text = DcaLadder.Format(ladder);
        Assert.AreEqual("2.13:200;4.5:400", text);

        List<CryptoDcaEntry>? back = DcaLadder.Parse(text);
        Assert.IsNotNull(back);
        Assert.AreEqual(2, back.Count);
        Assert.AreEqual(4.5m, back[1].Percentage);
        Assert.AreEqual(400m, back[1].Factor);

        Assert.IsNull(DcaLadder.Format([]));
        Assert.IsNull(DcaLadder.Parse(null));
        Assert.IsNull(DcaLadder.Parse("2.13"));
    }

    [TestMethod]
    public void PickDcaDistancesKeepingStopLine_LeavesOneLineForTheStop()
    {
        // Two lines behind the entry: one DCA on the first, the second stays free for the stop
        CollectionAssert.AreEqual(new[] { 2.0m }, MacPriceLevels.PickDcaDistancesKeepingStopLine([2.0m, 4.0m], 2, 0.5m, 0.5m));
        // One line: no DCA on a line, the line is the stop
        Assert.AreEqual(0, MacPriceLevels.PickDcaDistancesKeepingStopLine([2.0m], 2, 0.5m, 0.5m).Count);
        // Enough lines: both DCAs and a line beyond them
        CollectionAssert.AreEqual(new[] { 1.0m, 2.0m }, MacPriceLevels.PickDcaDistancesKeepingStopLine([1.0m, 2.0m, 4.0m], 2, 0.5m, 0.5m));
    }
}
