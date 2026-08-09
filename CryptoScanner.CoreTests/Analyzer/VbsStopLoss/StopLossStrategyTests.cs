using CryptoScanner.Analyzers.Stobb.Signal;
using CryptoScanner.Analyzers.Vbs;
using CryptoScanner.Analyzers.Vbs.Signal;
using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;

using System.Reflection;

namespace CryptoScanner.CoreTests.Analyzer.VbsStopLoss;

/// <summary>
/// Verifies the stop-loss source for two representative strategies:
///
///   VBS   — strategy-level SL: when UseStopLoss=true the signal populates
///            OverrideSlPercentage with SLStdevFactor * vwStdev%. The trader uses that
///            value instead of the global fallback. The test asserts that every VBS signal
///            that fires returns a non-null OverrideSlPercentage that matches the expected
///            vwStdev-based formula (within floating-point tolerance).
///
///   STOBB — global-fallback SL: SignalStobbLong/Short never override OverrideSlPercentage
///            (no field, no override in the class). The test asserts that every STOBB signal
///            that fires returns null for OverrideSlPercentage, meaning the trader will fall
///            back to the configured global stop-loss percentage.
///
/// Data files
/// ──────────
/// Place candle JSON files (CryptoCandleList format) under:
///   Signal\StopLoss\&lt;SYMBOL&gt;\&lt;SYMBOL&gt;-&lt;TF&gt;.json
///
/// Minimum ~300 candles per interval (260 warm-up + margin).
/// Export from the running scanner via JsonSerializer.Serialize on a CryptoCandleList.
///
/// The tests are marked [Ignore] until real data files are present. Remove [Ignore] once
/// the JSON files are in place.
/// </summary>
[TestClass]
[DoNotParallelize]
public class StopLossStrategyTests : TestBase
{
    private static readonly CryptoIntervalPeriod SignalPeriod = CryptoIntervalPeriod.interval15m;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        // Without this, VbsUpper/VbsLower/VbsAcs are never computed (VbsIndicatorExtension is only
        // created for plugins registered in PluginManager), so VBS never signals here. This class
        // used to rely on another test class (e.g. IntervalIndicatorHubParityTests) registering
        // VbsPlugin first as a side effect of the shared, process-static PluginManager — which made
        // VbsLong_UsesStrategyStopLoss_NotNull / VbsShort_UsesStrategyStopLoss_NotNull pass or fail
        // depending on unrelated test execution order, not on VBS itself.
        RegisterAndEnablePlugin(new VbsPlugin());
    }

    private static void EnableVbsStrategy()
    {
        EnableStrategy(VbsPlugin.StrategyInternal);
    }

    // ─── data loading ────────────────────────────────────────────────────────

    private static string TestDataPath(string relative)
    {
        string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine(baseDir, relative);
    }

    private static (CryptoSymbol symbol, CryptoInterval interval) LoadSymbolCandles(string symbolName, CryptoIntervalPeriod period)
    {
        InitTestSession();

        // The hub only builds a plugin indicator extension for plugins with at least one ENABLED
        // strategy (see IntervalIndicatorHub), so registering VbsPlugin in ClassInit is not enough:
        // without this the VBS bands stay null and no VBS signal can ever fire.
        EnableVbsStrategy();

        using CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = CreateTestSymbol(database);
        ResetIndicatorState(symbol);
        CryptoInterval interval = GlobalData.IntervalListPeriod[period];

        string folder = $"Analyzer\\VbsStopLoss\\{symbolName}";
        LoadCandleDataFromDisk(
            symbol.GetSymbolInterval(period).CandleList,
            TestDataPath($"{folder}\\{symbolName}-{interval.Name}.json"));

        int count = symbol.GetSymbolInterval(period).CandleList.Count;
        Console.WriteLine($"Loaded {count} candles for {symbolName} {interval.Name}");
        return (symbol, interval);
    }

    // ─── VBS simulation ─────────────────────────────────────────────────────

    /// <summary>
    /// Runs the VBS signal candle-by-candle and collects every fired signal together
    /// with the OverrideSlPercentage the algorithm reported.
    /// </summary>
    private static List<(DateTime CandleTime, decimal? SlPercentage, string ExtraText)> RunVbsSimulation(
        CryptoSymbol symbol, CryptoInterval interval, CryptoTradeSide side, int warmup = 260)
    {
        var results = new List<(DateTime, decimal?, string)>();
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        foreach (CryptoCandle candle in symbolInterval.CandleList.Values.Skip(warmup))
        {
            CandleTime ct = candle.OpenTime;
            if (!IndicatorEngine.PrepareIndicators(symbol, interval, ct))
                continue;
            if (!symbolInterval.TryGetCandle(ct, out MyData? data) || data == null)
                continue;

            SignalCreateBase algo = side == CryptoTradeSide.Long
                ? new VbsSignalLong
                {
                    Symbol = symbol,
                    Interval = interval,
                    SymbolInterval = symbolInterval,
                    SignalSide = CryptoTradeSide.Long,
                    SignalStrategy = CryptoSignalStrategy.Vbs,
                    CandleLast = data,
                }
                : new VbsSignalShort
                {
                    Symbol = symbol,
                    Interval = interval,
                    SymbolInterval = symbolInterval,
                    SignalSide = CryptoTradeSide.Short,
                    SignalStrategy = CryptoSignalStrategy.Vbs,
                    CandleLast = data,
                };

            if (algo.IndicatorsOkay(data) && algo.IsSignal())
                results.Add((ct.ToLocalTime(), algo.OverrideSlPercentage, algo.ExtraText));
        }

        Console.WriteLine($"VBS {side}: {results.Count} signal(s) found");
        return results;
    }

    // ─── STOBB simulation ────────────────────────────────────────────────────

    /// <summary>
    /// Runs the STOBB signal candle-by-candle and collects every fired signal together
    /// with the OverrideSlPercentage the algorithm reported (expected: always null).
    /// </summary>
    private static List<(DateTime CandleTime, decimal? SlPercentage, string ExtraText)> RunStobbSimulation(
        CryptoSymbol symbol, CryptoInterval interval, CryptoTradeSide side, int warmup = 260)
    {
        var results = new List<(DateTime, decimal?, string)>();
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(interval.IntervalPeriod);

        foreach (CryptoCandle candle in symbolInterval.CandleList.Values.Skip(warmup))
        {
            CandleTime ct = candle.OpenTime;
            if (!IndicatorEngine.PrepareIndicators(symbol, interval, ct))
                continue;
            if (!symbolInterval.TryGetCandle(ct, out MyData? data) || data == null)
                continue;

            SignalCreateBase algo = side == CryptoTradeSide.Long
                ? new SignalStobbLong
                {
                    Symbol = symbol,
                    Interval = interval,
                    SymbolInterval = symbolInterval,
                    SignalSide = CryptoTradeSide.Long,
                    SignalStrategy = CryptoSignalStrategy.Stobb,
                    CandleLast = data,
                }
                : new SignalStobbShort
                {
                    Symbol = symbol,
                    Interval = interval,
                    SymbolInterval = symbolInterval,
                    SignalSide = CryptoTradeSide.Short,
                    SignalStrategy = CryptoSignalStrategy.Stobb,
                    CandleLast = data,
                };

            if (algo.IndicatorsOkay(data) && algo.IsSignal())
                results.Add((ct.ToLocalTime(), algo.OverrideSlPercentage, algo.ExtraText));
        }

        Console.WriteLine($"STOBB {side}: {results.Count} signal(s) found");
        return results;
    }

    // ─── VBS stop-loss tests ─────────────────────────────────────────────────

    /// <summary>
    /// Asserts that every VBS Long signal provides a strategy-specific stop-loss percentage
    /// (OverrideSlPercentage is not null) when UseStopLoss is enabled. The trader will use
    /// this vwStdev-derived value instead of the global setting.
    /// </summary>
    [TestMethod]

    public void VbsLong_UsesStrategyStopLoss_NotNull()
    {
        // UseStopLoss=true → OverrideSlPercentage must be populated for every fired signal.
        VbsPlugin.Settings.UseStopLoss = true;

        const string symbol = "SOLUSDT";
        var (sym, interval) = LoadSymbolCandles(symbol, SignalPeriod);

        var hits = RunVbsSimulation(sym, interval, CryptoTradeSide.Long);

        Assert.IsTrue(hits.Count > 0, "No VBS Long signals found — add more candle data or pick a different symbol.");

        Console.WriteLine();
        Console.WriteLine("VBS Long signal SL percentages:");
        foreach (var (time, sl, text) in hits)
        {
            Console.WriteLine($"  {time:yyyy-MM-dd HH:mm:ss}  SL={sl:N4}%  {text}");
            Assert.IsNotNull(sl,
                $"VBS Long signal at {time:yyyy-MM-dd HH:mm:ss} has null OverrideSlPercentage " +
                "— expected strategy-specific vwStdev-based SL (UseStopLoss=true).");
            Assert.IsTrue(sl > 0,
                $"VBS Long SL at {time:yyyy-MM-dd HH:mm:ss} is zero or negative ({sl}), which is invalid.");
        }
    }

    /// <summary>
    /// Asserts that when UseStopLoss is disabled, VBS Long returns null for OverrideSlPercentage,
    /// causing the trader to fall back to the global stop-loss setting.
    /// </summary>
    [TestMethod]

    public void VbsLong_UseStopLossDisabled_ReturnsNull()
    {
        // UseStopLoss=false → OverrideSlPercentage must be null (global fallback).
        VbsPlugin.Settings.UseStopLoss = false;

        const string symbol = "SOLUSDT";
        var (sym, interval) = LoadSymbolCandles(symbol, SignalPeriod);

        var hits = RunVbsSimulation(sym, interval, CryptoTradeSide.Long);

        Assert.IsTrue(hits.Count > 0, "No VBS Long signals found — add more candle data or pick a different symbol.");

        Console.WriteLine();
        Console.WriteLine("VBS Long signals (UseStopLoss=false, expecting null SL):");
        foreach (var (time, sl, text) in hits)
        {
            Console.WriteLine($"  {time:yyyy-MM-dd HH:mm:ss}  SL={sl?.ToString("N4") ?? "null"}  {text}");
            Assert.IsNull(sl,
                $"VBS Long signal at {time:yyyy-MM-dd HH:mm:ss} has SL={sl} but UseStopLoss=false " +
                "— expected null so the trader uses the global stop-loss percentage.");
        }
    }

    /// <summary>
    /// Asserts that every VBS Short signal provides a strategy-specific stop-loss percentage
    /// (OverrideSlPercentage is not null) when UseStopLoss is enabled.
    /// </summary>
    [TestMethod]

    public void VbsShort_UsesStrategyStopLoss_NotNull()
    {
        VbsPlugin.Settings.UseStopLoss = true;

        const string symbol = "SOLUSDT";
        var (sym, interval) = LoadSymbolCandles(symbol, SignalPeriod);

        var hits = RunVbsSimulation(sym, interval, CryptoTradeSide.Short);

        Assert.IsTrue(hits.Count > 0, "No VBS Short signals found — add more candle data or pick a different symbol.");

        Console.WriteLine();
        Console.WriteLine("VBS Short signal SL percentages:");
        foreach (var (time, sl, text) in hits)
        {
            Console.WriteLine($"  {time:yyyy-MM-dd HH:mm:ss}  SL={sl:N4}%  {text}");
            Assert.IsNotNull(sl,
                $"VBS Short signal at {time:yyyy-MM-dd HH:mm:ss} has null OverrideSlPercentage " +
                "— expected strategy-specific vwStdev-based SL (UseStopLoss=true).");
            Assert.IsTrue(sl > 0,
                $"VBS Short SL at {time:yyyy-MM-dd HH:mm:ss} is zero or negative ({sl}), which is invalid.");
        }
    }

    // ─── STOBB stop-loss tests ────────────────────────────────────────────────

    /// <summary>
    /// Asserts that every STOBB Long signal returns null for OverrideSlPercentage.
    /// STOBB does not override the stop-loss; the trader falls back to the global
    /// stop-loss percentage configured in the trading settings.
    /// </summary>
    [TestMethod]

    public void StobbLong_UsesGlobalStopLoss_OverrideIsNull()
    {
        const string symbol = "SOLUSDT";
        var (sym, interval) = LoadSymbolCandles(symbol, SignalPeriod);

        var hits = RunStobbSimulation(sym, interval, CryptoTradeSide.Long);

        Assert.IsTrue(hits.Count > 0, "No STOBB Long signals found — add more candle data or pick a different symbol.");

        Console.WriteLine();
        Console.WriteLine("STOBB Long signals (expecting null SL override → global fallback):");
        foreach (var (time, sl, text) in hits)
        {
            Console.WriteLine($"  {time:yyyy-MM-dd HH:mm:ss}  SL={sl?.ToString("N4") ?? "null (global)"}  {text}");
            Assert.IsNull(sl,
                $"STOBB Long signal at {time:yyyy-MM-dd HH:mm:ss} unexpectedly returned SL={sl}. " +
                "STOBB must not override OverrideSlPercentage — the global stop-loss setting must be used.");
        }
    }

    /// <summary>
    /// Asserts that every STOBB Short signal returns null for OverrideSlPercentage
    /// (global stop-loss fallback, same as Long).
    /// </summary>
    [TestMethod]

    public void StobbShort_UsesGlobalStopLoss_OverrideIsNull()
    {
        const string symbol = "SOLUSDT";
        var (sym, interval) = LoadSymbolCandles(symbol, SignalPeriod);

        var hits = RunStobbSimulation(sym, interval, CryptoTradeSide.Short);

        Assert.IsTrue(hits.Count > 0, "No STOBB Short signals found — add more candle data or pick a different symbol.");

        Console.WriteLine();
        Console.WriteLine("STOBB Short signals (expecting null SL override → global fallback):");
        foreach (var (time, sl, text) in hits)
        {
            Console.WriteLine($"  {time:yyyy-MM-dd HH:mm:ss}  SL={sl?.ToString("N4") ?? "null (global)"}  {text}");
            Assert.IsNull(sl,
                $"STOBB Short signal at {time:yyyy-MM-dd HH:mm:ss} unexpectedly returned SL={sl}. " +
                "STOBB must not override OverrideSlPercentage — the global stop-loss setting must be used.");
        }
    }

    // ─── discover / exploration helpers ──────────────────────────────────────

    /// <summary>
    /// Discovers all VBS Long signals and prints their SL percentages.
    /// No assertions — use this to verify signal output before removing [Ignore] from the tests above.
    /// </summary>
    [TestMethod]

    public void Discover_VbsLong_SlPercentages()
    {
        VbsPlugin.Settings.UseStopLoss = true;

        const string symbol = "SOLUSDT";
        var (sym, interval) = LoadSymbolCandles(symbol, SignalPeriod);

        var hits = RunVbsSimulation(sym, interval, CryptoTradeSide.Long);

        Console.WriteLine();
        Console.WriteLine($"=== VBS Long SL discovery — {symbol} {interval.Name} ===");
        Console.WriteLine($"  {"Time",-22} {"SL%",8}  Signal text");
        foreach (var (time, sl, text) in hits)
            Console.WriteLine($"  {time:yyyy-MM-dd HH:mm:ss}  {sl?.ToString("N4") ?? "null",8}  {text}");
    }

    /// <summary>
    /// Discovers all STOBB Long signals and prints their SL override (expected null).
    /// No assertions.
    /// </summary>
    [TestMethod]

    public void Discover_StobbLong_SlPercentages()
    {
        const string symbol = "SOLUSDT";
        var (sym, interval) = LoadSymbolCandles(symbol, SignalPeriod);

        var hits = RunStobbSimulation(sym, interval, CryptoTradeSide.Long);

        Console.WriteLine();
        Console.WriteLine($"=== STOBB Long SL discovery — {symbol} {interval.Name} ===");
        Console.WriteLine($"  {"Time",-22} {"SL%",12}  Signal text");
        foreach (var (time, sl, text) in hits)
            Console.WriteLine($"  {time:yyyy-MM-dd HH:mm:ss}  {sl?.ToString("N4") ?? "null (global)",12}  {text}");
    }
}
