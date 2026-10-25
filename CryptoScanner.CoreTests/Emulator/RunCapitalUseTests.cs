using CryptoScanner.Core.Context;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Emulator.ViewModels;

using Dapper;

using Microsoft.Data.Sqlite;

using System.Globalization;
using System.Text.Json;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// How much of the account was at work in trades (<see cref="RunCapitalUse"/>, database version 110):
/// the average capital in trades, the exposure against the account value, and the time in market.
/// <para>
/// The exact figures come from the fills: every entry and DCA counts from the moment it filled until
/// its position closed. The estimate for the older runs comes from the digest, which counts the full
/// invested amount from the entry on and therefore lands higher. The tests run the production query
/// against hand-built Position and PositionStep tables.
/// </para>
/// </summary>
[TestClass]
public class RunCapitalUseTests
{
    private const int RunId = 7;
    private static readonly DateTime FromDate = new(2026, 1, 1);
    private static readonly DateTime ToDate = new(2026, 1, 11);

    /// <summary>Only the columns the calculation reads. Times are TEXT and decimals TEXT, as in the real schema.</summary>
    private static SqliteConnection CreateDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.Execute(
            "create table Position (Id integer primary key, EmulatorRunId integer, Side integer, CloseTime text, Invested text)");
        connection.Execute(
            "create table PositionStep (Id integer primary key, PositionId integer, CloseTime text, Status integer, " +
            "Side integer, QuoteQuantityFilled text)");
        return connection;
    }

    private static int AddPosition(SqliteConnection connection, CryptoTradeSide side, string? closeTime, double invested, int runId = RunId)
    {
        return connection.ExecuteScalar<int>(
            "insert into Position (EmulatorRunId, Side, CloseTime, Invested) values (@runId, @side, @closeTime, @invested); " +
            "select last_insert_rowid()",
            new { runId, side = (int)side, closeTime, invested = invested.ToString(CultureInfo.InvariantCulture) });
    }

    private static void AddStep(SqliteConnection connection, int positionId, string closeTime, CryptoOrderStatus status,
        CryptoOrderSide side, double quote)
    {
        connection.Execute(
            "insert into PositionStep (PositionId, CloseTime, Status, Side, QuoteQuantityFilled) " +
            "values (@positionId, @closeTime, @status, @side, @quote)",
            new { positionId, closeTime, status = (int)status, side = (int)side, quote = quote.ToString(CultureInfo.InvariantCulture) });
    }

    private static double Minutes(DateTime moment) => (moment - CandleTime.Epoch).TotalMinutes;


    [TestMethod]
    public void FillsCountFromTheMomentTheyFilled()
    {
        using var connection = CreateDatabase();

        // A long from 01-01 to 01-05: entry 100 at the start (4 days), DCA 200 on 01-03 (2 days).
        int longId = AddPosition(connection, CryptoTradeSide.Long, "2026-01-05 00:00:00", 300);
        AddStep(connection, longId, "2026-01-01 00:00:00", CryptoOrderStatus.Filled, CryptoOrderSide.Buy, 100);
        AddStep(connection, longId, "2026-01-03 00:00:00", CryptoOrderStatus.Filled, CryptoOrderSide.Buy, 200);
        // A DCA that was cancelled when the position closed still carries a QuoteQuantityFilled; it was never bought.
        AddStep(connection, longId, "2026-01-05 00:00:00", CryptoOrderStatus.PositionClosed, CryptoOrderSide.Buy, 400);
        // The take profit is the exit, not capital going in.
        AddStep(connection, longId, "2026-01-05 00:00:00", CryptoOrderStatus.Filled, CryptoOrderSide.Sell, 330);

        // A short entered on 01-07 and still open at the end: 50 for 4 days.
        int shortId = AddPosition(connection, CryptoTradeSide.Short, null, 50);
        AddStep(connection, shortId, "2026-01-07 00:00:00", CryptoOrderStatus.Filled, CryptoOrderSide.Sell, 50);

        // Another run's fill does not count.
        int otherId = AddPosition(connection, CryptoTradeSide.Long, "2026-01-10 00:00:00", 1000, runId: 8);
        AddStep(connection, otherId, "2026-01-01 00:00:00", CryptoOrderStatus.Filled, CryptoOrderSide.Buy, 1000);

        RunCapitalUse use = RunCapitalUse.FromFills(connection, RunId, FromDate, ToDate, averageAccountValue: 10000m)!.Value;

        Assert.AreEqual(100m, use.AvgInvested, "(100 x 4 + 200 x 2 + 50 x 4) / 10 dagen");
        Assert.AreEqual(1m, use.ExposurePercentage!.Value, "100 van een rekening van 10.000 is 1%");
        Assert.AreEqual(80m, use.TimeInMarketPercentage, "4 + 4 van de 10 dagen stond er iets open");
        Assert.IsFalse(use.Estimated);
    }


    [TestMethod]
    public void DigestEstimateCountsTheWholeStakeFromTheEntry()
    {
        // The same two positions as in the digest: it only knows the full invested amount and the
        // moment the entry was placed, so the DCA counts from 01-01 as well - 140 instead of 100.
        string digest = JsonSerializer.Serialize(new
        {
            v = 1,
            cols = new[] { "open", "close", "invested" },
            rows = new object?[][]
            {
                [Minutes(new DateTime(2026, 1, 1)), Minutes(new DateTime(2026, 1, 5)), 300.0],
                [Minutes(new DateTime(2026, 1, 7)), null, 50.0],
                // A timed-out entry: never invested, never counted.
                [Minutes(new DateTime(2026, 1, 2)), Minutes(new DateTime(2026, 1, 3)), 0.0],
            },
        });

        RunCapitalUse use = RunCapitalUse.FromDigest(digest, FromDate, ToDate, averageAccountValue: 10000m)!.Value;

        Assert.AreEqual(140m, use.AvgInvested, "(300 x 4 + 50 x 4) / 10 dagen");
        Assert.AreEqual(1.4m, use.ExposurePercentage!.Value);
        Assert.AreEqual(80m, use.TimeInMarketPercentage);
        Assert.IsTrue(use.Estimated, "uit de digest is altijd een schatting");
    }


    [TestMethod]
    public void OverlappingPositionsCountOnceForTheTimeInMarket()
    {
        // Open 01-01..01-05 and 01-03..01-07: six days with something open, not eight.
        List<RunCapitalUse.Piece> pieces =
        [
            new(new DateTime(2026, 1, 1), new DateTime(2026, 1, 5), 10),
            new(new DateTime(2026, 1, 3), new DateTime(2026, 1, 7), 10),
        ];

        RunCapitalUse use = RunCapitalUse.Compute(pieces, FromDate, ToDate, averageAccountValue: null, estimated: false)!.Value;

        Assert.AreEqual(60m, use.TimeInMarketPercentage);
        Assert.AreEqual(8m, use.AvgInvested, "(10 x 4 + 10 x 4) / 10 dagen: het geld telt wel dubbel");
        Assert.IsNull(use.ExposurePercentage, "zonder accountwaarde geen exposure");
    }


    [TestMethod]
    public void PiecesAreCutToThePeriod()
    {
        // A position opened before the period and one still running after it only count inside it.
        List<RunCapitalUse.Piece> pieces =
        [
            new(new DateTime(2025, 12, 30), new DateTime(2026, 1, 2), 100),
            new(new DateTime(2026, 1, 10), new DateTime(2026, 1, 15), 100),
        ];

        RunCapitalUse use = RunCapitalUse.Compute(pieces, FromDate, ToDate, averageAccountValue: 1000m, estimated: false)!.Value;

        Assert.AreEqual(20m, use.AvgInvested, "(100 x 1 + 100 x 1) / 10 dagen");
        Assert.AreEqual(20m, use.TimeInMarketPercentage);
    }


    [TestMethod]
    public void RunWithoutTradesHadNothingAtWork()
    {
        RunCapitalUse use = RunCapitalUse.Compute([], FromDate, ToDate, averageAccountValue: 1000m, estimated: false)!.Value;

        Assert.AreEqual(0m, use.AvgInvested);
        Assert.AreEqual(0m, use.ExposurePercentage!.Value);
        Assert.AreEqual(0m, use.TimeInMarketPercentage);
        Assert.IsNull(RunCapitalUse.Compute([], ToDate, FromDate, 1000m, false), "een lege periode zegt niets");
    }


    [TestMethod]
    public void AverageAccountValueReadsTheEquityCurve()
    {
        // Round values come out of the curve as JSON integers, the rest as reals.
        const string curve = "{\"v\":1,\"cols\":[\"date\",\"value\"],\"rows\":[[\"2026-01-01\",100],[\"2026-01-02\",300.5]]}";

        Assert.AreEqual(200.25m, RunCapitalUse.AverageAccountValue(curve));
        Assert.IsNull(RunCapitalUse.AverageAccountValue(null));
        Assert.IsNull(RunCapitalUse.AverageAccountValue("{\"v\":1,\"cols\":[\"date\",\"value\"],\"rows\":[]}"));
    }


    [TestMethod]
    public void RiskAdjustedReturnIsTotalPnlOverTheCapitalAtWork()
    {
        // Total PnL 80 on an average of 400 in trades: the money at work made 20%.
        RunRow row = new() { Profit = 50m, EquityStart = 10000m, EquityEnd = 10080m, AvgInvested = 400m, ExposurePercentage = 4m };

        Assert.AreEqual(20m, row.RiskAdjustedReturn);
        Assert.AreEqual(20m.ToString("N2") + "%", row.RiskAdjustedReturnText);
        Assert.AreEqual(1.0, row.CapitalUseOpacity);
        Assert.IsNull(row.CapitalUseReason);

        // An older run: the estimate is greyed and says so, also once it is pasted elsewhere.
        row.CapitalUseEstimated = true;
        Assert.AreEqual("ca. " + 4m.ToString("N2") + "%", row.ExposurePercentageText);
        Assert.AreEqual(0.4, row.CapitalUseOpacity);
        Assert.IsNotNull(row.CapitalUseReason);

        // Without an equity curve the closed profit is the result, like the recovery factor.
        RunRow closedOnly = new() { Profit = 50m, AvgInvested = 400m };
        Assert.AreEqual(12.5m, closedOnly.RiskAdjustedReturn);
        Assert.AreEqual("—", new RunRow { Profit = 50m }.RiskAdjustedReturnText);
    }
}
