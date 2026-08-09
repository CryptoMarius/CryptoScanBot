using CryptoScanner.Analyzers.Bbma;
using CryptoScanner.Analyzers.Nwe;
using CryptoScanner.Analyzers.Nwe.Signal;
using CryptoScanner.Analyzers.Vbs;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Indicators;

using Skender.Stock.Indicators;

using System.Reflection;
using System.Text.Json;

namespace CryptoScanner.CoreTests.Analyzer.Indicators;

/// <summary>
/// Regression tests that pin Skender indicator output for 2500 deterministic candles.
///
/// The values are pinned in a JSON file on disk. Every run recomputes them three ways and
/// compares each against that file, so a Skender upgrade or a code change that alters any
/// indicator value fails a test immediately.
///
/// Three test phases:
/// 1. Batch    → reference file  (an independent Skender batch computation, local to this test)
/// 2. Hub      → reference file  (same candles through IntervalIndicatorHub)
/// 3. Pipeline → reference file  (IndicatorEngine.PrepareIndicators on a test symbol)
///
/// Note that phase 1 is this test's OWN batch computation. The production batch path
/// (IndicatorData.PrepareViaBatch) no longer exists — the hub is the only path the scanner uses —
/// so phase 1 exists purely to keep an implementation-independent cross-check against the hub.
///
/// The reference also pins the PLUGIN indicators (VBS bands + ACS, NWE repainting and
/// non-repainting). Only the hub produces those, which makes them the part of the safety net that
/// matters most now that the batch path is gone. Their reference values are computed here with the
/// plugins' own batch implementations (VbsBandsHelper.ComputeBands / NweIndicator.Calculate), so
/// this is an independent check and not merely a recording of whatever the hub produced.
/// </summary>
[TestClass]
[DoNotParallelize]
public class SkenderReferenceRegressionTests
{
    private const int CandleCount = 2500;
    private const byte TickDec = 4;
    private const double Tolerance = 1e-6;
    // The pipeline warms up from a 260-candle window, so recursive indicators (EMA, RSI, ATR,
    // MACD signal) carry a seed-truncation difference vs. the reference (computed over all 2500).
    private const double PipelineTolerance = 1e-3;
    // The plugin indicators are far more sensitive to that same 260-candle warm-up window:
    // NWE regresses over a 500-candle kernel (so the pipeline sees roughly half of it) and
    // VbsAtrSl is an RMA over 50 periods seeded from 260 candles instead of the full history.
    // Both drift ~2e-3 relative, which is still tight enough to catch a missing or structurally
    // wrong indicator (those show up as infinity or orders of magnitude).
    private const double PluginPipelineTolerance = 1e-2;

    private static string ReferenceFilePath =>
        Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!,
            "Analyzer", "Indicators", "indicator_reference.json");

    // ── Snapshot data model ──────────────────────────────────────────────

    private class IndicatorSnapshot
    {
        public int Index { get; set; }
        public double? Sma20 { get; set; }
        public double? Sma50 { get; set; }
        public double? Sma100 { get; set; }
        public double? Sma200 { get; set; }
        public double? Rsi { get; set; }
        public double? MacdValue { get; set; }
        public double? MacdSignal { get; set; }
        public double? MacdHistogram { get; set; }
        public double? StochOscillator { get; set; }
        public double? StochSignal { get; set; }
        public double? PSar { get; set; }
        public double? BbDeviation { get; set; }
        public double? BbPercentage { get; set; }
        public double? Ema50 { get; set; }
        public double? Wma05Low { get; set; }
        public double? Wma05High { get; set; }
        public double? Wma10Low { get; set; }
        public double? Wma10High { get; set; }
        public double? Atr14 { get; set; }
        public short? Lux5mValue { get; set; }

        // ── Plugin indicators ────────────────────────────────────────────
        // Only the hub can produce these (the batch path has no plugin support), which is exactly
        // why they belong in the reference: once the batch path is gone this file is the only
        // thing standing between a broken plugin indicator and a silently signal-less scanner.
        public double? VbsBasis { get; set; }
        public double? VbsUpper { get; set; }
        public double? VbsLower { get; set; }
        public double? VbsVwStdev { get; set; }
        public double? VbsAcs { get; set; }
        public double? VbsAtrSl { get; set; }

        public double? NweCenter { get; set; }
        public double? NweUpper { get; set; }
        public double? NweLower { get; set; }
        public double? NweNpCenter { get; set; }
        public double? NweNpUpper { get; set; }
        public double? NweNpLower { get; set; }
    }

    private class IndicatorReferenceData
    {
        public int CandleCount { get; set; }
        public byte TickDecimals { get; set; }
        public List<IndicatorSnapshot> Snapshots { get; set; } = [];
    }

    // ── Candle generation (deterministic, identical across runs) ──────────

    private static List<CryptoCandle> MakeCandles(int count)
    {
        var list = new List<CryptoCandle>(count);
        decimal prevClose = 100m;
        for (int i = 0; i < count; i++)
        {
            double mid = 100 + 15 * Math.Sin(i * 0.05) + 5 * Math.Sin(i * 0.23)
                       + 3 * Math.Cos(i * 0.11) + (i % 7) * 0.10;
            decimal close = Math.Round((decimal)mid, TickDec);
            decimal high = close + 0.50m + (i % 5) * 0.05m;
            decimal low = close - 0.50m - (i % 3) * 0.05m;
            list.Add(new CryptoCandle
            {
                TickDecimals = TickDec,
                OpenTime = new CandleTime((uint)(i * 15)),  // 15m candles
                Open = prevClose,
                High = high,
                Low = low,
                Close = close,
                Volume = 1000m + (i % 13) * 50m,
            });
            prevClose = close;
        }
        return list;
    }

    // ── Batch indicator computation (serves as reference baseline) ────────

    private static IndicatorReferenceData ComputeBatchIndicators(List<CryptoCandle> candles)
    {
        IReadOnlyList<IQuote> quotes = candles.AsQuotes();
        var g = GlobalData.Settings.General;
        var bb = quotes.ToBollingerBands(g.SettingsBb.Length, g.SettingsBb.Deviation).ToList();
        var sma50 = quotes.ToSma(50).ToList();
        var sma100 = quotes.ToSma(100).ToList();
        var sma200 = quotes.ToSma(200).ToList();
        var rsi = quotes.ToRsi(g.SettingsRsi.Length).ToList();
        var macd = quotes.ToMacd(12, 26, 9).ToList();
        var stoch = quotes.ToStoch(g.SettingsStoch.Length, g.SettingsStoch.SmoothingD, g.SettingsStoch.SmoothingK).ToList();
        var psar = quotes.ToParabolicSar(0.02, 0.2).ToList();

        var ema50 = quotes.ToEma(50).ToList();
        var wma05Low = quotes.Use(CandlePart.Low).ToWma(5).ToList();
        var wma05High = quotes.Use(CandlePart.High).ToWma(5).ToList();
        var wma10Low = quotes.Use(CandlePart.Low).ToWma(10).ToList();
        var wma10High = quotes.Use(CandlePart.High).ToWma(10).ToList();
        var atr14 = quotes.ToAtr(14).ToList();

        // Lux Multi-RSI: manual RMA computation identical to IntervalIndicatorHub.Add
        var luxValues = ComputeLuxBatch(candles);

        // Plugin indicators, computed here with the plugins' own batch implementations — an
        // independent reference, not a copy of whatever the hub happens to produce.
        VbsBandsHelper.BandValue[] vbsBands = VbsBandsHelper.ComputeBands(candles);
        var vbsAtrSl = quotes.ToAtr(VbsPlugin.Settings.Length).ToList();
        (double?[] repCenter, double?[] repUpper, double?[] repLower) = ComputeNweBatch(candles, smoothRepainting: true);
        (double?[] npCenter, double?[] npUpper, double?[] npLower) = ComputeNweBatch(candles, smoothRepainting: false);

        var reference = new IndicatorReferenceData { CandleCount = candles.Count, TickDecimals = TickDec };

        for (int i = 0; i < candles.Count; i++)
        {
            reference.Snapshots.Add(new IndicatorSnapshot
            {
                Index = i,
                Sma20 = bb[i].Sma,
                BbDeviation = 0.5 * (bb[i].UpperBand - bb[i].LowerBand),
                BbPercentage = 100 * (bb[i].UpperBand / bb[i].LowerBand - 1),
                Sma50 = sma50[i].Sma,
                Sma100 = sma100[i].Sma,
                Sma200 = sma200[i].Sma,
                Rsi = rsi[i].Rsi,
                MacdValue = macd[i].Macd,
                MacdSignal = macd[i].Signal,
                MacdHistogram = macd[i].Histogram,
                StochOscillator = stoch[i].Oscillator,
                StochSignal = stoch[i].Signal,
                PSar = psar[i].Sar,
                Ema50 = ema50[i].Ema,
                Wma05Low = wma05Low[i].Wma,
                Wma05High = wma05High[i].Wma,
                Wma10Low = wma10Low[i].Wma,
                Wma10High = wma10High[i].Wma,
                Atr14 = atr14[i].Atr,
                Lux5mValue = luxValues[i],

                VbsBasis = vbsBands[i].HasValue ? vbsBands[i].Basis : null,
                VbsUpper = vbsBands[i].HasValue ? vbsBands[i].Upper : null,
                VbsLower = vbsBands[i].HasValue ? vbsBands[i].Lower : null,
                VbsVwStdev = vbsBands[i].HasValue ? vbsBands[i].VwStdev : null,
                VbsAcs = vbsBands[i].HasValue ? vbsBands[i].Acs : null,
                VbsAtrSl = vbsAtrSl[i].Atr,

                NweCenter = repCenter[i],
                NweUpper = repUpper[i],
                NweLower = repLower[i],
                NweNpCenter = npCenter[i],
                NweNpUpper = npUpper[i],
                NweNpLower = npLower[i],
            });
        }

        return reference;
    }

    /// <summary>
    /// NWE reference: replays the indicator exactly as NweIndicatorExtension does — a rolling
    /// buffer capped at Length+50 candles, recalculated on every candle, reading the last result.
    /// </summary>
    private static (double?[] center, double?[] upper, double?[] lower) ComputeNweBatch(
        List<CryptoCandle> candles, bool smoothRepainting)
    {
        var s = NwePlugin.Settings;
        var indicator = new NweIndicator(bandwidth: s.BandWidth, multiplier: s.Multiplication,
            smoothRepainting: smoothRepainting);

        int n = candles.Count;
        var center = new double?[n];
        var upper = new double?[n];
        var lower = new double?[n];

        // Same rolling buffer the extension keeps (Length + 50), fed through the public
        // Calculate(CryptoCandleList) so no internals have to be exposed to the test project.
        var window = new CryptoCandleList();
        var order = new List<CandleTime>(600);
        int maxBuffer = indicator.Length + 50;

        for (int i = 0; i < n; i++)
        {
            window.TryAdd(candles[i].OpenTime, candles[i]);
            order.Add(candles[i].OpenTime);
            if (order.Count > maxBuffer)
            {
                int trim = order.Count - indicator.Length;
                for (int t = 0; t < trim; t++)
                    window.Remove(order[t]);
                order.RemoveRange(0, trim);
            }

            var results = indicator.Calculate(window);
            var last = results[^1];
            center[i] = last.Center.HasValue ? (double)last.Center.Value : null;
            upper[i] = last.Upper.HasValue ? (double)last.Upper.Value : null;
            lower[i] = last.Lower.HasValue ? (double)last.Lower.Value : null;
        }

        return (center, upper, lower);
    }

    /// <summary>
    /// Register and enable the plugins whose indicators are part of the reference. The hub only
    /// builds a plugin indicator extension for plugins with at least one ENABLED strategy, so
    /// registering alone is not enough — that is precisely the trap this reference now guards.
    /// </summary>
    private static void RegisterAndEnablePlugins()
    {
        TestBase.RegisterAndEnablePlugin(new VbsPlugin());
        TestBase.RegisterAndEnablePlugin(new NwePlugin());
        // BbmaPlugin declares Ema50 / Wma05Low+High / Wma10Low+High / Atr14, so registering it is
        // what makes the hub build them. They used to appear only in a DEBUG build. Its own
        // strategies do not have to be enabled: declared indicators follow registration, not
        // enabling — only an IIndicatorExtension needs an enabled strategy.
        TestBase.RegisterPlugin(new BbmaPlugin());
    }

    private static IndicatorSnapshot CryptoDataToSnapshot(int index, CryptoData data)
    {
        // Plugin values no longer live on CryptoData itself; each plugin owns a typed slot.
        var vbs = data.GetPluginData<VbsCandleData>();
        var nwe = data.GetPluginData<NweCandleData>();

        return new IndicatorSnapshot
        {
            Index = index,
            Sma20 = data.Sma20,
            Sma50 = data.Sma50,
            Sma100 = data.Sma100,
            Sma200 = data.Sma200,
            Rsi = data.Rsi,
            MacdValue = data.MacdValue,
            MacdSignal = data.MacdSignal,
            MacdHistogram = data.MacdHistogram,
            StochOscillator = data.StochOscillator,
            StochSignal = data.StochSignal,
            PSar = data.PSar,
            BbDeviation = data.BollingerBandsDeviation,
            BbPercentage = data.BollingerBandsPercentage,
            Ema50 = data.Ema50,
            Wma05Low = data.Wma05Low,
            Wma05High = data.Wma05High,
            Wma10Low = data.Wma10Low,
            Wma10High = data.Wma10High,
            Atr14 = data.Atr14,
            Lux5mValue = data.Lux5mValue,

            VbsBasis = vbs?.Basis,
            VbsUpper = vbs?.Upper,
            VbsLower = vbs?.Lower,
            VbsVwStdev = vbs?.VwStdev,
            VbsAcs = vbs?.Acs,
            VbsAtrSl = vbs?.AtrSl,

            NweCenter = nwe?.Center,
            NweUpper = nwe?.Upper,
            NweLower = nwe?.Lower,
            NweNpCenter = nwe?.NpCenter,
            NweNpUpper = nwe?.NpUpper,
            NweNpLower = nwe?.NpLower,
        };
    }

    // ── Reference file I/O ───────────────────────────────────────────────

    private static IndicatorReferenceData LoadReference()
    {
        Assert.IsTrue(File.Exists(ReferenceFilePath),
            $"Reference file not found: {ReferenceFilePath}. " +
            "This file is committed in the repository and must not be regenerated. " +
            "If it was intentionally deleted (e.g. after a Skender major upgrade), " +
            "run GenerateReferenceFile to create a new baseline.");

        string json = File.ReadAllText(ReferenceFilePath);
        var reference = JsonSerializer.Deserialize<IndicatorReferenceData>(json);
        Assert.IsNotNull(reference, "Failed to deserialize reference file");
        Assert.AreEqual(CandleCount, reference.Snapshots.Count,
            $"Reference file has {reference.Snapshots.Count} snapshots, expected {CandleCount}");
        return reference;
    }

    /// <summary>
    /// Not a test — utility to (re)generate the reference file after an intentional Skender
    /// major version upgrade. Run manually from the test runner, then commit the new file.
    /// </summary>
    [TestMethod]
    [Ignore("Run manually to regenerate the reference file after an intentional Skender upgrade")]
    public void GenerateReferenceFile()
    {
        GlobalData.Settings = new SettingsBasic();
        RegisterAndEnablePlugins();
        List<CryptoCandle> candles = MakeCandles(CandleCount);
        IndicatorReferenceData reference = ComputeBatchIndicators(candles);

        Directory.CreateDirectory(Path.GetDirectoryName(ReferenceFilePath)!);
        string output = JsonSerializer.Serialize(reference, new JsonSerializerOptions { WriteIndented = false });
        File.WriteAllText(ReferenceFilePath, output);
        Console.WriteLine($"Reference file written: {ReferenceFilePath} ({reference.Snapshots.Count} snapshots, {output.Length:N0} bytes)");
    }

    // ── Comparison helpers ────────────────────────────────────────────────

    /// <param name="pluginDiffs">Where the plugin indicator differences are recorded. Pass null to
    /// skip them entirely (the batch path cannot compute them).</param>
    private static void CompareSnapshots(IndicatorSnapshot expected, IndicatorSnapshot actual,
        double tolerance, Dictionary<string, double> maxDiffs, Dictionary<string, double>? pluginDiffs)
    {
        Cmp("Sma20", expected.Sma20, actual.Sma20, tolerance, maxDiffs);
        Cmp("Sma50", expected.Sma50, actual.Sma50, tolerance, maxDiffs);
        Cmp("Sma100", expected.Sma100, actual.Sma100, tolerance, maxDiffs);
        Cmp("Sma200", expected.Sma200, actual.Sma200, tolerance, maxDiffs);
        Cmp("Rsi", expected.Rsi, actual.Rsi, tolerance, maxDiffs);
        Cmp("MacdValue", expected.MacdValue, actual.MacdValue, tolerance, maxDiffs);
        Cmp("MacdSignal", expected.MacdSignal, actual.MacdSignal, tolerance, maxDiffs);
        Cmp("MacdHistogram", expected.MacdHistogram, actual.MacdHistogram, tolerance, maxDiffs);
        Cmp("StochOscillator", expected.StochOscillator, actual.StochOscillator, tolerance, maxDiffs);
        Cmp("StochSignal", expected.StochSignal, actual.StochSignal, tolerance, maxDiffs);
        Cmp("PSar", expected.PSar, actual.PSar, tolerance, maxDiffs);
        Cmp("BbDeviation", expected.BbDeviation, actual.BbDeviation, tolerance, maxDiffs);
        Cmp("BbPercentage", expected.BbPercentage, actual.BbPercentage, tolerance, maxDiffs);
        Cmp("Ema50", expected.Ema50, actual.Ema50, tolerance, maxDiffs);
        Cmp("Wma05Low", expected.Wma05Low, actual.Wma05Low, tolerance, maxDiffs);
        Cmp("Wma05High", expected.Wma05High, actual.Wma05High, tolerance, maxDiffs);
        Cmp("Wma10Low", expected.Wma10Low, actual.Wma10Low, tolerance, maxDiffs);
        Cmp("Wma10High", expected.Wma10High, actual.Wma10High, tolerance, maxDiffs);
        Cmp("Atr14", expected.Atr14, actual.Atr14, tolerance, maxDiffs);
        // Lux was stored in the reference but never compared, so a stale value in the file went
        // unnoticed for a long time. It is a short?, hence the widening to double?.
        Cmp("Lux5mValue", expected.Lux5mValue, actual.Lux5mValue, tolerance, maxDiffs);

        // The batch path has no plugin support, so those fields are skipped when comparing it
        if (pluginDiffs == null)
            return;

        Cmp("VbsBasis", expected.VbsBasis, actual.VbsBasis, tolerance, pluginDiffs);
        Cmp("VbsUpper", expected.VbsUpper, actual.VbsUpper, tolerance, pluginDiffs);
        Cmp("VbsLower", expected.VbsLower, actual.VbsLower, tolerance, pluginDiffs);
        Cmp("VbsVwStdev", expected.VbsVwStdev, actual.VbsVwStdev, tolerance, pluginDiffs);
        Cmp("VbsAcs", expected.VbsAcs, actual.VbsAcs, tolerance, pluginDiffs);
        Cmp("VbsAtrSl", expected.VbsAtrSl, actual.VbsAtrSl, tolerance, pluginDiffs);

        Cmp("NweCenter", expected.NweCenter, actual.NweCenter, tolerance, pluginDiffs);
        Cmp("NweUpper", expected.NweUpper, actual.NweUpper, tolerance, pluginDiffs);
        Cmp("NweLower", expected.NweLower, actual.NweLower, tolerance, pluginDiffs);
        Cmp("NweNpCenter", expected.NweNpCenter, actual.NweNpCenter, tolerance, pluginDiffs);
        Cmp("NweNpUpper", expected.NweNpUpper, actual.NweNpUpper, tolerance, pluginDiffs);
        Cmp("NweNpLower", expected.NweNpLower, actual.NweNpLower, tolerance, pluginDiffs);
    }

    private static void Cmp(string field, short? expected, short? actual, double tolerance,
        Dictionary<string, double> maxDiffs)
    {
        Cmp(field, (double?)expected, (double?)actual, tolerance, maxDiffs);
    }

    private static void Cmp(string field, double? expected, double? actual, double tolerance,
        Dictionary<string, double> maxDiffs)
    {
        double rel;
        if (expected.HasValue != actual.HasValue)
            rel = double.PositiveInfinity;
        else if (!expected.HasValue)
            rel = 0;
        else
            rel = Math.Abs(expected.Value - actual.Value) /
                  Math.Max(Math.Max(Math.Abs(expected.Value), Math.Abs(actual.Value)), 1e-9);

        if (rel > maxDiffs.GetValueOrDefault(field))
            maxDiffs[field] = rel;
    }

    private static string Describe(Dictionary<string, double> maxDiffs) =>
        string.Join(", ", maxDiffs.Where(m => m.Value > 0)
            .OrderByDescending(m => m.Value)
            .Select(m => $"{m.Key}={m.Value:E2}"));

    // ── Test 1: Batch → reference (verify the batch path reproduces saved values) ──

    [TestMethod]
    public void Test1_Batch_Matches_Saved_Reference()
    {
        GlobalData.Settings = new SettingsBasic();
        RegisterAndEnablePlugins();
        IndicatorReferenceData reference = LoadReference();

        List<CryptoCandle> candles = MakeCandles(CandleCount);
        IndicatorReferenceData current = ComputeBatchIndicators(candles);

        Assert.AreEqual(reference.Snapshots.Count, current.Snapshots.Count, "Snapshot count mismatch");

        var maxDiffs = new Dictionary<string, double>();
        for (int i = 0; i < reference.Snapshots.Count; i++)
            CompareSnapshots(reference.Snapshots[i], current.Snapshots[i], Tolerance, maxDiffs, maxDiffs);

        double worst = maxDiffs.Values.DefaultIfEmpty(0).Max();
        Assert.IsTrue(worst <= Tolerance,
            $"Batch path diverged from saved reference (tolerance={Tolerance:E1}). " +
            $"Max relative diff per field: {Describe(maxDiffs)}");

        Console.WriteLine($"Batch vs reference: max diffs = {Describe(maxDiffs)}");
    }

    // ── Test 2: Hub → reference (verify hub incremental matches saved values) ──

    [TestMethod]
    public void Test2_Hub_Matches_Saved_Reference()
    {
        GlobalData.Settings = new SettingsBasic();
        RegisterAndEnablePlugins();
        IndicatorReferenceData reference = LoadReference();
        List<CryptoCandle> candles = MakeCandles(CandleCount);

        var hub = new IntervalIndicatorHub();
        var hubSnapshots = new List<IndicatorSnapshot>(candles.Count);

        for (int i = 0; i < candles.Count; i++)
        {
            hub.Add(candles[i]);
            hubSnapshots.Add(CryptoDataToSnapshot(i, hub.BuildCurrent()));
        }

        var maxDiffs = new Dictionary<string, double>();
        for (int i = 0; i < candles.Count; i++)
            CompareSnapshots(reference.Snapshots[i], hubSnapshots[i], Tolerance, maxDiffs, maxDiffs);

        double worst = maxDiffs.Values.DefaultIfEmpty(0).Max();
        Assert.IsTrue(worst <= Tolerance,
            $"Hub path diverged from saved reference (tolerance={Tolerance:E1}). " +
            $"Max relative diff per field: {Describe(maxDiffs)}");

        Console.WriteLine($"Hub vs reference: max diffs = {Describe(maxDiffs)}");
    }

    // ── Test 3: Pipeline → reference (PrepareIndicators on a test symbol) ──

    [TestMethod]
    public void Test3_Pipeline_Matches_Saved_Reference()
    {
        GlobalData.Settings = new SettingsBasic();
        RegisterAndEnablePlugins();
        IndicatorReferenceData reference = LoadReference();
        List<CryptoCandle> candles = MakeCandles(CandleCount);

        SetupIntervalList();

        CryptoInterval interval15m = GlobalData.IntervalListPeriod[CryptoIntervalPeriod.interval15m];
        CryptoSymbol symbol = CreateLightweightSymbol();
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval15m);

        foreach (CryptoCandle candle in candles)
            symbolInterval.CandleList.TryAdd(candle.OpenTime, candle);

        CandleTime lastCandleTime = candles[^1].OpenTime;

        symbolInterval.Data.Clear();
        symbolInterval.IndicatorHub = null;
        symbolInterval.IndicatorHubLastAdded = null;
        symbolInterval.IndicatorHubAddCount = 0;

        bool success = IndicatorEngine.PrepareIndicators(symbol, interval15m, lastCandleTime);
        Assert.IsTrue(success, "PrepareIndicators failed");
        Assert.IsTrue(symbolInterval.Data.ContainsKey(lastCandleTime), "No indicator data for last candle");

        CryptoData pipelineData = symbolInterval.Data[lastCandleTime];
        IndicatorSnapshot pipelineSnapshot = CryptoDataToSnapshot(candles.Count - 1, pipelineData);

        var maxDiffs = new Dictionary<string, double>();
        var pluginDiffs = new Dictionary<string, double>();
        CompareSnapshots(reference.Snapshots[candles.Count - 1], pipelineSnapshot, PipelineTolerance,
            maxDiffs, pluginDiffs);

        double worst = maxDiffs.Values.DefaultIfEmpty(0).Max();
        Assert.IsTrue(worst <= PipelineTolerance,
            $"Pipeline diverged from reference (tolerance={PipelineTolerance:E1}). " +
            $"Max relative diff per field: {Describe(maxDiffs)}");

        double worstPlugin = pluginDiffs.Values.DefaultIfEmpty(0).Max();
        Assert.IsTrue(worstPlugin <= PluginPipelineTolerance,
            $"Pipeline plugin indicators diverged from reference (tolerance={PluginPipelineTolerance:E1}). " +
            $"Max relative diff per field: {Describe(pluginDiffs)}");

        Console.WriteLine($"Pipeline vs reference: max diffs = {Describe(maxDiffs)}");
        Console.WriteLine($"Pipeline plugin indicators vs reference: max diffs = {Describe(pluginDiffs)}");
    }

    // ── Lightweight GlobalData setup (no DB) ─────────────────────────────

    private static void SetupIntervalList()
    {
        if (GlobalData.IntervalList.Count > 0)
            return;

        int id = 0;
        foreach (CryptoInterval interval in CryptoInterval.CreateStandardIntervalList())
        {
            interval.Id = id++;
            GlobalData.IntervalList.Add(interval);
            GlobalData.IntervalListId.Add(interval.Id, interval);
            GlobalData.IntervalListPeriodName.Add(interval.Name, interval);
            GlobalData.IntervalListPeriod.Add(interval.IntervalPeriod, interval);
        }
    }

    private static CryptoSymbol CreateLightweightSymbol()
    {
        var exchange = new CryptoScanner.Core.Model.CryptoExchange { Id = 1, Name = "TestExchange" };
        var quoteData = new CryptoQuoteData { Name = "USDT" };
        return new CryptoSymbol
        {
            Id = 1,
            Status = 1,
            Base = "TEST",
            Quote = "USDT",
            Name = "TESTUSDT",
            Exchange = exchange,
            ExchangeName = exchange.Name,
            QuoteData = quoteData,
            PriceDecimals = TickDec,
            PriceTickSize = 0.0001m,
            PriceMinimum = 0m,
            PriceMaximum = 0m,
            QuantityTickSize = 0.01m,
            QuantityMinimum = 0.01m,
            QuantityMaximum = 100000m,
            QuoteValueMinimum = 1m,
            QuoteValueMaximum = 200000m,
        };
    }

    // ── Lux Multi-RSI batch computation (mirrors IntervalIndicatorHub.Add) ───

    private const int LuxMin = 10;
    private const int LuxMax = 20;
    private const int LuxN = LuxMax - LuxMin + 1;

    private static short?[] ComputeLuxBatch(List<CryptoCandle> candles)
    {
        var result = new short?[candles.Count];
        var num = new double[LuxN];
        var den = new double[LuxN];
        double prevClose = 0;
        bool hasPrev = false;

        for (int i = 0; i < candles.Count; i++)
        {
            double close = (double)candles[i].Close;
            if (hasPrev)
            {
                double diff = close - prevClose;
                int overbuy = 0, oversell = 0;
                for (int j = 0; j < LuxN; j++)
                {
                    double alpha = 1.0 / (LuxMin + j);
                    num[j] = alpha * diff + (1.0 - alpha) * num[j];
                    den[j] = alpha * Math.Abs(diff) + (1.0 - alpha) * den[j];
                    double rsi = den[j] == 0.0 ? 50.0 : 50.0 * num[j] / den[j] + 50.0;
                    if (rsi > 70) overbuy++;
                    if (rsi < 30) oversell++;
                }
                int luxOversold = (int)(100.0 * oversell / LuxN);
                int luxOverbought = (int)(100.0 * overbuy / LuxN);
                int luxValue = 0;
                if (luxOverbought > 0) luxValue += luxOverbought;
                if (luxOversold > 0) luxValue -= luxOversold;
                result[i] = (short)luxValue;
            }
            else
            {
                // Hub always sets Lux5mValue (0 on the first candle before any diff is computed)
                result[i] = 0;
            }
            prevClose = close;
            hasPrev = true;
        }
        return result;
    }

    // ── Test 4: DEBUG indicators — batch vs hub parity ──────────────────

    [TestMethod]
    public void Test4_DebugIndicators_HubMatchesBatch()
    {
        GlobalData.Settings = new SettingsBasic();
        RegisterAndEnablePlugins();
        List<CryptoCandle> candles = MakeCandles(CandleCount);
        IndicatorReferenceData batchRef = ComputeBatchIndicators(candles);

        var hub = new IntervalIndicatorHub();
        var hubSnapshots = new List<IndicatorSnapshot>(candles.Count);
        for (int i = 0; i < candles.Count; i++)
        {
            hub.Add(candles[i]);
            hubSnapshots.Add(CryptoDataToSnapshot(i, hub.BuildCurrent()));
        }

        var maxDiffs = new Dictionary<string, double>();
        for (int i = 0; i < candles.Count; i++)
            CompareSnapshots(batchRef.Snapshots[i], hubSnapshots[i], Tolerance, maxDiffs, maxDiffs);

        // Verify DEBUG-only indicators are populated
        var last = batchRef.Snapshots[^1];
        Assert.IsNotNull(last.Ema50, "Ema50 must have a value for the last candle");
        Assert.IsNotNull(last.Wma05Low, "Wma05Low must have a value for the last candle");
        Assert.IsNotNull(last.Wma05High, "Wma05High must have a value for the last candle");
        Assert.IsNotNull(last.Wma10Low, "Wma10Low must have a value for the last candle");
        Assert.IsNotNull(last.Wma10High, "Wma10High must have a value for the last candle");
        Assert.IsNotNull(last.Atr14, "Atr14 must have a value for the last candle");

        double worstDebug = 0;
        foreach (var field in new[] { "Ema50", "Wma05Low", "Wma05High", "Wma10Low", "Wma10High", "Atr14" })
            if (maxDiffs.TryGetValue(field, out double d) && d > worstDebug)
                worstDebug = d;
        Assert.IsTrue(worstDebug <= Tolerance,
            $"DEBUG indicators diverged between batch and hub (tolerance={Tolerance:E1}). " +
            $"Max relative diff per field: {Describe(maxDiffs)}");
        Console.WriteLine($"DEBUG indicators batch vs hub: max diffs = {Describe(maxDiffs)}");

        double worst = maxDiffs.Values.DefaultIfEmpty(0).Max();
        Assert.IsTrue(worst <= Tolerance,
            $"Hub diverged from batch (tolerance={Tolerance:E1}). " +
            $"Max relative diff per field: {Describe(maxDiffs)}");
    }

    // ── Test 5: Lux Multi-RSI — hub matches batch reference implementation ──

    [TestMethod]
    public void Test5_LuxMultiRsi_HubMatchesBatch()
    {
        GlobalData.Settings = new SettingsBasic();
        RegisterAndEnablePlugins();
        List<CryptoCandle> candles = MakeCandles(CandleCount);
        short?[] batchLux = ComputeLuxBatch(candles);

        var hub = new IntervalIndicatorHub();
        int mismatches = 0;
        for (int i = 0; i < candles.Count; i++)
        {
            hub.Add(candles[i]);
            CryptoData data = hub.BuildCurrent();
            if (batchLux[i] != data.Lux5mValue)
                mismatches++;
        }

        Console.WriteLine($"Lux Multi-RSI: {mismatches} mismatches out of {candles.Count} candles");
        Assert.AreEqual(0, mismatches, "Lux Multi-RSI hub values must match the batch reference implementation");
    }

    // ── Test 6: Lux Multi-RSI produces non-zero values in trending data ──

    [TestMethod]
    public void Test6_LuxMultiRsi_ProducesNonZeroValues()
    {
        GlobalData.Settings = new SettingsBasic();
        RegisterAndEnablePlugins();
        List<CryptoCandle> candles = MakeCandles(CandleCount);
        short?[] luxValues = ComputeLuxBatch(candles);

        int nonZero = luxValues.Count(v => v.HasValue && v.Value != 0);
        Console.WriteLine($"Lux Multi-RSI: {nonZero} non-zero values out of {candles.Count} candles");
        Assert.IsTrue(nonZero > 100,
            $"Expected many non-zero Lux values in trending synthetic data, got only {nonZero}");

        int positive = luxValues.Count(v => v.HasValue && v.Value > 0);
        int negative = luxValues.Count(v => v.HasValue && v.Value < 0);
        Console.WriteLine($"Lux Multi-RSI: {positive} positive (overbought), {negative} negative (oversold)");
        Assert.IsTrue(positive > 0, "Expected at least some overbought Lux values");
        Assert.IsTrue(negative > 0, "Expected at least some oversold Lux values");
    }
}
