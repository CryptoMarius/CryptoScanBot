using CryptoScanner.Emulator.Engine;

using Dapper;

using Microsoft.Data.Sqlite;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// Peak capital and the peak number of simultaneously open positions, as computed by
/// <see cref="EmulatorDb.PeakExposureSql"/> at the end of a run.
/// <para>
/// This is the number that answers "could an account have run this", and it cannot be recovered
/// afterwards: the paper Asset balance carries over between runs, so the only source is the open
/// and close times of the positions themselves. Once those are archived away it is gone - which is
/// exactly what happened to the 6.366 runs in Session0. The tests run the production query text
/// against a hand-built position table, so a change to the SQL has to keep answering these.
/// </para>
/// </summary>
[TestClass]
public class RunSummaryPeakTests
{
    private const int RunId = 7;

    /// <summary>
    /// A position table with only the columns the summary reads. Times are TEXT in the real schema
    /// too ("yyyy-MM-dd HH:mm:ss"), which sorts correctly as text; Invested and Profit are TEXT
    /// decimals that the query CASTs to REAL.
    /// </summary>
    private static SqliteConnection CreateDatabase()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        connection.Execute(
            "create table position (Id integer primary key, EmulatorRunId integer, Side integer, " +
            "CreateTime text, CloseTime text, Invested text, Profit text, PartCount integer)");
        return connection;
    }

    private static void AddPosition(SqliteConnection connection, string createTime, string? closeTime,
        double invested, int runId = RunId)
    {
        connection.Execute(
            "insert into position (EmulatorRunId, Side, CreateTime, CloseTime, Invested, Profit, PartCount) " +
            "values (@runId, 0, @createTime, @closeTime, @invested, '0', 0)",
            new { runId, createTime, closeTime, invested = invested.ToString(System.Globalization.CultureInfo.InvariantCulture) });
    }

    private static EmulatorDb.PeakRow Peak(SqliteConnection connection, int id = RunId) =>
        connection.QueryFirst<EmulatorDb.PeakRow>(EmulatorDb.PeakExposureSql, new { id });


    [TestMethod]
    public void OverlappingPositionsStack()
    {
        using var connection = CreateDatabase();
        // Three positions, all open together between 12:00 and 12:30.
        AddPosition(connection, "2026-01-01 10:00:00", "2026-01-01 13:00:00", 15);
        AddPosition(connection, "2026-01-01 11:00:00", "2026-01-01 12:30:00", 45);
        AddPosition(connection, "2026-01-01 12:00:00", "2026-01-01 14:00:00", 105);

        var peak = Peak(connection);

        Assert.AreEqual(165.0, peak.Money!.Value, 0.001, "15 + 45 + 105 stonden tegelijk open");
        Assert.AreEqual(3.0, peak.Positions!.Value, 0.001);
    }


    [TestMethod]
    public void SequentialPositionsDoNotStack()
    {
        using var connection = CreateDatabase();
        // The same money going round: each position closes before the next one opens. Summing the
        // invested amounts would say 315, which is the mistake this column exists to avoid.
        AddPosition(connection, "2026-01-01 10:00:00", "2026-01-01 11:00:00", 105);
        AddPosition(connection, "2026-01-01 11:30:00", "2026-01-01 12:00:00", 105);
        AddPosition(connection, "2026-01-01 13:00:00", "2026-01-01 14:00:00", 105);

        var peak = Peak(connection);

        Assert.AreEqual(105.0, peak.Money!.Value, 0.001, "nooit meer dan een positie tegelijk");
        Assert.AreEqual(1.0, peak.Positions!.Value, 0.001);
    }


    [TestMethod]
    public void ClosingAndOpeningOnTheSameMomentDoesNotStack()
    {
        using var connection = CreateDatabase();
        // One closes at exactly the moment the next opens. The ordering in the query puts the close
        // first, so the two do not count as simultaneous - the conservative reading.
        AddPosition(connection, "2026-01-01 10:00:00", "2026-01-01 12:00:00", 60);
        AddPosition(connection, "2026-01-01 12:00:00", "2026-01-01 14:00:00", 60);

        var peak = Peak(connection);

        Assert.AreEqual(60.0, peak.Money!.Value, 0.001);
        Assert.AreEqual(1.0, peak.Positions!.Value, 0.001);
    }


    [TestMethod]
    public void OpenAndUnfundedPositionsAreIgnored()
    {
        using var connection = CreateDatabase();
        AddPosition(connection, "2026-01-01 10:00:00", "2026-01-01 11:00:00", 30);
        // Still running: no close time, so there is no moment at which its money is freed again.
        AddPosition(connection, "2026-01-01 10:00:00", null, 999);
        // An entry order that never filled: closed, but it never put money to work.
        AddPosition(connection, "2026-01-01 10:00:00", "2026-01-01 10:05:00", 0);

        var peak = Peak(connection);

        Assert.AreEqual(30.0, peak.Money!.Value, 0.001);
        Assert.AreEqual(1.0, peak.Positions!.Value, 0.001);
    }


    [TestMethod]
    public void OtherRunsAreNotCounted()
    {
        using var connection = CreateDatabase();
        AddPosition(connection, "2026-01-01 10:00:00", "2026-01-01 12:00:00", 15);
        AddPosition(connection, "2026-01-01 10:00:00", "2026-01-01 12:00:00", 500, runId: RunId + 1);

        var peak = Peak(connection);

        Assert.AreEqual(15.0, peak.Money!.Value, 0.001);
        Assert.AreEqual(1.0, peak.Positions!.Value, 0.001);
    }


    [TestMethod]
    public void RunWithoutTradedPositionsHasNoPeak()
    {
        using var connection = CreateDatabase();

        var peak = Peak(connection);

        // Null, not zero - the caller turns it into 0 so a run that never traded reads as such.
        Assert.IsNull(peak.Money);
        Assert.IsNull(peak.Positions);
    }


    /// <summary>
    /// Recalculating a run whose positions have been archived away would replace its counters and
    /// summary with zeros. That is not a refresh, it is a deletion: for Session0 (6.366 runs,
    /// aggregates only) it would wipe out every result there is.
    /// </summary>
    [TestMethod]
    public void ArchivedRunIsNotRecalculated()
    {
        Assert.IsFalse(EmulatorDb.CanRecalculate(positionsInTable: 0, storedPositionCount: 2077),
            "posities weg maar de run zegt dat hij er had: met rust laten");
    }


    [TestMethod]
    public void RunWithItsPositionsIsRecalculated()
    {
        Assert.IsTrue(EmulatorDb.CanRecalculate(positionsInTable: 2077, storedPositionCount: 2077));
        Assert.IsTrue(EmulatorDb.CanRecalculate(positionsInTable: 2077, storedPositionCount: 0),
            "een lopende run die nog geen telling heeft, mag wel");
    }


    [TestMethod]
    public void RunThatNeverTradedIsRecalculated()
    {
        // A zone strategy with an empty interval list produced nothing at all. Its stored count is
        // zero too, so it has nothing to lose and must not be treated as archived.
        Assert.IsTrue(EmulatorDb.CanRecalculate(positionsInTable: 0, storedPositionCount: 0));
    }
}
