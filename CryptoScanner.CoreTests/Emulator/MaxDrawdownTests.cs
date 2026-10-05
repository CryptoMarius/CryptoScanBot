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


    // ═══════════════════════════════════════════════════════════════════════
    //  The same fall in USDT, with the day it was reached (database version 103)
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// The number the report leads with: 130 down to 65 is 65 USDT, on the fifth day. As a
    /// percentage of 20.000 a fall like run 1860's 217 USDT read as 1,08% and was overlooked.
    /// </summary>
    [TestMethod]
    public void OpenDrawdown_IsTheDeepestFallInMoney_WithItsDay()
    {
        (decimal amount, DateTime? day) = EmulatorDb.OpenDrawdown(Series(100m, 120m, 90m, 130m, 65m));
        Assert.AreEqual(65m, amount);
        Assert.AreEqual(new DateTime(2026, 1, 5), day);
    }


    [TestMethod]
    public void OpenDrawdown_OfASeriesThatOnlyRises_IsZeroWithoutADay()
    {
        (decimal amount, DateTime? day) = EmulatorDb.OpenDrawdown(Series(100m, 110m, 125m));
        Assert.AreEqual(0m, amount);
        Assert.IsNull(day);
    }


    [TestMethod]
    public void EquityCurveJson_KeepsEveryDayWithItsValue()
    {
        string json = EmulatorDb.EquityCurveJson(Series(20000m, 19950.126m));
        using var document = System.Text.Json.JsonDocument.Parse(json);
        var rows = document.RootElement.GetProperty("rows");
        Assert.AreEqual(2, rows.GetArrayLength());
        Assert.AreEqual("2026-01-02", rows[1][0].GetString());
        Assert.AreEqual(19950.13m, rows[1][1].GetDecimal());
    }
}
