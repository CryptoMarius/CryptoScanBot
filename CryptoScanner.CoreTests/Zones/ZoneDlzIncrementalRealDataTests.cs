using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trend;
using CryptoScanner.Core.Zones;

namespace CryptoScanner.CoreTests.Zones;

/// <summary>
/// The incremental DLZ (open point 31: settled against provisional) on candles of the emulator
/// itself, next to the ETHUSDT and ADAUSDT sets of <see cref="ZoneDlzIncrementalTests"/>. Asked for by
/// Marius on 2026-09-27 before the design is signed off: "extra tests so it keeps running one to one,
/// with the data of one of the emulator's coins".
/// <para>
/// The data are the last 4000 candles of SOLUSDT 1h and XRPUSDT 15m from the emulator's candle
/// database (Binance Perpetual, exported 2026-09-27, no gaps), so a different coin, a different
/// interval and a different price scale than the existing sets. The property is the same: one full
/// calculation and the incremental walk at any calling rhythm give exactly the same zones, strength
/// included - and that also holds across a restart, where the ZigZag is rebuilt from the candles
/// while the committed zones and the cursor are carried over (the cursor is stored in the candle
/// database as DlzMarker).
/// </para>
/// </summary>
[TestClass]
public class ZoneDlzIncrementalRealDataTests : TestBase
{
    private static (CryptoSymbol symbol, CryptoInterval interval, CryptoCandleList candles) LoadScenario(
        string file, CryptoIntervalPeriod period)
    {
        InitTestSession();
        ZoneDlzTests.ConfigureSettingsForTest();

        using CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoInterval interval = GlobalData.IntervalListPeriod[period];
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        string path = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location)
            ?? throw new Exception("Error assembly");
        LoadCandleDataFromDisk(symbolInterval.CandleList, Path.Combine(path, "Zones", "Data", file));
        return (symbol, interval, symbolInterval.CandleList);
    }


    [TestMethod]
    [DataRow("SOLUSDT-1h.json", CryptoIntervalPeriod.interval1h, 1)]
    [DataRow("SOLUSDT-1h.json", CryptoIntervalPeriod.interval1h, 4)]
    [DataRow("SOLUSDT-1h.json", CryptoIntervalPeriod.interval1h, 24)]
    [DataRow("SOLUSDT-1h.json", CryptoIntervalPeriod.interval1h, 100)]
    [DataRow("XRPUSDT-15m.json", CryptoIntervalPeriod.interval15m, 1)]
    [DataRow("XRPUSDT-15m.json", CryptoIntervalPeriod.interval15m, 4)]
    [DataRow("XRPUSDT-15m.json", CryptoIntervalPeriod.interval15m, 96)]
    public async Task EmulatorCandlesGiveTheSameZonesAtEveryRhythm(string file, CryptoIntervalPeriod period, int blockSize)
    {
        var (symbol, interval, candles) = LoadScenario(file, period);

        List<CryptoZone> full = await ZoneDlzIncrementalTests.Replay(symbol, interval, candles, 0);
        List<CryptoZone> chunked = await ZoneDlzIncrementalTests.Replay(symbol, interval, candles, blockSize);

        Assert.IsTrue(full.Count > 20, $"only {full.Count} zones - too few for the comparison to say anything");
        string difference = ZoneDlzIncrementalTests.CompareZones(full, chunked, blockSize);
        Console.WriteLine(difference == "" ? $"{file} blockSize {blockSize}: identical ({full.Count} zones)" : difference);
        Assert.AreEqual("", difference);
    }


    [TestMethod]
    [DataRow("SOLUSDT-1h.json", CryptoIntervalPeriod.interval1h, 1)]
    [DataRow("SOLUSDT-1h.json", CryptoIntervalPeriod.interval1h, 24)]
    [DataRow("XRPUSDT-15m.json", CryptoIntervalPeriod.interval15m, 4)]
    public async Task EmulatorCandlesGiveTheSameStrengthAtEveryRhythm(string file, CryptoIntervalPeriod period, int blockSize)
    {
        var (symbol, interval, candles) = LoadScenario(file, period);
        GlobalData.Settings.Signal.ZonesDlz.ZoneStartApply = true;
        try
        {
            List<CryptoZone> full = await ZoneDlzIncrementalTests.Replay(symbol, interval, candles, 0);
            List<CryptoZone> chunked = await ZoneDlzIncrementalTests.Replay(symbol, interval, candles, blockSize);

            int graded = full.Count(z => z.Strength != CryptoZoneStrength.None);
            Assert.IsTrue(graded > 0, "no zone was graded - ZoneStartApply did not take effect, so this test is empty");

            string difference = ZoneDlzIncrementalTests.CompareZones(full, chunked, blockSize);
            Console.WriteLine(difference == "" ? $"{file} blockSize {blockSize}: identical ({full.Count} zones, {graded} graded)" : difference);
            Assert.AreEqual("", difference);
        }
        finally
        {
            GlobalData.Settings.Signal.ZonesDlz.ZoneStartApply = false;
        }
    }


    /// <summary>
    /// A restart halfway: the scanner stops, the committed zones and the cursor survive (the cursor
    /// in the candle database), the ZigZag does not - it is rebuilt from the candles in one batch -
    /// and the walk continues. The zones at the end have to be the zones of one full calculation.
    /// </summary>
    [TestMethod]
    [DataRow("SOLUSDT-1h.json", CryptoIntervalPeriod.interval1h, 2000)]
    [DataRow("XRPUSDT-15m.json", CryptoIntervalPeriod.interval15m, 2500)]
    public async Task ARestartHalfwayGivesTheSameZones(string file, CryptoIntervalPeriod period, int restartAt)
    {
        var (symbol, interval, candles) = LoadScenario(file, period);
        List<CryptoZone> full = await ZoneDlzIncrementalTests.Replay(symbol, interval, candles, 0);

        List<CryptoCandle> all = [.. candles.Values];
        ZoneCandleWindows loaded = new();
        List<CryptoZone> committed = [];
        List<CryptoZone> provisional = [];
        CandleTime? cursor = null;

        async Task Settle(ZigZagIndicator indicator)
        {
            indicator.FinishBatch();
            List<ZigZagResult> settledPivots = [];
            List<ZigZagResult> provisionalPivots = [];
            cursor = await ZoneDlz.CalculateDlzAsync(null, symbol, interval, indicator, loaded,
                cursor, settledPivots, provisionalPivots);
            ZoneDlz.CreateZonesFromZigZag(symbol, interval, settledPivots, committed);
            provisional = [];
            ZoneDlz.CreateZonesFromZigZag(symbol, interval, provisionalPivots, provisional);
        }

        // Before the restart: one candle per call, the emulator's rhythm
        ZigZagIndicator before = new(TrendType.Primary, false);
        for (int i = 0; i < restartAt; i++)
        {
            before.Calculate(all[i], batchProcess: true);
            before.LastFedCandleTime = all[i].OpenTime;
            await Settle(before);
        }

        // The restart: a fresh ZigZag over the candles seen so far, the store and the cursor kept
        ZigZagIndicator after = new(TrendType.Primary, false);
        for (int i = 0; i < restartAt; i++)
        {
            after.Calculate(all[i], batchProcess: true);
            after.LastFedCandleTime = all[i].OpenTime;
        }
        await Settle(after);

        // After the restart: blocks of ten candles
        for (int i = restartAt; i < all.Count; i++)
        {
            after.Calculate(all[i], batchProcess: true);
            after.LastFedCandleTime = all[i].OpenTime;
            if ((i - restartAt) % 10 == 9 || i == all.Count - 1)
                await Settle(after);
        }

        string difference = ZoneDlzIncrementalTests.CompareZones(full, [.. committed, .. provisional], -restartAt);
        Console.WriteLine(difference == "" ? $"{file} restart at {restartAt}: identical ({full.Count} zones)" : difference);
        Assert.AreEqual("", difference);
    }
}
