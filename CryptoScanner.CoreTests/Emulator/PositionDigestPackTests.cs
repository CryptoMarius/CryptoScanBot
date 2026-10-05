using CryptoScanner.Core.Context;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The position digest stored compressed (database version 104) and the risk checks that are kept
/// as plain columns because a query cannot look inside the compressed digest any more.
/// </summary>
[TestClass]
public class PositionDigestPackTests
{
    private static long Minutes(DateTime moment) => (long)(moment - CandleTime.Epoch).TotalMinutes;


    private static string Digest(params (DateTime Open, DateTime? Close, double? Profit, double Invested)[] rows)
    {
        string body = string.Join(",", rows.Select(r =>
            $"[1,0,{Minutes(r.Open)},{(r.Close == null ? "null" : Minutes(r.Close.Value).ToString())}," +
            $"{(r.Profit == null ? "null" : r.Profit.Value.ToString(System.Globalization.CultureInfo.InvariantCulture))}," +
            $"{r.Invested.ToString(System.Globalization.CultureInfo.InvariantCulture)},1,2]"));
        return "{\"v\":1,\"cols\":[\"sym\",\"side\",\"open\",\"close\",\"profit\",\"invested\",\"dca\",\"status\"],\"rows\":[" + body + "]}";
    }


    [TestMethod]
    public void PackedDigestComesBackUnchanged()
    {
        string json = Digest((new DateTime(2026, 1, 3), new DateTime(2026, 1, 5), 1.25, 15));

        string? packed = PositionDigest.Pack(json);

        Assert.IsTrue(packed!.StartsWith(PositionDigest.CompressedPrefix));
        Assert.AreEqual(json, PositionDigest.Unpack(packed));
    }


    /// <summary>The migration may meet a digest twice (interrupted and restarted): packing must not pack again.</summary>
    [TestMethod]
    public void PackingIsIdempotentAndPlainJsonPassesUnpack()
    {
        string json = Digest((new DateTime(2026, 1, 3), new DateTime(2026, 1, 5), 1.25, 15));
        string? packed = PositionDigest.Pack(json);

        Assert.AreEqual(packed, PositionDigest.Pack(packed));
        Assert.AreEqual(json, PositionDigest.Unpack(json));
        Assert.IsNull(PositionDigest.Pack(null));
        Assert.AreEqual("", PositionDigest.Unpack(""));
    }


    /// <summary>A real digest of a few thousand rows is the point: it has to shrink a lot.</summary>
    [TestMethod]
    public void ALargeDigestShrinks()
    {
        var rows = Enumerable.Range(0, 3000)
            .Select(i => (new DateTime(2026, 1, 1).AddHours(i), (DateTime?)new DateTime(2026, 1, 1).AddHours(i + 5), (double?)(i % 7 - 3) * 0.37, 15.0))
            .ToArray();
        string json = Digest(rows);

        string? packed = PositionDigest.Pack(json);

        Assert.IsTrue(packed!.Length * 3 < json.Length, $"{packed.Length} against {json.Length}");
    }


    [TestMethod]
    public void RiskMetricsFollowTheReportRules()
    {
        string json = Digest(
            // January: +2 and -1 closes in profit
            (new DateTime(2026, 1, 2), new DateTime(2026, 1, 3), 2.0, 10),
            (new DateTime(2026, 1, 4), new DateTime(2026, 1, 14), -1.0, 10),   // loser open 10 days, -10%
            // February: a loser of -6 on 4 invested (-150%), open 2 days
            (new DateTime(2026, 2, 1), new DateTime(2026, 2, 3), -6.0, 4),
            // still open and a cancelled entry: both ignored
            (new DateTime(2026, 2, 5), null, null, 10),
            (new DateTime(2026, 2, 6), new DateTime(2026, 2, 6), 0.0, 0));

        RunRiskMetrics? metrics = RunRiskMetrics.FromDigest(json, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1));

        Assert.IsNotNull(metrics);
        Assert.AreEqual(1, metrics.Value.MonthsInProfit);        // January only
        Assert.AreEqual(3, metrics.Value.MonthsTotal);           // January, February, March
        Assert.AreEqual(10.0m, metrics.Value.LongestLoserDays);
        Assert.AreEqual(-150.0m, metrics.Value.WorstPositionPercentage);
        Assert.AreEqual(0m, metrics.Value.ProfitWithoutBestTen);        // only three trades, all among the best ten
    }


    /// <summary>The settings stored flat: same content, no line breaks, "+" and accents kept readable.</summary>
    [TestMethod]
    public void FlattenedJsonKeepsItsContent()
    {
        string indented = "{\n  \"Label\": \"long ma200+ café\",\n  \"List\": [ 1, 2.50 ],\n  \"On\": true\n}";

        string? flat = JsonCompact.Flatten(indented);

        Assert.AreEqual("{\"Label\":\"long ma200+ café\",\"List\":[1,2.50],\"On\":true}", flat);
        Assert.AreEqual("not json", JsonCompact.Flatten("not json"));
        Assert.IsNull(JsonCompact.Flatten(null));
    }


    [TestMethod]
    public void NoClosedTradesMeansNoMetrics()
    {
        string json = Digest((new DateTime(2026, 2, 5), null, null, 10));

        Assert.IsNull(RunRiskMetrics.FromDigest(json, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1)));
        Assert.IsNull(RunRiskMetrics.FromDigest(null, new DateTime(2026, 1, 1), new DateTime(2026, 4, 1)));
    }
}
