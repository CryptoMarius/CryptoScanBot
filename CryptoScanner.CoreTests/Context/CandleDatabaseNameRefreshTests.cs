using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper;

namespace CryptoScanner.CoreTests.Context;

/// <summary>
/// The Name column of a registration, kept current by the hourly cleanup.
/// <para>
/// A row is addressed by ExchangeName, the exchange's own name for the instrument, while Name carries
/// the SCANNER name as it was when the row was written. The conversion to version 4 repairs that
/// column once, at the version step, and the scanner keeps renaming symbols afterwards - the TradFi
/// split landed after that step and left 217 of the 627 rows on Okx Perpetual reading AAPLUSDT.PERP
/// where the scanner says AAPLUSDT.TRADFI (measured 16-09-2026). Every candle stayed reachable,
/// because no rename touches the key, so this is about keeping the column readable rather than about
/// saving data.
/// </para>
/// </summary>
[TestClass]
[DoNotParallelize]
public class CandleDatabaseNameRefreshTests : TestBase
{
    private static CryptoSymbol Setup()
    {
        InitTestSession();
        CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CandleDatabase.InitializeSchema(symbol.Exchange);
        return symbol;
    }

    private static void WriteName(CryptoSymbol symbol, string instrument, string name)
    {
        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        candleDb.Connection.Execute(
            "INSERT OR REPLACE INTO Symbol (ExchangeName, Name) VALUES (@ExchangeName, @Name)",
            new { ExchangeName = instrument, Name = name });
    }

    /// <summary>
    /// What the scanner calls the instrument right now. Read from the index the refresh itself reads,
    /// not from the symbol the test happens to hold: the suite keeps one cached test symbol and the
    /// exchange knows it under the name with its product behind the dot.
    /// </summary>
    private static string CurrentName(CryptoSymbol symbol)
        => symbol.Exchange.SymbolListExchangeName[symbol.ExchangeName].Name;

    private static string? ReadName(CryptoSymbol symbol, string instrument)
    {
        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        return candleDb.Connection.QueryFirstOrDefault<string>(
            "SELECT Name FROM Symbol WHERE ExchangeName = @ExchangeName", new { ExchangeName = instrument });
    }


    /// <summary>
    /// The file is already at the current version, so the migration will not run again - the cleanup
    /// is what has to notice that the scanner calls this instrument something else by now.
    /// </summary>
    [TestMethod]
    public void AStaleNameOnACurrentFile_IsBroughtInLineByTheCleanup()
    {
        CryptoSymbol symbol = Setup();
        WriteName(symbol, symbol.ExchangeName, "TESTUSDT_naam_van_toen");

        CandleDatabase.CleanCandlesForExchange(symbol.Exchange);

        Assert.AreEqual(CurrentName(symbol), ReadName(symbol, symbol.ExchangeName),
            "de naam hoort overgenomen te zijn van het symbool waar de sleutel naar wijst");
    }


    /// <summary>
    /// The key is what addresses the candles, so the refresh may never touch it.
    /// </summary>
    [TestMethod]
    public void TheInstrumentKey_IsLeftAlone()
    {
        CryptoSymbol symbol = Setup();
        WriteName(symbol, symbol.ExchangeName, "TESTUSDT_naam_van_toen");

        CandleDatabase.CleanCandlesForExchange(symbol.Exchange);

        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        int stillThere = candleDb.Connection.QueryFirstOrDefault<int>(
            "SELECT COUNT(*) FROM Symbol WHERE ExchangeName = @ExchangeName",
            new { ExchangeName = symbol.ExchangeName });
        Assert.AreEqual(1, stillThere, "de rij is nog steeds op dezelfde instrumentsleutel te vinden");
    }


    /// <summary>
    /// A name that already agrees is left alone, and running the cleanup twice changes nothing.
    /// </summary>
    [TestMethod]
    public void RunningItAgain_ChangesNothing()
    {
        CryptoSymbol symbol = Setup();
        WriteName(symbol, symbol.ExchangeName, "TESTUSDT_naam_van_toen");

        CandleDatabase.CleanCandlesForExchange(symbol.Exchange);
        string? first = ReadName(symbol, symbol.ExchangeName);

        CandleDatabase.CleanCandlesForExchange(symbol.Exchange);

        Assert.AreEqual(first, ReadName(symbol, symbol.ExchangeName));
        Assert.AreEqual(CurrentName(symbol), ReadName(symbol, symbol.ExchangeName));
    }
}
