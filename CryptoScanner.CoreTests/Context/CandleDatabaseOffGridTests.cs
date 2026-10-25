using CryptoScanner.Core.Context;
using CryptoScanner.Core.Model;

using Dapper;

using Microsoft.Data.Sqlite;

namespace CryptoScanner.CoreTests.Context;

/// <summary>
/// The one-time repair of candles that do not start on the scanner's grid. Until 25-10-2026 the
/// scanner took 1w from exchanges whose week starts on Thursday or Sunday (and 6h, 12h, 1d from Okx in
/// UTC+8), and stored those beside its own Monday weeks. A series with a candle off the grid goes as a
/// whole, with its sync pointer; the barometer symbols, which hold a candle per minute on purpose,
/// stay.
/// </summary>
[TestClass]
[DoNotParallelize]
public class CandleDatabaseOffGridTests : TestBase
{
    private const int Interval1d = 15;
    private const int Interval1w = 16;
    private const long Week = 10080;

    private static CryptoSymbol Setup()
    {
        InitTestSession();
        CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CandleDatabase.InitializeSchema(symbol.Exchange);
        return symbol;
    }

    /// <summary>
    /// The test symbol with a Monday week, a Thursday week and two daily candles on the grid, and a
    /// barometer symbol with a daily candle off the grid. Plus the two emulator tables.
    /// </summary>
    private static (int SymbolId, int BarometerId) Arrange(CryptoSymbol symbol)
    {
        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        SqliteConnection connection = candleDb.Connection;
        connection.Execute("DELETE FROM Candle");
        connection.Execute("DELETE FROM SymbolInterval");
        connection.Execute("INSERT OR REPLACE INTO Symbol (ExchangeName, Name) VALUES (@ExchangeName, @Name)",
            new { symbol.ExchangeName, symbol.Name });
        connection.Execute("INSERT OR REPLACE INTO Symbol (ExchangeName, Name) VALUES ('$BMPUSDT', '$BMPUSDT')");
        int symbolId = connection.QueryFirst<int>("SELECT SymbolId FROM Symbol WHERE ExchangeName = @ExchangeName", new { symbol.ExchangeName });
        int barometerId = connection.QueryFirst<int>("SELECT SymbolId FROM Symbol WHERE ExchangeName = '$BMPUSDT'");

        connection.Execute(
            "INSERT INTO Candle (SymbolId, IntervalId, OpenTime, Ticks, Open, High, Low, Close, Volume) VALUES " +
            "(@SymbolId, @I1w, @Monday, 2, 100, 110, 90, 105, 1)," +
            "(@SymbolId, @I1w, @Thursday, 2, 100, 110, 90, 105, 1)," +
            "(@SymbolId, @I1d, @Monday, 2, 100, 110, 90, 105, 1)," +
            "(@SymbolId, @I1d, @NextDay, 2, 100, 110, 90, 105, 1)," +
            "(@BarometerId, @I1d, @OffDay, 2, 100, 110, 90, 105, 1)",
            new
            {
                SymbolId = symbolId, BarometerId = barometerId, I1w = Interval1w, I1d = Interval1d,
                Monday = 100 * Week, Thursday = 100 * Week + 3 * 1440, NextDay = 100 * Week + 1440, OffDay = 100 * Week + 7,
            });
        connection.Execute(
            "INSERT INTO SymbolInterval (SymbolId, IntervalId, LastSync, DlzMarker) VALUES " +
            "(@SymbolId, @I1w, @Next, 900), (@SymbolId, @I1d, @Next, 900)",
            new { SymbolId = symbolId, I1w = Interval1w, I1d = Interval1d, Next = 101 * Week });

        connection.Execute("CREATE TABLE IF NOT EXISTS EmulatorHistoryAsked (SymbolName TEXT NOT NULL, IntervalId INTEGER NOT NULL, " +
            "FromTime INTEGER NOT NULL, ToTime INTEGER NOT NULL, PRIMARY KEY (SymbolName, IntervalId, FromTime)) WITHOUT ROWID");
        connection.Execute("DELETE FROM EmulatorHistoryAsked");
        connection.Execute("INSERT INTO EmulatorHistoryAsked VALUES (@Name, @I1w, 0, 1), (@Name, @I1d, 0, 1)",
            new { symbol.Name, I1w = Interval1w, I1d = Interval1d });
        connection.Execute("CREATE TABLE IF NOT EXISTS CandleSyncState (SymbolId INTEGER PRIMARY KEY, DerivedUpTo INTEGER, DeriveFrom INTEGER)");
        connection.Execute("DELETE FROM CandleSyncState");
        connection.Execute("INSERT INTO CandleSyncState VALUES (@SymbolId, 5, NULL)", new { SymbolId = symbolId });
        return (symbolId, barometerId);
    }

    private static (int Candles, int Series) Repair(CryptoSymbol symbol)
    {
        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        using SqliteTransaction tx = candleDb.Connection.BeginTransaction();
        var result = CandleDatabase.RepairOffGridCandles(candleDb.Connection, tx);
        tx.Commit();
        return result;
    }

    private static T Query<T>(CryptoSymbol symbol, string sql, object? args = null)
    {
        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        return candleDb.Connection.QueryFirst<T>(sql, args);
    }


    [TestMethod]
    public void AWeekOffTheGrid_RemovesTheWholeWeekSeries_AndOnlyThat()
    {
        CryptoSymbol symbol = Setup();
        (int symbolId, _) = Arrange(symbol);

        var result = Repair(symbol);

        Assert.AreEqual(2, result.Candles, "the Thursday week and the Monday week beside it");
        Assert.AreEqual(1, result.Series);
        Assert.AreEqual(0L, Query<long>(symbol, "SELECT COUNT(*) FROM Candle WHERE SymbolId = @SymbolId AND IntervalId = @I",
            new { SymbolId = symbolId, I = Interval1w }));
        Assert.AreEqual(2L, Query<long>(symbol, "SELECT COUNT(*) FROM Candle WHERE SymbolId = @SymbolId AND IntervalId = @I",
            new { SymbolId = symbolId, I = Interval1d }), "the daily candles are on the grid and stay");
    }


    /// <summary>
    /// Without this the fetcher and CryptoScanBot.CandleSync believe the week is complete and never
    /// build it again.
    /// </summary>
    [TestMethod]
    public void TheBookkeepingOfThatSeries_IsCleared()
    {
        CryptoSymbol symbol = Setup();
        (int symbolId, _) = Arrange(symbol);

        Repair(symbol);

        Assert.IsNull(Query<long?>(symbol, "SELECT LastSync FROM SymbolInterval WHERE SymbolId = @SymbolId AND IntervalId = @I",
            new { SymbolId = symbolId, I = Interval1w }), "LastSync of 1w");
        Assert.AreEqual(900L, Query<long?>(symbol, "SELECT DlzMarker FROM SymbolInterval WHERE SymbolId = @SymbolId AND IntervalId = @I",
            new { SymbolId = symbolId, I = Interval1w }), "the dlz marker stays");
        Assert.IsNotNull(Query<long?>(symbol, "SELECT LastSync FROM SymbolInterval WHERE SymbolId = @SymbolId AND IntervalId = @I",
            new { SymbolId = symbolId, I = Interval1d }), "LastSync of 1d is untouched");
        Assert.AreEqual(1L, Query<long>(symbol, "SELECT COUNT(*) FROM EmulatorHistoryAsked WHERE SymbolName = @Name", new { symbol.Name }),
            "only the 1w period is forgotten");
        Assert.AreEqual(0L, Query<long>(symbol, "SELECT COUNT(*) FROM CandleSyncState WHERE SymbolId = @SymbolId", new { SymbolId = symbolId }),
            "CandleSync derives the symbol from the start again");
    }


    [TestMethod]
    public void ABarometerSymbol_KeepsItsCandles()
    {
        CryptoSymbol symbol = Setup();
        (_, int barometerId) = Arrange(symbol);

        Repair(symbol);

        Assert.AreEqual(1L, Query<long>(symbol, "SELECT COUNT(*) FROM Candle WHERE SymbolId = @SymbolId", new { SymbolId = barometerId }));
    }
}
