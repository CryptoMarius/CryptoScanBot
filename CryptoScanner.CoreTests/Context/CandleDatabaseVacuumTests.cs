using CryptoScanner.Core.Context;
using CryptoScanner.Core.Model;

using Dapper;

using Microsoft.Data.Sqlite;

namespace CryptoScanner.CoreTests.Context;

/// <summary>
/// The weekly VACUUM of the candle file: it runs when due, leaves the file in WAL and stamps the date, does nothing while the date is recent, and steps aside
/// without stamping when another connection has the file open.
/// </summary>
[TestClass]
[DoNotParallelize]
public class CandleDatabaseVacuumTests : TestBase
{
    private static CryptoSymbol Setup()
    {
        InitTestSession();
        CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CandleDatabase.InitializeSchema(symbol.Exchange);
        CandleDatabase.DeleteMeta(symbol.Exchange, CandleDatabase.LastVacuumMetaKey);
        return symbol;
    }

    [TestMethod]
    public void VacuumRunsWhenDueAndKeepsWal()
    {
        CryptoSymbol symbol = Setup();

        string? line = CandleDatabase.VacuumIfDue(symbol.Exchange, CandleDatabase.VacuumInterval);

        Assert.IsNotNull(line);
        Assert.IsFalse(line.Contains("skipped") || line.Contains("FAILED"), line);
        Assert.IsNotNull(CandleDatabase.ReadMeta(symbol.Exchange, CandleDatabase.LastVacuumMetaKey));

        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        Assert.AreEqual("wal", candleDb.Connection.ExecuteScalar<string>("PRAGMA journal_mode"));
    }

    [TestMethod]
    public void VacuumIsSkippedWhileTheDateIsRecent()
    {
        CryptoSymbol symbol = Setup();
        CandleDatabase.VacuumIfDue(symbol.Exchange, CandleDatabase.VacuumInterval);

        Assert.IsNull(CandleDatabase.VacuumIfDue(symbol.Exchange, CandleDatabase.VacuumInterval));
    }

    [TestMethod]
    public void VacuumStepsAsideWhenTheFileIsInUse()
    {
        CryptoSymbol symbol = Setup();

        // A second, unpooled connection that stays open, as another process would
        string dbFile = Path.Combine(CandleDatabase.ResolveCandleFolder(), symbol.Exchange.Name + ".db");
        using (SqliteConnection other = new($"Filename={dbFile};Mode=ReadWrite;Pooling=False;"))
        {
            other.Open();
            other.ExecuteScalar<int>("SELECT COUNT(*) FROM Meta");

            string? line = CandleDatabase.VacuumIfDue(symbol.Exchange, CandleDatabase.VacuumInterval);

            Assert.IsNotNull(line);
            StringAssert.Contains(line, "skipped");
        }
        Assert.IsNull(CandleDatabase.ReadMeta(symbol.Exchange, CandleDatabase.LastVacuumMetaKey));
    }
}
