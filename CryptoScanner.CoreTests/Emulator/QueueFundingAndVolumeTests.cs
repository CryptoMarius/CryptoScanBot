using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Emulator.Engine;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The two queue keys of 25-11-2026: "Funding" (book the funding of the perpetuals on the open
/// positions, off unless asked) and "MinimalVolume" (the volume filter of the quote coins for one run),
/// plus the symbols a queue entry names itself, which "Fetch candles" now fetches as well.
/// </summary>
[TestClass]
public class QueueFundingAndVolumeTests : TestBase
{
    private static CryptoQuoteData Usdt()
    {
        if (!GlobalData.Settings.QuoteCoins.TryGetValue("USDT", out CryptoQuoteData? quote))
        {
            quote = new CryptoQuoteData { Name = "USDT" };
            GlobalData.Settings.QuoteCoins.Add("USDT", quote);
        }
        return quote;
    }


    [TestMethod]
    public void MinimalVolume_IsAppliedForTheEntryAndTheSessionValueComesBack()
    {
        InitTestSession();
        CryptoQuoteData quote = Usdt();
        quote.MinimalVolume = 15_000_000;

        QueueBaseline.Install();
        List<SignalGridExpander.Override> overrides = [];
        try
        {
            // Without the key the session filter stays: it is not a strategy setting
            overrides = QueueEntryApplier.Apply(new EmulatorQueueEntry());
            Assert.AreEqual(15_000_000, quote.MinimalVolume);

            overrides = QueueEntryApplier.Apply(new EmulatorQueueEntry { MinimalVolume = 0 });
            Assert.AreEqual(0, quote.MinimalVolume);
        }
        finally
        {
            SignalGridExpander.Revert(overrides);
            QueueBaseline.Restore();
        }

        Assert.AreEqual(15_000_000, quote.MinimalVolume);
    }


    /// <summary>
    /// A repeat from the stored settings of a run takes the volume filter that run had, so a run
    /// measured without the filter is not repeated with the filter of the session.
    /// </summary>
    [TestMethod]
    public void MinimalVolume_OfAStoredRunIsTakenOnARepeat()
    {
        InitTestSession();
        CryptoQuoteData quote = Usdt();
        quote.MinimalVolume = 0;

        QueueBaseline.Install();
        string snapshot;
        try
        {
            snapshot = QueueSourceCheck.CaptureSettingsJson();
        }
        finally
        {
            QueueBaseline.Restore();
        }

        quote.MinimalVolume = 15_000_000;
        QueueBaseline.Install(snapshot);
        try
        {
            Assert.AreEqual(0, quote.MinimalVolume);
        }
        finally
        {
            QueueBaseline.Restore();
        }

        Assert.AreEqual(15_000_000, quote.MinimalVolume);
    }


    /// <summary>
    /// Off is not written, so the configuration of a run without funding reads exactly as before the
    /// setting existed and the duplicate check still recognises the older runs.
    /// </summary>
    [TestMethod]
    public void BookFunding_IsOffByDefaultAndOnlyWrittenWhenOn()
    {
        EmulatorRunConfig off = new() { Symbols = ["BTCUSDT.PERP"] };
        Assert.IsFalse(off.BookFunding);
        StringAssert.DoesNotMatch(JsonSerializer.Serialize(off), new System.Text.RegularExpressions.Regex("BookFunding"));

        EmulatorRunConfig on = new() { Symbols = ["BTCUSDT.PERP"], BookFunding = true };
        string json = JsonSerializer.Serialize(on);
        StringAssert.Contains(json, "\"BookFunding\":true");
        Assert.IsTrue(JsonSerializer.Deserialize<EmulatorRunConfig>(json)!.BookFunding);
    }


    [TestMethod]
    public void QueueEntry_ReadsTheFundingAndVolumeKeys()
    {
        string folder = Path.Combine(Path.GetTempPath(), "queue-keys-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        try
        {
            string file = Path.Combine(folder, "01-test.json");
            File.WriteAllText(file, """[{"Label": "a", "Algorithm": "xsmom", "Funding": true, "MinimalVolume": 0}, {"Label": "b", "Algorithm": "xsmom"}]""");

            List<EmulatorQueueEntry> entries = EmulatorQueueFile.LoadFrom(file);

            Assert.AreEqual(true, entries[0].Funding);
            Assert.AreEqual(0, entries[0].MinimalVolume);
            Assert.IsNull(entries[1].Funding);
            Assert.IsNull(entries[1].MinimalVolume);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }


    /// <summary>
    /// "Fetch candles" takes the coins queue entries name themselves on top of the run configuration:
    /// parked files included, Done left out, each coin once, the ones already known not again.
    /// </summary>
    [TestMethod]
    public void FetchCandles_TakesTheSymbolsOfTheQueueEntries()
    {
        string folder = Path.Combine(Path.GetTempPath(), "queue-symbols-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(folder, "Done"));
        try
        {
            File.WriteAllText(Path.Combine(folder, "01-a.json"),
                """[{"Label": "a", "Symbols": ["BTCUSDT.PERP", "AAAUSDT.PERP"]}, {"Label": "b"}]""");
            File.WriteAllText(Path.Combine(folder, "02-b.debug.json.na-nieuwe-build"),
                """[{"Label": "c", "Symbols": ["aaausdt.perp", "BBBUSDT.PERP"]}]""");
            File.WriteAllText(Path.Combine(folder, "03-broken.json"), "not json");
            File.WriteAllText(Path.Combine(folder, "Done", "20261101-0000 04-c.json"),
                """[{"Label": "d", "Symbols": ["CCCUSDT.PERP"]}]""");

            List<string> extra = EmulatorCandleSync.QueueSymbolsOutside(["BTCUSDT.PERP"], folder);

            CollectionAssert.AreEqual(new[] { "AAAUSDT.PERP", "BBBUSDT.PERP" }, extra);
        }
        finally
        {
            Directory.Delete(folder, true);
        }
    }
}
