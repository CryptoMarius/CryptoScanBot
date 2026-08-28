using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper;

namespace CryptoScanner.CoreTests.Context;

/// <summary>
/// The conversion of a candles.db from version 3 to version 4.
/// <para>
/// Version 4 changes no layout - it repairs one column. A row is addressed by ExchangeName, the
/// exchange's own name for the instrument, while the Name column carries the SCANNER name as it was
/// when the row was written. The scanner renamed its symbols on 27-08-2026 to carry the product
/// behind a dot (ZECUSDT became ZECUSDT.PERP) and that rename did not touch ExchangeName, so every
/// candle stayed reachable and the stale Name stayed invisible: 697 of 877 rows on Binance
/// Perpetual, in a file in daily use.
/// </para>
/// <para>
/// Invisible, but not harmless - MigrateToVersion3 resolves a row through TryGetSymbolByPair on
/// exactly this Name, so a file left in this state would lose those candles the next time it was
/// converted.
/// </para>
/// </summary>
[TestClass]
[DoNotParallelize]
public class CandleDatabaseVersion4Tests : TestBase
{
    private const string Instrument = "TESTUSDT_v4instrument";
    private const string OriginalInstrument = "TEST exchange";

    private static CryptoSymbol Setup()
    {
        InitTestSession();
        CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CandleDatabase.InitializeSchema(symbol.Exchange);
        return symbol;
    }

    [TestCleanup]
    public void RestoreSymbol()
    {
        // CreateTestSymbol hands the same cached symbol to every test, so a changed instrument name
        // would leak into the next one.
        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName,
                out CryptoScanner.Core.Model.CryptoExchange? exchange)
            && exchange.SymbolListName.TryGetValue("TESTUSDT", out CryptoSymbol? symbol))
        {
            // SetSymbolExchangeName, niet het veld zelf: de migratie zoekt in
            // SymbolListExchangeName, en die index verhuist alleen mee via deze methode.
            exchange.SetSymbolExchangeName(symbol, OriginalInstrument);
        }
    }

    /// <summary>
    /// Puts the file back into the version-3 state this migration exists for: a row keyed on the
    /// instrument, carrying a Name that is no longer what the scanner calls the symbol.
    /// </summary>
    private static void ArrangeStaleRow(CryptoSymbol symbol, string instrument, string staleName)
    {
        symbol.Exchange.SetSymbolExchangeName(symbol, instrument);

        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        candleDb.Connection.Execute(
            "INSERT OR REPLACE INTO Symbol (ExchangeName, Name) VALUES (@ExchangeName, @Name)",
            new { ExchangeName = instrument, Name = staleName });
        candleDb.Connection.Execute(
            "INSERT OR REPLACE INTO Meta (Key, Value) VALUES ('SchemaVersion', '3')");
    }

    private static string? ReadName(CryptoSymbol symbol, string instrument)
    {
        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        return candleDb.Connection.QueryFirstOrDefault<string>(
            "SELECT Name FROM Symbol WHERE ExchangeName = @ExchangeName", new { ExchangeName = instrument });
    }

    private static string? ReadVersion(CryptoSymbol symbol)
    {
        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        return candleDb.Connection.QueryFirstOrDefault<string>(
            "SELECT Value FROM Meta WHERE Key = 'SchemaVersion'");
    }


    [TestMethod]
    public void AStaleName_IsBroughtInLineWithTheScanner()
    {
        CryptoSymbol symbol = Setup();
        ArrangeStaleRow(symbol, Instrument, staleName: "TESTUSDT_oude_naam");

        CandleDatabase.InitializeSchema(symbol.Exchange);

        Assert.AreEqual(symbol.Name, ReadName(symbol, Instrument),
            "de naam hoort overgenomen te zijn van het symbool waar de sleutel naar wijst");
        Assert.AreEqual(CandleDatabase.CurrentSchemaVersion.ToString(), ReadVersion(symbol),
            "en het bestand staat daarna op de huidige versie");
    }


    /// <summary>
    /// The key is what addresses the candles. A migration that touches it would orphan every candle
    /// in the file, which is the one thing this must never do.
    /// </summary>
    [TestMethod]
    public void TheInstrumentKey_IsLeftAlone()
    {
        CryptoSymbol symbol = Setup();
        ArrangeStaleRow(symbol, Instrument, staleName: "TESTUSDT_oude_naam");

        CandleDatabase.InitializeSchema(symbol.Exchange);

        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        int stillThere = candleDb.Connection.QueryFirstOrDefault<int>(
            "SELECT COUNT(*) FROM Symbol WHERE ExchangeName = @ExchangeName", new { ExchangeName = Instrument });
        Assert.AreEqual(1, stillThere, "de rij is nog steeds op dezelfde instrumentsleutel te vinden");
    }


    /// <summary>
    /// A row whose instrument the exchange no longer lists keeps its name and its candles. Deciding
    /// what is an orphan belongs to version 3; a name repair may not quietly throw data away.
    /// </summary>
    [TestMethod]
    public void AnInstrumentTheExchangeNoLongerLists_IsLeftUntouched()
    {
        CryptoSymbol symbol = Setup();
        // Registered in the file, but never handed to the exchange's instrument index.
        using (var candleDb = new CandleDatabase(symbol.Exchange))
        {
            candleDb.Open();
            candleDb.Connection.Execute(
                "INSERT OR REPLACE INTO Symbol (ExchangeName, Name) VALUES ('TESTUSDT_delisted', 'TESTUSDT_delisted')");
            candleDb.Connection.Execute(
                "INSERT OR REPLACE INTO Meta (Key, Value) VALUES ('SchemaVersion', '3')");
        }

        CandleDatabase.InitializeSchema(symbol.Exchange);

        Assert.AreEqual("TESTUSDT_delisted", ReadName(symbol, "TESTUSDT_delisted"),
            "een instrument dat de beurs niet meer noemt blijft staan zoals het stond");
    }


    /// <summary>
    /// Running it twice must be a no-op: the file is already at version 4, so the conversion is not
    /// entered again.
    /// </summary>
    [TestMethod]
    public void RunningItAgain_ChangesNothing()
    {
        CryptoSymbol symbol = Setup();
        ArrangeStaleRow(symbol, Instrument, staleName: "TESTUSDT_oude_naam");

        CandleDatabase.InitializeSchema(symbol.Exchange);
        string? na = ReadName(symbol, Instrument);

        CandleDatabase.InitializeSchema(symbol.Exchange);

        Assert.AreEqual(na, ReadName(symbol, Instrument));
        Assert.AreEqual(CandleDatabase.CurrentSchemaVersion.ToString(), ReadVersion(symbol));
    }
}
