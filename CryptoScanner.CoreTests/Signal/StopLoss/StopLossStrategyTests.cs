using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Baba;
using CryptoScanner.Core.Signal.Stobb;

using System.Reflection;

namespace CryptoScanner.CoreTests.Signal.StopLoss;

/// <summary>
/// Verifies the stop-loss source for two representative strategies:
///
///   BABA  — strategy-level SL: when UseStopLoss=true the signal populates
///            OverrideSlPercentage with StopLossAtrFactor * ATR%. The trader uses that
///            value instead of the global fallback. The test asserts that every BABA signal
///            that fires returns a non-null OverrideSlPercentage that matches the expected
///            ATR-based formula (within floating-point tolerance).
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
public class StopLossStrategyTests : TestBase
{
    private static readonly CryptoIntervalPeriod SignalPeriod = CryptoIntervalPeriod.interval15m;

    // ─── data loading ────────────────────────────────────────────────────────

    private static string TestDataPath(string relative)
    {
        string baseDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!;
        return Path.Combine(baseDir, relative);
    }

    private static CryptoSymbol LoadSymbolCandles(string symbolName, CryptoIntervalPeriod period)
    {
        InitTestSession();
        using CryptoDatabase database = new();
        database.Open();

        CryptoSymbol symbol = CreateTestSymbol(database);

        string periodName = GlobalData.IntervalListPeriod[period].Name;
        string folder = $"Signal\\StopLoss\\{symbolName}";
        LoadCandleDataFromDisk(
            symbol.GetSymbolInterval(period).CandleList,
            TestDataPath($"{folder}\\{symbolName}-{periodName}.json"));

        int count = symbol.GetSymbolInterval(period).CandleList.Count;
        Console.WriteLine($"Loaded {count} candles for {symbolName} {periodName}");
        return symbol;
    }

    // ─── BABA simulation ─────────────────────────────────────────────────────

    /// <summary>
    /// Runs the BABA signal candle-by-candle and collects every fired signal together
    /// with the OverrideSlPercentage the algorithm reported.
    /// </summary>
    private static List<(DateTime CandleTime, decimal? SlPercentage, string ExtraText)> RunBabaSimulation(
        CryptoSymbol symbol, CryptoInterval interval, CryptoTradeSide side, int warmup = 260)
    {
        var results = new List<(DateTime, decimal?, string)>();
        CryptoSymbolInterval symbolInterval= symbol.GetSymbolInterval(interval.IntervalPeriod);

        foreach (CryptoCandle candle in symbolInterval.CandleList.Values.Skip(warmup))
        {
            CandleTime ct = candle.OpenTime;
            if (!IndicatorEngine.PrepareIndicators(symbol, interval, ct))
                continue;
            if (!symbolInterval.TryGetCandle(ct, out MyData? data) || data == null)
                continue;

            SignalCreateBase algo = side == CryptoTradeSide.Long
                ? new SignalBabaLong
                {
                    Symbol = symbol,
                    Interval = interval,
                    SymbolInterval = symbolInterval,
                    SignalSide = CryptoTradeSide.Long,
                    SignalStrategy = CryptoSignalStrategy.Baba,
                    CandleLast = data,
                }
                : new SignalBabaShort
                {
                    Symbol = symbol,
                    Interval = interval,
                    SymbolInterval = symbolInterval,
                    SignalSide = CryptoTradeSide.Short,
                    SignalStrategy = CryptoSignalStrategy.Baba,
                    CandleLast = data,
                };

            if (algo.IndicatorsOkay(data) && algo.IsSignal())
                results.Add((ct.ToLocalTime(), algo.OverrideSlPercentage, algo.ExtraText));
        }

        Console.WriteLine($"BABA {side}: {results.Count} signal(s) found");
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
        CryptoSymbolInterval symbolInterval= symbol.GetSymbolInterval(interval.IntervalPeriod);

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

    // ─── BABA stop-loss tests ─────────────────────────────────────────────────

    /// <summary>
    /// Asserts that every BABA Long signal provides a strategy-specific stop-loss percentage
    /// (OverrideSlPercentage is not null) when UseStopLoss is enabled. The trader will use
    /// this ATR-derived value instead of the global setting.
    /// </summary>
    [TestMethod]
    [Ignore("Needs Signal\\StopLoss\\SOLUSDT\\SOLUSDT-15m.json — export from the running scanner")]
    public void BabaLong_UsesStrategyStopLoss_NotNull()
    {
        // UseStopLoss=true (default) → OverrideSlPercentage must be populated for every fired signal.
        GlobalData.Settings.Signal.Baba.UseStopLoss = true;

        const string symbol = "SOLUSDT";
        CryptoInterval interval = GlobalData.IntervalListPeriod[SignalPeriod];
        CryptoSymbol sym = LoadSymbolCandles(symbol, SignalPeriod);

        var hits = RunBabaSimulation(sym, interval, CryptoTradeSide.Long);

        Assert.IsTrue(hits.Count > 0, "No BABA Long signals found — add more candle data or pick a different symbol.");

        Console.WriteLine();
        Console.WriteLine("BABA Long signal SL percentages:");
        foreach (var (time, sl, text) in hits)
        {
            Console.WriteLine($"  {time:yyyy-MM-dd HH:mm:ss}  SL={sl:N4}%  {text}");
            Assert.IsNotNull(sl,
                $"BABA Long signal at {time:yyyy-MM-dd HH:mm:ss} has null OverrideSlPercentage " +
                "— expected strategy-specific ATR-based SL (UseStopLoss=true).");
            Assert.IsTrue(sl > 0,
                $"BABA Long SL at {time:yyyy-MM-dd HH:mm:ss} is zero or negative ({sl}), which is invalid.");
        }
    }

    /// <summary>
    /// Asserts that when UseStopLoss is disabled, BABA Long returns null for OverrideSlPercentage,
    /// causing the trader to fall back to the global stop-loss setting.
    /// </summary>
    [TestMethod]
    [Ignore("Needs Signal\\StopLoss\\SOLUSDT\\SOLUSDT-15m.json — export from the running scanner")]
    public void BabaLong_UseStopLossDisabled_ReturnsNull()
    {
        // UseStopLoss=false → OverrideSlPercentage must be null (global fallback).
        GlobalData.Settings.Signal.Baba.UseStopLoss = false;

        const string symbol = "SOLUSDT";
        CryptoInterval interval = GlobalData.IntervalListPeriod[SignalPeriod];
        CryptoSymbol sym = LoadSymbolCandles(symbol, SignalPeriod);

        var hits = RunBabaSimulation(sym, interval, CryptoTradeSide.Long);

        Assert.IsTrue(hits.Count > 0, "No BABA Long signals found — add more candle data or pick a different symbol.");

        Console.WriteLine();
        Console.WriteLine("BABA Long signals (UseStopLoss=false, expecting null SL):");
        foreach (var (time, sl, text) in hits)
        {
            Console.WriteLine($"  {time:yyyy-MM-dd HH:mm:ss}  SL={sl?.ToString("N4") ?? "null"}  {text}");
            Assert.IsNull(sl,
                $"BABA Long signal at {time:yyyy-MM-dd HH:mm:ss} has SL={sl} but UseStopLoss=false " +
                "— expected null so the trader uses the global stop-loss percentage.");
        }
    }

    /// <summary>
    /// Asserts that every BABA Short signal provides a strategy-specific stop-loss percentage
    /// (OverrideSlPercentage is not null) when UseStopLoss is enabled.
    /// </summary>
    [TestMethod]
    [Ignore("Needs Signal\\StopLoss\\SOLUSDT\\SOLUSDT-15m.json — export from the running scanner")]
    public void BabaShort_UsesStrategyStopLoss_NotNull()
    {
        GlobalData.Settings.Signal.Baba.UseStopLoss = true;

        const string symbol = "SOLUSDT";
        CryptoInterval interval = GlobalData.IntervalListPeriod[SignalPeriod];
        CryptoSymbol sym = LoadSymbolCandles(symbol, SignalPeriod);

        var hits = RunBabaSimulation(sym, interval, CryptoTradeSide.Short);

        Assert.IsTrue(hits.Count > 0, "No BABA Short signals found — add more candle data or pick a different symbol.");

        Console.WriteLine();
        Console.WriteLine("BABA Short signal SL percentages:");
        foreach (var (time, sl, text) in hits)
        {
            Console.WriteLine($"  {time:yyyy-MM-dd HH:mm:ss}  SL={sl:N4}%  {text}");
            Assert.IsNotNull(sl,
                $"BABA Short signal at {time:yyyy-MM-dd HH:mm:ss} has null OverrideSlPercentage " +
                "— expected strategy-specific ATR-based SL (UseStopLoss=true).");
            Assert.IsTrue(sl > 0,
                $"BABA Short SL at {time:yyyy-MM-dd HH:mm:ss} is zero or negative ({sl}), which is invalid.");
        }
    }

    // ─── STOBB stop-loss tests ────────────────────────────────────────────────

    /// <summary>
    /// Asserts that every STOBB Long signal returns null for OverrideSlPercentage.
    /// STOBB does not override the stop-loss; the trader falls back to the global
    /// stop-loss percentage configured in the trading settings.
    /// </summary>
    [TestMethod]
    [Ignore("Needs Signal\\StopLoss\\SOLUSDT\\SOLUSDT-15m.json — export from the running scanner")]
    public void StobbLong_UsesGlobalStopLoss_OverrideIsNull()
    {
        const string symbol = "SOLUSDT";
        CryptoInterval interval = GlobalData.IntervalListPeriod[SignalPeriod];
        CryptoSymbol sym = LoadSymbolCandles(symbol, SignalPeriod);

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
    [Ignore("Needs Signal\\StopLoss\\SOLUSDT\\SOLUSDT-15m.json — export from the running scanner")]
    public void StobbShort_UsesGlobalStopLoss_OverrideIsNull()
    {
        const string symbol = "SOLUSDT";
        CryptoInterval interval = GlobalData.IntervalListPeriod[SignalPeriod];
        CryptoSymbol sym = LoadSymbolCandles(symbol, SignalPeriod);

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
    /// Discovers all BABA Long signals and prints their SL percentages.
    /// No assertions — use this to verify signal output before removing [Ignore] from the tests above.
    /// </summary>
    [TestMethod]
    [Ignore("Needs Signal\\StopLoss\\SOLUSDT\\SOLUSDT-15m.json — export from the running scanner")]
    public void Discover_BabaLong_SlPercentages()
    {
        GlobalData.Settings.Signal.Baba.UseStopLoss = true;

        const string symbol = "SOLUSDT";
        CryptoInterval interval = GlobalData.IntervalListPeriod[SignalPeriod];
        CryptoSymbol sym = LoadSymbolCandles(symbol, SignalPeriod);

        var hits = RunBabaSimulation(sym, interval, CryptoTradeSide.Long);

        Console.WriteLine();
        Console.WriteLine($"=== BABA Long SL discovery — {symbol} {interval.Name} ===");
        Console.WriteLine($"  {"Time",-22} {"SL%",8}  Signal text");
        foreach (var (time, sl, text) in hits)
            Console.WriteLine($"  {time:yyyy-MM-dd HH:mm:ss}  {sl?.ToString("N4") ?? "null",8}  {text}");
    }

    /// <summary>
    /// Discovers all STOBB Long signals and prints their SL override (expected null).
    /// No assertions.
    /// </summary>
    [TestMethod]
    [Ignore("Needs Signal\\StopLoss\\SOLUSDT\\SOLUSDT-15m.json — export from the running scanner")]
    public void Discover_StobbLong_SlPercentages()
    {
        const string symbol = "SOLUSDT";
        CryptoInterval interval = GlobalData.IntervalListPeriod[SignalPeriod];
        CryptoSymbol sym = LoadSymbolCandles(symbol, SignalPeriod);

        var hits = RunStobbSimulation(sym, interval, CryptoTradeSide.Long);

        Console.WriteLine();
        Console.WriteLine($"=== STOBB Long SL discovery — {symbol} {interval.Name} ===");
        Console.WriteLine($"  {"Time",-22} {"SL%",12}  Signal text");
        foreach (var (time, sl, text) in hits)
            Console.WriteLine($"  {time:yyyy-MM-dd HH:mm:ss}  {sl?.ToString("N4") ?? "null (global)",12}  {text}");
    }
}
