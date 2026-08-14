using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;

namespace CryptoScanner.CoreTests.Context;

/// <summary>
/// Tests for the key candles.db stores its candles under: the exchange INSTRUMENT, not the scanner
/// name. A scanner name does not identify an instrument — Binance publishes BTCUSDT next to
/// BTCUSDT_261225 and both carry base BTC and quote USDT, and Okx moved its futures from spot
/// instruments to swap instruments. Keyed on the instrument each gets its own registration, so the
/// candles of one can never be served as the candles of the other.
/// </summary>
[TestClass]
[DoNotParallelize]
public class CandleDatabaseInstrumentTests : TestBase
{
    private const string OriginalInstrument = "TEST exchange";

    // One instrument name per test. They share the same candle database file, so a name that another
    // test registers would still be there — which is exactly what NeverStoredInstrument must not find.
    private const string NeverStoredInstrument = "TEST exchange_never_stored";
    private const string SecondInstrument = "TEST exchange_261225";

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
    public void OtherInstrument_FindsNothing()
    {
        CryptoSymbol symbol = Setup();
        Save(symbol, OriginalInstrument, StoredLastSync);

        // Same scanner name, different instrument: this is the case that used to hand back the
        // candles of the first one. There is no registration for it, so nothing is found and the
        // next fetch cycle pulls the whole window.
        Load(symbol, NeverStoredInstrument);

        AssertLastSyncRestored(symbol, false);
    }


    [TestMethod]
    public void TwoInstruments_KeepTheirOwnCandles()
    {
        CryptoSymbol symbol = Setup();
        Save(symbol, OriginalInstrument, StoredLastSync);

        // Give the second instrument its own bookkeeping...
        CandleTime otherLastSync = StoredLastSync + 1000;
        Save(symbol, SecondInstrument, otherLastSync);

        // ...and the first one must still return its own, not the second one's
        Load(symbol, OriginalInstrument);
        AssertLastSyncRestored(symbol, true);

        Load(symbol, SecondInstrument);
        foreach (CryptoSymbolInterval symbolInterval in symbol.Data.SymbolIntervalList)
        {
            Assert.AreEqual(otherLastSync, symbolInterval.LastCandleSynchronized,
                $"{symbolInterval.Interval.Name}: the second instrument has its own LastCandleSynchronized");
        }
    }
}
