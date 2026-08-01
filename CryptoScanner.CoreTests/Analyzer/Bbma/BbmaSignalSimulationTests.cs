using CryptoScanner.Analyzers.Bbma.Signal;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

using System.Reflection;
using System.Text;
// BbmaState enum lives in SignalBbmaBase; GetBbmaState/Short are in the derived classes.
using static CryptoScanner.Analyzers.Bbma.Signal.SignalBbmaBase;

namespace CryptoScanner.CoreTests.Analyzer.Bbma;

/// <summary>
/// Candle-by-candle BBMA signal simulation.
///
/// How it works
/// ────────────
/// For every completed LTF candle (after the 260-candle warm-up period) the test
/// instantiates a fresh SignalBbmaLong / SignalBbmaShort, recalculates indicators
/// for that specific candle window (sliding), and calls IsSignal() directly.
/// This is identical to what the production scanner does — minus all the production
/// side-effects (barometers, blacklists, trading, database writes, etc.).
///
/// Data files
/// ──────────
/// Each symbol sub-folder under Analyzer\Bbma\ must contain three JSON files with
/// candle history in CryptoCandleList format (same format as the ZigZag tests):
///
///   Analyzer\Bbma\ADAUSDT\ADAUSDT-5m.json
///   Analyzer\Bbma\ADAUSDT\ADAUSDT-15m.json
///   Analyzer\Bbma\ADAUSDT\ADAUSDT-1h.json
///
///
/// To create these files, export candle data from the running scanner using the
/// standard JSON serializer (JsonSerializer.Serialize) on a CryptoCandleList.
/// A minimum of ~300 candles per interval is recommended (260 warm-up + margin).
///
/// Workflow
/// ────────
/// 1. Run DiscoverSignals_Long / DiscoverSignals_Short to see all signals found
///    in your data set. Output goes to Console (visible in the test runner).
/// 2. Copy interesting timestamps into checkpoint tests (see CheckpointExample).
/// 3. Run checkpoint tests as regression guard after code changes.
/// </summary>
[TestClass]
[DoNotParallelize]
public class BbmaSignalSimulationTests : TestBase
{
    [TestInitialize]
    public void SkipOutsideDebug()
    {
        // BBMA reads Ema50/Atr14/Wma05Low/Wma05High/Wma10Low/Wma10High off CryptoData, which
        // IntervalIndicatorHub only computes inside #if DEBUG (see IntervalIndicatorHub.cs) — the
        // same gate that keeps the BBMA strategy itself DEBUG-only in AnalyzerRegistration.RegisterAll.
        // In a Release build those fields stay null all the way through, so every simulation here
        // silently finds 0 signals regardless of the data — not a real pass/fail signal.
#if !DEBUG
        Assert.Inconclusive("BBMA simulation requires a DEBUG build (Ema50/Atr14/Wma05/10 are DEBUG-only in IntervalIndicatorHub).");
#endif
    }

    // ─── timeframe pair for the 5m LTF BBMA setup ────────────────────────────
    private static readonly CryptoIntervalPeriod LtfPeriod = CryptoIntervalPeriod.interval5m;
    private static readonly CryptoIntervalPeriod MtfPeriod = CryptoIntervalPeriod.interval15m;
    private static readonly CryptoIntervalPeriod HtfPeriod = CryptoIntervalPeriod.interval1h;

    // ─── data ────────────────────────────────────────────────────────────────

    /// <summary>
    /// A single fired signal: the LTF candle time and the ExtraText code/description.
    /// </summary>
    private record SignalHit(DateTime CandleTimeLocal, string ExtraText);

    // ─── setup helpers ───────────────────────────────────────────────────────

    private static string TestDataPath(string relative)
    {
        string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine(baseDir, relative);
    }

    /// <summary>
    /// Loads candle data for a symbol from three JSON files (LTF / MTF / HTF).
    /// Returns the symbol and the three resolved interval objects.
    /// </summary>
    private static (CryptoSymbol symbol, CryptoInterval ltf, CryptoInterval mtf, CryptoInterval htf)
        LoadTestData(string symbolName)
    {
        InitTestSession();
        GlobalData.Settings.Signal.UseNewIndicatorHub = true;
        using CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = CreateTestSymbol(database);
        ResetIndicatorState(symbol);

        CryptoInterval ltf = GlobalData.IntervalListPeriod[LtfPeriod];
        CryptoInterval mtf = GlobalData.IntervalListPeriod[MtfPeriod];
        CryptoInterval htf = GlobalData.IntervalListPeriod[HtfPeriod];

        string folder = $"Analyzer\\Bbma\\{symbolName}";
        LoadCandleDataFromDisk(
            symbol.GetSymbolInterval(LtfPeriod).CandleList,
            TestDataPath($"{folder}\\{symbolName}-5m.json"));
        LoadCandleDataFromDisk(
            symbol.GetSymbolInterval(MtfPeriod).CandleList,
            TestDataPath($"{folder}\\{symbolName}-15m.json"));
        LoadCandleDataFromDisk(
            symbol.GetSymbolInterval(HtfPeriod).CandleList,
            TestDataPath($"{folder}\\{symbolName}-1h.json"));

        Console.WriteLine($"Loaded: LTF {symbol.GetSymbolInterval(LtfPeriod).CandleList.Count} candles" +
                          $" | MTF {symbol.GetSymbolInterval(MtfPeriod).CandleList.Count} candles" +
                          $" | HTF {symbol.GetSymbolInterval(HtfPeriod).CandleList.Count} candles");
        return (symbol, ltf, mtf, htf);
    }

    // ─── core simulation ─────────────────────────────────────────────────────

    /// <summary>
    /// Runs the BBMA algorithm candle-by-candle over the full LTF candle list.
    ///
    /// For each completed LTF candle (after the 260-candle warm-up):
    ///   1. Calculates LTF indicators using a fresh, per-candle indicator list
    ///      (= sliding window — identical to the production scanner).
    ///   2. Instantiates SignalBbmaLong or SignalBbmaShort with all required properties.
    ///   3. Calls IsSignal() directly (no production side-effects).
    ///   4. Collects every signal hit.
    ///
    /// Note: A fresh CryptoIndicatorDataList is created for every candle so that
    /// indicators are always recalculated for the correct time window.
    /// MTF/HTF indicators are calculated on demand inside IsSignal() via
    /// CalculateIndicatorsForInterval — same as production.
    /// </summary>
    private static List<SignalHit> RunSimulation(
        CryptoSymbol symbol, CryptoInterval ltf, CryptoTradeSide side,
        int warmupCandles = 260)
    {
        var hits = new List<SignalHit>();
        CryptoSymbolInterval ltfSymbolInterval = symbol.GetSymbolInterval(ltf.IntervalPeriod);

        // Skip the first warmupCandles candles — not enough history for indicators yet.
        List<CryptoCandle> candles = ltfSymbolInterval.CandleList.Values
            .Skip(warmupCandles)
            .ToList();

        int processed = 0;
        foreach (CryptoCandle candle in candles)
        {
            CandleTime candleTime = candle.OpenTime;

            // Calculate LTF indicators up to this candle (persisted on the symbol's CryptoSymbolInterval).
            if (!IndicatorEngine.PrepareIndicators(symbol, ltf, candleTime))
                continue;

            // The current candle must have indicator data.
            if (!ltfSymbolInterval.TryGetCandle(candleTime, out MyData? candleLast) || candleLast == null)
                continue;

            // Build algorithm instance — mirrors what SignalCreate.ExecuteAlgorithmAsync does.
            SignalCreateBase algorithm = side == CryptoTradeSide.Long
                ? new SignalBbmaLong
                {
                    Symbol = symbol,
                    Interval = ltf,
                    SymbolInterval = ltfSymbolInterval,
                    SignalSide = CryptoTradeSide.Long,
                    SignalStrategy = CryptoSignalStrategy.BbmaOmni,
                    CandleLast = candleLast,
                }
                : new SignalBbmaShort
                {
                    Symbol = symbol,
                    Interval = ltf,
                    SymbolInterval = ltfSymbolInterval,
                    SignalSide = CryptoTradeSide.Short,
                    SignalStrategy = CryptoSignalStrategy.BbmaOmni,
                    CandleLast = candleLast,
                };

            if (algorithm.IndicatorsOkay(candleLast) && algorithm.IsSignal())
            {
                var hit = new SignalHit(candleTime.ToLocalTime(), algorithm.ExtraText);
                hits.Add(hit);
            }

            processed++;
        }

        Console.WriteLine($"Processed {processed} LTF candles → {hits.Count} signal(s) found");
        return hits;
    }

    // ─── state dump (diagnostic) ─────────────────────────────────────────────

    /// <summary>
    /// Prints the BBMA state of every LTF candle to the console.
    /// Useful when investigating why a signal did or did not fire.
    /// </summary>
    private static void DumpBbmaStates(CryptoSymbol symbol, CryptoInterval ltf,
        int warmupCandles = 260, int maxRows = 200)
    {
        CryptoSymbolInterval ltfSymbolInterval = symbol.GetSymbolInterval(ltf.IntervalPeriod);
        List<CryptoCandle> candles = ltfSymbolInterval.CandleList.Values
            .Skip(warmupCandles)
            .TakeLast(maxRows)
            .ToList();

        var sb = new StringBuilder();
        sb.AppendLine($"{"Time",-22} {"Long",-14} {"Short",-14} {"Close",12} {"Wma5L",10} {"Wma10L",10} {"Wma5H",10} {"Wma10H",10} {"BB.Low",10} {"BB.Mid",10} {"BB.Up",10}");
        sb.AppendLine(new string('-', 130));

        foreach (CryptoCandle candle in candles)
        {
            CandleTime candleTime = candle.OpenTime;
            if (!IndicatorEngine.PrepareIndicators(symbol, ltf, candleTime))
                continue;
            if (!ltfSymbolInterval.TryGetCandle(candleTime, out MyData? myData) || myData == null)
                continue;

            var cd = myData.CandleData;
            if (cd.Sma20 == null || cd.BollingerBandsDeviation == null || cd.Ema50 == null
                || cd.Wma05Low == null || cd.Wma10Low == null || cd.Wma05High == null || cd.Wma10High == null)
                continue;

            BbmaState longState = SignalBbmaLong.GetBbmaState(myData);
            BbmaState shortState = SignalBbmaShort.GetBbmaState(myData);

            decimal bbMid = (decimal)cd.Sma20!.Value;
            decimal bbDev = (decimal)cd.BollingerBandsDeviation!.Value;
            decimal bbLower = bbMid - bbDev;
            decimal bbUpper = bbMid + bbDev;

            sb.AppendLine(
                $"{candleTime.ToLocalTime(),-22}" +
                $" {longState,-14}" +
                $" {shortState,-14}" +
                $" {candle.Close,12:N4}" +
                $" {(decimal)cd.Wma05Low!.Value,10:N4}" +
                $" {(decimal)cd.Wma10Low!.Value,10:N4}" +
                $" {(decimal)cd.Wma05High!.Value,10:N4}" +
                $" {(decimal)cd.Wma10High!.Value,10:N4}" +
                $" {bbLower,10:N4}" +
                $" {bbMid,10:N4}" +
                $" {bbUpper,10:N4}");
        }

        Console.WriteLine(sb.ToString());
    }

    // ─── discover tests ──────────────────────────────────────────────────────

    /// <summary>
    /// Runs the full Long simulation and prints every signal hit to the console.
    /// Use this to discover interesting timestamps for checkpoint tests.
    ///
    /// No assertions — always passes. Output is visible in the test runner output.
    /// </summary>
    [TestMethod]
    public void DiscoverSignals_Long_ADAUSDT()
    {
        const string symbol = "ADAUSDT";
        var (sym, ltf, _, _) = LoadTestData(symbol);

        Console.WriteLine($"=== BBMA LONG signals — {symbol} {ltf.Name} ===");
        List<SignalHit> hits = RunSimulation(sym, ltf, CryptoTradeSide.Long);

        Console.WriteLine();
        Console.WriteLine("Summary:");
        foreach (var hit in hits)
            Console.WriteLine($"  {hit.CandleTimeLocal:yyyy-MM-dd HH:mm:ss}  {hit.ExtraText}");
    }

    /// <summary>
    /// Runs the full Short simulation and prints every signal hit to the console.
    /// </summary>
    [TestMethod]
    public void DiscoverSignals_Short_ADAUSDT()
    {
        const string symbol = "ADAUSDT";
        var (sym, ltf, _, _) = LoadTestData(symbol);

        Console.WriteLine($"=== BBMA SHORT signals — {symbol} {ltf.Name} ===");
        List<SignalHit> hits = RunSimulation(sym, ltf, CryptoTradeSide.Short);

        Console.WriteLine();
        Console.WriteLine("Summary:");
        foreach (var hit in hits)
            Console.WriteLine($"  {hit.CandleTimeLocal:yyyy-MM-dd HH:mm:ss}  {hit.ExtraText}");
    }

    /// <summary>
    /// Dumps the BBMA state for the last 200 LTF candles — useful when debugging
    /// why a specific signal did or did not fire.
    /// </summary>
    [TestMethod]
    public void DumpBbmaStates_ADAUSDT()
    {
        const string symbol = "ADAUSDT";
        var (sym, ltf, _, _) = LoadTestData(symbol);

        Console.WriteLine($"=== BBMA state dump — {symbol} {ltf.Name} (last 200 candles) ===");
        DumpBbmaStates(sym, ltf, maxRows: 200);
    }

    // ─── checkpoint / regression tests ───────────────────────────────────────

    /// <summary>
    /// Example checkpoint test: asserts that a specific Long signal fires at a
    /// known candle timestamp and that no unexpected extra signals appear.
    ///
    /// Workflow:
    ///   1. Run DiscoverSignals_Long to find signal timestamps.
    ///   2. Copy the UTC times into expectedTimes below.
    ///   3. Rename the test and adjust the symbol/date filter as needed.
    ///
    /// The test is currently marked [Ignore] because it requires real data files.
    /// Remove [Ignore] once the JSON files are in place and expectedTimes is filled.
    /// </summary>
    [TestMethod]
    public void CheckpointTest_Long_ADAUSDT_KnownSignals()
    {
        const string symbol = "ADAUSDT";
        var (sym, ltf, _, _) = LoadTestData(symbol);

        List<SignalHit> hits = RunSimulation(sym, ltf, CryptoTradeSide.Long);

        // DiscoverSignals_Long found 0 signals in this dataset.
        Assert.AreEqual(0, hits.Count, "Unexpected Long signals appeared — regression?");
    }

    [TestMethod]
    public void CheckpointTest_Short_ADAUSDT_KnownSignals()
    {
        const string symbol = "ADAUSDT";
        var (sym, ltf, _, _) = LoadTestData(symbol);

        List<SignalHit> hits = RunSimulation(sym, ltf, CryptoTradeSide.Short);

        Console.WriteLine($"Short signals found: {hits.Count}");
        foreach (var hit in hits)
            Console.WriteLine($"  {hit.CandleTimeLocal:yyyy-MM-dd HH:mm:ss}  {hit.ExtraText}");

        // Verify at least 1 signal fires in this dataset.
        Assert.IsTrue(hits.Count >= 1, "Expected at least 1 Short signal in ADAUSDT dataset");

        // Known signal from DiscoverSignals_Short (local time).
        DateTime expected = new(2026, 2, 1, 20, 10, 0);
        var hitTimes = hits.Select(h => h.CandleTimeLocal).ToHashSet();
        Assert.IsTrue(hitTimes.Contains(expected),
            $"Expected Short signal at {expected:yyyy-MM-dd HH:mm:ss} but it was not found. " +
            $"Found: {string.Join(", ", hitTimes.Select(t => t.ToString("yyyy-MM-dd HH:mm:ss")))}");
    }
}
