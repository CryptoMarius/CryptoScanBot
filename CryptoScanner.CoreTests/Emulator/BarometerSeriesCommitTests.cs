using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;
using CryptoScanner.Emulator.Engine;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// When a stored barometer series counts as whole, and what happens to the minutes it turns out to
/// be missing.
/// <para>
/// Until 25-10-2026 a measuring run wrote its marker at the START. Run 118 on HyperLiquid was stopped
/// at 29%, the marker already said the series was measured, and every run after it found nothing
/// from 17-03 on. Those runs measured the missing minutes again, but the pruning between chunks threw
/// them away before the save at the end of the run - only the last week survived - so the hole never
/// closed and every run paid 30 to 40 minutes for it under a log that said "reading".
/// </para>
/// </summary>
[TestClass]
[DoNotParallelize]
public class BarometerSeriesCommitTests : TestBase
{
    // A quote name no real series uses, so the marker of the test database is the test's own
    private const string QuoteName = "TSTQ";

    private readonly Dictionary<CryptoTradeSide, SettingsCompiled?> savedSignals = [];
    private readonly Dictionary<CryptoTradeSide, SettingsCompiled?> savedTrading = [];
    private CryptoScanner.Core.Model.CryptoExchange exchange = null!;


    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        exchange = GlobalData.ActiveExchange!;
        CandleDatabase.InitializeSchema(exchange);
        CandleDatabase.DeleteMeta(exchange, BarometerReplay.MarkerKey(QuoteName));

        // The constructor collects the intervals the barometer conditions ask about, on both sides
        foreach (CryptoTradeSide side in new[] { CryptoTradeSide.Long, CryptoTradeSide.Short })
        {
            savedSignals[side] = TradingConfig.Signals.TryGetValue(side, out var signals) ? signals : null;
            savedTrading[side] = TradingConfig.Trading.TryGetValue(side, out var trading) ? trading : null;
            TradingConfig.Signals[side] = new SettingsCompiled();
            TradingConfig.Trading[side] = new SettingsCompiled();
        }
    }


    [TestCleanup]
    public void Cleanup()
    {
        CandleDatabase.DeleteMeta(exchange, BarometerReplay.MarkerKey(QuoteName));
        Restore(TradingConfig.Signals, savedSignals);
        Restore(TradingConfig.Trading, savedTrading);
    }


    private static void Restore(Dictionary<CryptoTradeSide, SettingsCompiled> target, Dictionary<CryptoTradeSide, SettingsCompiled?> saved)
    {
        foreach ((CryptoTradeSide side, SettingsCompiled? previous) in saved)
        {
            if (previous == null)
                target.Remove(side);
            else
                target[side] = previous;
        }
    }


    private (CryptoQuoteData Quote, List<CryptoSymbol> Coins) Coins(params string[] baseNames)
    {
        CryptoQuoteData quote = new() { Name = QuoteName, MinimalVolume = 0 };
        List<CryptoSymbol> coins = [.. baseNames.Select(b => new CryptoSymbol
        {
            Exchange = exchange,
            ExchangeName = exchange.Name,
            Name = b + QuoteName,
            Base = b,
            Quote = QuoteName,
            QuoteData = quote,
        })];
        return (quote, coins);
    }


    private string? StoredMarker() => CandleDatabase.ReadMeta(exchange, BarometerReplay.MarkerKey(QuoteName));


    /// <summary>
    /// A run that measures leaves no marker behind until it is told it completed. A Stop halfway
    /// through therefore leaves a series that the next run measures again, instead of one it trusts.
    /// </summary>
    [TestMethod]
    public void AMeasuringRunWritesItsMarkerOnlyWhenItIsCommitted()
    {
        var set = Coins("AAA", "BBB", "CCC", "DDD", "EEE");

        BarometerReplay barometer = new(exchange, set.Coins);

        Assert.IsNull(StoredMarker(), "no marker while the run is still measuring");
        Assert.IsTrue(barometer.HasMarkersToWrite);

        barometer.WriteMarkers();

        Assert.AreEqual(BarometerReplay.MarkerFor(set.Quote, set.Coins), StoredMarker());
        Assert.IsFalse(barometer.HasMarkersToWrite, "written once");
    }


    /// <summary>
    /// The marker of another coin list goes as soon as a run starts measuring over this one: the run
    /// overwrites the stored candles, also when it is stopped, and the old marker would then vouch for
    /// a mixture of two coin lists. That is the 121 coins after the 5-coin queue on Binance.
    /// </summary>
    [TestMethod]
    public void AMeasuringRunRemovesTheMarkerOfAnotherCoinListAtTheStart()
    {
        var before = Coins("AAA", "BBB", "CCC", "DDD", "EEE");
        var now = Coins("AAA", "BBB", "CCC", "DDD", "EEE", "FFF");
        CandleDatabase.WriteMeta(exchange, BarometerReplay.MarkerKey(QuoteName), BarometerReplay.MarkerFor(before.Quote, before.Coins));

        _ = new BarometerReplay(exchange, now.Coins);

        Assert.IsNull(StoredMarker());
    }


    /// <summary>A run over the same coins reads the series back, and its marker stays as it is.</summary>
    [TestMethod]
    public void AReusedSeriesKeepsItsMarkerAndHasNothingToWrite()
    {
        var set = Coins("AAA", "BBB", "CCC", "DDD", "EEE");
        string marker = BarometerReplay.MarkerFor(set.Quote, set.Coins);
        CandleDatabase.WriteMeta(exchange, BarometerReplay.MarkerKey(QuoteName), marker);

        BarometerReplay barometer = new(exchange, set.Coins);

        Assert.IsFalse(barometer.HasMarkersToWrite);
        Assert.AreEqual(marker, StoredMarker());
    }


    /// <summary>A stored candle before the new chunk is on disk already, so it may leave memory.</summary>
    [TestMethod]
    public void AStoredCandleBeforeTheChunkIsPruned()
    {
        CandleTime chunkFrom = new(100_000);

        Assert.IsTrue(BarometerReplay.MayBePruned(new CandleTime(99_999), chunkFrom, null));
        Assert.IsTrue(BarometerReplay.MayBePruned(new CandleTime(99_999), chunkFrom, [new CandleTime(99_000)]));
    }


    /// <summary>
    /// A minute this run measured because the store had nothing for it stays in memory, so the save
    /// at the end of the run writes it and the hole closes.
    /// </summary>
    [TestMethod]
    public void AMinuteMeasuredAgainStaysUntilTheSave()
    {
        CandleTime chunkFrom = new(100_000);

        Assert.IsFalse(BarometerReplay.MayBePruned(new CandleTime(99_999), chunkFrom, [new CandleTime(99_999)]));
    }


    /// <summary>The chunk itself is never pruned.</summary>
    [TestMethod]
    public void TheChunkItselfStays()
    {
        CandleTime chunkFrom = new(100_000);

        Assert.IsFalse(BarometerReplay.MayBePruned(chunkFrom, chunkFrom, null));
        Assert.IsFalse(BarometerReplay.MayBePruned(new CandleTime(100_001), chunkFrom, null));
    }


    /// <summary>The end-of-run line says how much of a reused series was measured again, and where.</summary>
    [TestMethod]
    public void TheEndOfRunLineCountsTheMinutesMeasuredAgain()
    {
        CandleTime first = CandleTime.FromDateTime(new DateTime(2026, 3, 17, 14, 34, 0, DateTimeKind.Utc));
        CandleTime last = CandleTime.FromDateTime(new DateTime(2026, 9, 23, 23, 59, 0, DateTimeKind.Utc));

        string text = BarometerReplay.MeasuredAgainText(270000, 393000, first, last);

        StringAssert.Contains(text, "270000 of 393000 minute(s)");
        StringAssert.Contains(text, "2026-03-17 14:34 .. 2026-09-23 23:59");
    }


    /// <summary>A series that was whole says so, so a reader of the log knows the reuse really worked.</summary>
    [TestMethod]
    public void TheEndOfRunLineSaysSoWhenNothingWasMissing()
    {
        string text = BarometerReplay.MeasuredAgainText(0, 393000, null, null);

        StringAssert.Contains(text, "all 393000 minute(s) read from the stored series");
    }
}
