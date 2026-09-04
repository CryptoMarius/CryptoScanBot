using CryptoScanner.Core.Context;
using CryptoScanner.Core.Model;

using Dapper;

using Microsoft.Data.Sqlite;

namespace CryptoScanner.CoreTests.Context;

/// <summary>
/// The conversion of a candles.db from version 4 to version 5: candles stored without decimals under
/// a symbol that has them are removed, and the sync bookkeeping of that symbol is cleared so the
/// history is fetched again.
/// <para>
/// Until 30-08-2026 HyperLiquid Perpetual derived its price tick through a text conversion that
/// looked for a '.', which a Windows with a decimal comma never writes. Every market then got a tick
/// size of 1 and every candle was stored without decimals - zero for any price under 0.50. The tick
/// size of the symbol heals with the next refresh; the candles do not, because each one carries the
/// decimals it was written with.
/// </para>
/// </summary>
[TestClass]
[DoNotParallelize]
public class CandleDatabaseVersion5Tests : TestBase
{
    private const int Interval1m = 1;
    private const int Interval1h = 8;

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
    /// Registers the test symbol in the file with two candles stored without decimals, one stored
    /// with the symbol's own decimals, and sync bookkeeping that says the history is complete.
    /// </summary>
    private static int ArrangeCandles(CryptoSymbol symbol)
    {
        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        candleDb.Connection.Execute("DELETE FROM Candle");
        candleDb.Connection.Execute("DELETE FROM SymbolInterval");
        candleDb.Connection.Execute(
            "INSERT OR REPLACE INTO Symbol (ExchangeName, Name) VALUES (@ExchangeName, @Name)",
            new { symbol.ExchangeName, symbol.Name });
        int symbolId = candleDb.Connection.QueryFirst<int>(
            "SELECT SymbolId FROM Symbol WHERE ExchangeName = @ExchangeName", new { symbol.ExchangeName });

        // Two candles the broken tick size produced (zero decimals, one of them a zero price) and one
        // that a corrected tick size produced afterwards.
        candleDb.Connection.Execute(
            "INSERT INTO Candle (SymbolId, IntervalId, OpenTime, Ticks, Open, High, Low, Close, Volume) VALUES " +
            "(@SymbolId, @I1m, 1000, 0, 0, 0, 0, 0, 1)," +
            "(@SymbolId, @I1h, 1000, 0, 374, 375, 374, 374, 1)," +
            "(@SymbolId, @I1m, 1001, @Decimals, 4123, 4125, 4120, 4124, 1)",
            new { SymbolId = symbolId, I1m = Interval1m, I1h = Interval1h, Decimals = (int)Indexed(symbol).PriceDecimals });
        candleDb.Connection.Execute(
            "INSERT INTO SymbolInterval (SymbolId, IntervalId, LastSync, DlzMarker) VALUES " +
            "(@SymbolId, @I1m, 1001, 900), (@SymbolId, @I1h, 1000, 900)",
            new { SymbolId = symbolId, I1m = Interval1m, I1h = Interval1h });
        candleDb.Connection.Execute(
            "INSERT OR REPLACE INTO Meta (Key, Value) VALUES ('SchemaVersion', '4')");
        return symbolId;
    }

    /// <summary>The object the exchange index holds for the test instrument, which is what the repair compares against.</summary>
    private static CryptoSymbol Indexed(CryptoSymbol symbol)
    {
        Assert.IsTrue(symbol.Exchange.SymbolListExchangeName.TryGetValue(symbol.ExchangeName, out CryptoSymbol? indexed),
            "the test instrument is registered in the exchange index");
        return indexed!;
    }

    private static (int Candles, int Symbols) Repair(CryptoSymbol symbol)
    {
        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        using SqliteTransaction tx = candleDb.Connection.BeginTransaction();
        var result = CandleDatabase.RepairZeroDecimalCandles(candleDb.Connection, symbol.Exchange, tx);
        tx.Commit();
        return result;
    }

    private static List<(int IntervalId, long OpenTime, int Ticks)> ReadCandles(CryptoSymbol symbol, int symbolId)
    {
        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        return [.. candleDb.Connection.Query<(int, long, int)>(
            "SELECT IntervalId, OpenTime, Ticks FROM Candle WHERE SymbolId = @SymbolId ORDER BY IntervalId, OpenTime",
            new { SymbolId = symbolId })];
    }

    private static List<(int IntervalId, long? LastSync, long? DlzMarker)> ReadBookkeeping(CryptoSymbol symbol, int symbolId)
    {
        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        return [.. candleDb.Connection.Query<(int, long?, long?)>(
            "SELECT IntervalId, LastSync, DlzMarker FROM SymbolInterval WHERE SymbolId = @SymbolId ORDER BY IntervalId",
            new { SymbolId = symbolId })];
    }

    private static string? ReadVersion(CryptoSymbol symbol)
    {
        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        return candleDb.Connection.QueryFirstOrDefault<string>(
            "SELECT Value FROM Meta WHERE Key = 'SchemaVersion'");
    }


    [TestMethod]
    public void CandlesWithoutDecimals_AreRemoved_AndTheOthersStay()
    {
        CryptoSymbol symbol = Setup();
        Assert.IsTrue(Indexed(symbol).PriceDecimals > 0, "the test symbol has decimals, otherwise there is nothing to judge");
        int symbolId = ArrangeCandles(symbol);

        var result = Repair(symbol);

        Assert.AreEqual(2, result.Candles, "the two candles stored without decimals");
        Assert.AreEqual(1, result.Symbols);
        var left = ReadCandles(symbol, symbolId);
        Assert.AreEqual(1, left.Count, "the candle with the symbol's own decimals is untouched");
        Assert.AreEqual((int)Indexed(symbol).PriceDecimals, left[0].Ticks);
    }


    /// <summary>
    /// Without this the fetcher believes the history is complete and never asks for it again, and
    /// the zone engine trusts a settled history whose candles are gone.
    /// </summary>
    [TestMethod]
    public void TheBookkeeping_IsCleared_SoTheHistoryIsFetchedAgain()
    {
        CryptoSymbol symbol = Setup();
        int symbolId = ArrangeCandles(symbol);

        Repair(symbol);

        var bookkeeping = ReadBookkeeping(symbol, symbolId);
        Assert.AreEqual(2, bookkeeping.Count, "the rows stay, only their content is cleared");
        foreach (var row in bookkeeping)
        {
            Assert.IsNull(row.LastSync, $"LastSync of interval {row.IntervalId}");
            Assert.IsNull(row.DlzMarker, $"DlzMarker of interval {row.IntervalId}");
        }
    }


    /// <summary>
    /// A symbol that trades in whole units really has zero decimals; its candles are right and stay.
    /// </summary>
    [TestMethod]
    public void ASymbolWithoutDecimals_KeepsItsCandles()
    {
        CryptoSymbol symbol = Setup();
        int symbolId = ArrangeCandles(symbol);
        // The repair looks the instrument up in the exchange's own index, and the test session can
        // hold another object under that key than CreateTestSymbol hands out - so the decimals are
        // changed on the object the repair will actually see.
        CryptoSymbol indexed = Indexed(symbol);
        byte previous = indexed.PriceDecimals;
        indexed.PriceDecimals = 0;
        try
        {
            var result = Repair(symbol);

            Assert.AreEqual(0, result.Candles);
            Assert.AreEqual(3, ReadCandles(symbol, symbolId).Count);
            Assert.IsNotNull(ReadBookkeeping(symbol, symbolId)[0].LastSync, "and its bookkeeping is left alone");
        }
        finally
        {
            indexed.PriceDecimals = previous;
        }
    }


    /// <summary>
    /// The conversion itself only repairs the one market that could have produced these candles;
    /// any other file is stamped and left as it is.
    /// </summary>
    [TestMethod]
    public void AnotherMarket_IsStampedAndLeftAlone()
    {
        CryptoSymbol symbol = Setup();
        int symbolId = ArrangeCandles(symbol);
        Assert.AreNotEqual("HyperLiquid Perpetual", symbol.Exchange.Name);

        CandleDatabase.InitializeSchema(symbol.Exchange);

        Assert.AreEqual(CandleDatabase.CurrentSchemaVersion.ToString(), ReadVersion(symbol));
        Assert.AreEqual(3, ReadCandles(symbol, symbolId).Count, "nothing removed on a market that never had the text tick");
    }
}
