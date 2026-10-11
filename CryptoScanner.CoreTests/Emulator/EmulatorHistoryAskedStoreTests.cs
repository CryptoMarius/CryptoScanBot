using CryptoScanner.Core.Context;
using CryptoScanner.Core.Model;
using CryptoScanner.Emulator.Engine;

using Microsoft.Data.Sqlite;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The periods the emulator's candle fetch asked the exchange for survive a restart, so a period that
/// was asked once (history before a listing, a hole in the exchange's data) is not asked again.
/// </summary>
[TestClass]
[DoNotParallelize]
public class EmulatorHistoryAskedStoreTests : TestBase
{
    [TestMethod]
    public void SaveAndLoad_RestoresTheAskedPeriods()
    {
        InitTestSession();
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoSymbolInterval first = symbol.Data.SymbolIntervalList[0];
        CryptoSymbolInterval last = symbol.Data.SymbolIntervalList[^1];
        first.ForgetHistory();
        last.ForgetHistory();

        using var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        EmulatorHistoryAskedStore.EnsureTable(connection);

        // A 1w window that starts before the listing, asked once
        last.RememberHistoryAsked(new CandleTime(3371040), new CandleTime(8760000));
        first.RememberHistoryAsked(new CandleTime(8409590), new CandleTime(8760961));
        EmulatorHistoryAskedStore.Save(connection, symbol);

        // A fresh session: nothing in memory
        first.ForgetHistory();
        last.ForgetHistory();
        Assert.IsFalse(last.HistoryWasAsked(new CandleTime(3371040), new CandleTime(8760000)));

        EmulatorHistoryAskedStore.Load(connection, symbol);
        Assert.IsTrue(last.HistoryWasAsked(new CandleTime(3371040), new CandleTime(8760000)));
        Assert.IsTrue(first.HistoryWasAsked(new CandleTime(8409590), new CandleTime(8760961)));
        // An earlier period was never asked, so it is still fetched
        Assert.IsFalse(first.HistoryWasAsked(new CandleTime(8000000), new CandleTime(8760961)));
        Assert.AreEqual(new CandleTime(8760961), first.SkipHistoryAlreadyAsked(new CandleTime(8409590)));
    }
}
