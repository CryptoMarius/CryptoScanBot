using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

using Dapper;

namespace CryptoScanner.CoreTests.Context;

/// <summary>
/// Tests for the instrument provenance that candles.db stores next to the candles.
///
/// A scanner name does not identify an instrument: an exchange can publish several instruments with
/// the same base and quote (BTCUSDT next to BTCUSDT_261225), rename one, or move it from spot to
/// swap. Candles of the one are worthless as candles of the other, so the instrument they were
/// fetched with is recorded and checked on load. On a mismatch the "synchronised up to here" marker
/// is deliberately NOT restored, which makes the next fetch cycle pull the whole window again.
/// </summary>
[TestClass]
[DoNotParallelize]
public class CandleDatabaseInstrumentTests : TestBase
{
    private const string OriginalInstrument = "TEST exchange";
    private const string OtherInstrument = "TEST exchange_261225";

    // Arbitrary but fixed, so a failure reports the same numbers on every run.
    private static readonly CandleTime StoredLastSync = new(8_700_000);


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
        // CreateTestSymbol hands out the SAME cached symbol to every test, so a changed instrument
        // name would leak into the next one.
        // Fully qualified: CryptoExchange is also the root namespace of the CryptoExchange.Net library.
        if (GlobalData.ExchangeListName.TryGetValue(GlobalData.Settings.General.ExchangeName,
                out CryptoScanner.Core.Model.CryptoExchange? exchange)
            && exchange.SymbolListName.TryGetValue("TESTUSDT", out CryptoSymbol? symbol))
        {
            symbol.ExchangeName = OriginalInstrument;
        }
    }


    private static void Save(CryptoSymbol symbol, string instrument, CandleTime? lastSync)
    {
        symbol.ExchangeName = instrument;
        foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
            symbolInterval.LastCandleSynchronized = lastSync;

        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        CandleDatabase.SaveCandlesForSymbol(candleDb.Connection, symbol);
    }


    private static void Load(CryptoSymbol symbol, string instrument)
    {
        symbol.ExchangeName = instrument;
        foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
            symbolInterval.LastCandleSynchronized = null;

        using var candleDb = new CandleDatabase(symbol.Exchange);
        candleDb.Open();
        CandleDatabase.LoadCandlesForSymbol(candleDb.Connection, symbol);
    }


    private static void AssertLastSyncRestored(CryptoSymbol symbol, bool expected)
    {
        foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
        {
            if (expected)
            {
                Assert.AreEqual(StoredLastSync, symbolInterval.LastCandleSynchronized,
                    $"{symbolInterval.Interval.Name}: LastCandleSynchronized should have been restored");
            }
            else
            {
                Assert.IsNull(symbolInterval.LastCandleSynchronized,
                    $"{symbolInterval.Interval.Name}: LastCandleSynchronized should NOT have been restored");
            }
        }
    }


    [TestMethod]
    public void SameInstrument_RestoresLastCandleSynchronized()
    {
        CryptoSymbol symbol = Setup();
        Save(symbol, OriginalInstrument, StoredLastSync);

        Load(symbol, OriginalInstrument);

        AssertLastSyncRestored(symbol, true);
    }


    [TestMethod]
    public void ChangedInstrument_DoesNotRestoreLastCandleSynchronized()
    {
        CryptoSymbol symbol = Setup();
        Save(symbol, OriginalInstrument, StoredLastSync);

        // The exchange now hands us a different instrument for the same scanner name — everything
        // stored belongs to the previous one and has to be fetched again.
        Load(symbol, OtherInstrument);

        AssertLastSyncRestored(symbol, false);
    }


    [TestMethod]
    public void UnrecordedInstrument_DoesNotRestoreLastCandleSynchronized()
    {
        CryptoSymbol symbol = Setup();
        Save(symbol, OriginalInstrument, StoredLastSync);

        // Simulate a database written by a build that did not record the instrument yet. Its candles
        // cannot be vouched for, so they are refetched once — after which the save fills the column
        // and this can never trigger a second time.
        using (var candleDb = new CandleDatabase(symbol.Exchange))
        {
            candleDb.Open();
            candleDb.Connection.Execute(
                "UPDATE Symbol SET ExchangeName = NULL WHERE Name = $Name", new { symbol.Name });
        }

        Load(symbol, OriginalInstrument);

        AssertLastSyncRestored(symbol, false);
    }


    [TestMethod]
    public void SavingAfterAMismatch_RecordsTheNewInstrument()
    {
        CryptoSymbol symbol = Setup();
        Save(symbol, OriginalInstrument, StoredLastSync);

        // The refetch that follows a mismatch ends in a save, which must adopt the new instrument so
        // the very next load matches again instead of refetching forever.
        Save(symbol, OtherInstrument, StoredLastSync);
        Load(symbol, OtherInstrument);

        AssertLastSyncRestored(symbol, true);
    }
}
