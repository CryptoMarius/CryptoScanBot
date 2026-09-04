using CryptoScanner.Analyzers.Bbma;
using CryptoScanner.Analyzers.MacdCross;
using CryptoScanner.Analyzers.Vbs;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal.Indicators;

using Skender.Stock.Indicators;

namespace CryptoScanner.CoreTests.Analyzer.Indicators;

/// <summary>
/// Cross-checks IntervalIndicatorHub against an independent Skender batch computation.
/// (The production batch path is gone; this local one only serves as a second opinion.)
///
/// Test 1 (mapping): the production <see cref="IntervalIndicatorHub"/> fed candle-by-candle produces the
/// same CryptoData as the Skender batch over the SAME series — i.e. BuildCurrent's field mapping matches
/// the batch fill-loop mapping below. Exact (≤1e-6 relative, all fields).
///
/// Test 2 (flow): the actual integrated flow. PrepareViaHub warms up on a 260-candle window and then feeds
/// ONE candle per close (a CONTINUOUS series); the batch reference recomputes a SLIDING 260-candle window each
/// close. Window-based indicators (SMA/BB/WMA/Stoch%K) are identical. Recursive indicators carry a tiny
/// seed-truncation difference because the batch only sees 260 candles of history while the hub sees all:
/// it decays below 1e-6 for the short periods (MACD-EMAs, RMA-based ATR/RSI) but for EMA(50) it is ~1e-4
/// relative (well under a price tick). The test asserts the whole flow agrees to &lt; 1e-3 relative and
/// reports the per-field maximum so the divergence is visible, not hidden.
///
/// Self-contained: synthetic candles + default settings, so it runs without the exchange/DB.
/// </summary>
[TestClass]
public class IntervalIndicatorHubParityTests
{
    private const int Window = 260;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        GlobalData.Settings = new SettingsBasic();
        // Register the VBS plugin AND enable it: registration alone is not enough, since the "NWE
        // optimalization" commit the hub only builds an extension for plugins that have at least one
        // ENABLED strategy, so an untouched SettingsBasic (which lists sbm/stobb/storsi, not vbs)
        // would leave every VBS field null here while the batch reference still computes them.
        TestBase.RegisterAndEnablePlugin(new VbsPlugin());
        // Declares Ema50 / Wma05 / Wma10 / Atr14 — without it the hub has no reason to build them.
        TestBase.RegisterPlugin(new BbmaPlugin());
        // Declares Adx14 for its trend-strength filters.
        TestBase.RegisterPlugin(new MacdCrossPlugin());
    }

    private static void EnableStrategy(string name)
    {
        if (!GlobalData.Settings.Signal.Long.Strategy.Contains(name))
            GlobalData.Settings.Signal.Long.Strategy.Add(name);
        if (!GlobalData.Settings.Signal.Short.Strategy.Contains(name))
            GlobalData.Settings.Signal.Short.Strategy.Add(name);
    }

    [TestMethod]
    public void Test1_Hub_Mapping_Matches_Batch_Over_Same_Series()
    {
        List<CryptoCandle> candles = MakeCandles(300);

        var hub = new IntervalIndicatorHub();
        var hubData = new List<CryptoData>(candles.Count);
        foreach (CryptoCandle candle in candles)
        {
            hub.Add(candle);
            hubData.Add(hub.BuildCurrent());
        }

        IReadOnlyList<IQuote> quotes = candles.AsQuotes();
        var maxRel = new Dictionary<string, double>();
        for (int i = 0; i < candles.Count; i++)
            Compare(hubData[i], BatchCryptoData(quotes, candles, i), maxRel);

        double worst = maxRel.Values.DefaultIfEmpty(0).Max();
        Assert.IsTrue(worst <= 1e-6, "Mapping must be exact. Max relative diff per field: " + Describe(maxRel));
    }

    [TestMethod]
    public void Test2_Hub_Flow_Matches_Batch_Flow_Over_A_Sequence()
    {
        List<CryptoCandle> candles = MakeCandles(560);

        // HUB flow — warm up the first window, then feed one candle per close (continuous), exactly like
        // IndicatorEngine.PrepareViaHub (warm-up once, then incremental Add).
        var hub = new IntervalIndicatorHub();
        var hubFlow = new Dictionary<int, CryptoData>();
        for (int i = 0; i < candles.Count; i++)
        {
            hub.Add(candles[i]);
            if (i >= Window - 1)
                hubFlow[i] = hub.BuildCurrent();
        }

        // BATCH flow — a fresh sliding 260-window per close, take the last candle's data, exactly like
        // the retired batch path did (CollectCandles window + batch each candle).
        var maxRel = new Dictionary<string, double>();
        for (int t = Window - 1; t < candles.Count; t++)
        {
            var windowCandles = candles.GetRange(t - Window + 1, Window);
            IReadOnlyList<IQuote> win = windowCandles.AsQuotes();
            Compare(hubFlow[t], BatchCryptoData(win, windowCandles, Window - 1), maxRel);
        }

        double worst = maxRel.Values.DefaultIfEmpty(0).Max();
        Assert.IsTrue(worst < 1e-3,
            "Flow must agree to <1e-3 relative (EMA50 seed-truncation is the only non-exact field). " +
            "Max relative diff per field: " + Describe(maxRel));
    }

    // ── batch reference: build the CryptoData at index i with the plain Skender batch calls ──
    private static CryptoData BatchCryptoData(IReadOnlyList<IQuote> quotes, List<CryptoCandle> candles, int i)
    {
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
        var atr14 = quotes.ToAtr(14).ToList();
        var adx14 = quotes.ToAdx(14).ToList();
        var wma05Low = quotes.Use(CandlePart.Low).ToWma(5).ToList();
        var wma05High = quotes.Use(CandlePart.High).ToWma(5).ToList();
        var wma10Low = quotes.Use(CandlePart.Low).ToWma(10).ToList();
        var wma10High = quotes.Use(CandlePart.High).ToWma(10).ToList();

        // VBS VWAP bands — same VbsBandsHelper.ComputeBands the hub path (via VbsIndicatorExtension) uses.
        var vbs = VbsPlugin.Settings;
        VbsBandsHelper.BandValue[] vbsBands = VbsBandsHelper.ComputeBands(candles);
        var atrVbsSl = quotes.ToAtr(vbs.Length).ToList();

        var result = new CryptoData
        {
            Sma20 = bb[i].Sma,
            BollingerBandsDeviation = 0.5 * (bb[i].UpperBand - bb[i].LowerBand),
            BollingerBandsPercentage = 100 * (bb[i].UpperBand / bb[i].LowerBand - 1),
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
            Atr14 = atr14[i].Atr,
            Adx14 = adx14[i].Adx,
            Wma05Low = wma05Low[i].Wma,
            Wma05High = wma05High[i].Wma,
            Wma10Low = wma10Low[i].Wma,
            Wma10High = wma10High[i].Wma,
        };

        // The VBS values live in the plugin's own slot, exactly as VbsIndicatorExtension writes them.
        result.SetPluginData(new VbsCandleData
        {
            AtrSl = atrVbsSl[i].Atr,
            Basis = vbsBands[i].HasValue ? vbsBands[i].Basis : null,
            Upper = vbsBands[i].HasValue ? vbsBands[i].Upper : null,
            Lower = vbsBands[i].HasValue ? vbsBands[i].Lower : null,
            VwStdev = vbsBands[i].HasValue ? vbsBands[i].VwStdev : null,
        });
        return result;
    }

    private static void Compare(CryptoData hub, CryptoData batch, Dictionary<string, double> maxRel)
    {
        Eq("Sma20", hub.Sma20, batch.Sma20, maxRel);
        Eq("BbDeviation", hub.BollingerBandsDeviation, batch.BollingerBandsDeviation, maxRel);
        Eq("BbPercentage", hub.BollingerBandsPercentage, batch.BollingerBandsPercentage, maxRel);
        Eq("Sma50", hub.Sma50, batch.Sma50, maxRel);
        Eq("Sma100", hub.Sma100, batch.Sma100, maxRel);
        Eq("Sma200", hub.Sma200, batch.Sma200, maxRel);
        Eq("Rsi", hub.Rsi, batch.Rsi, maxRel);
        Eq("MacdValue", hub.MacdValue, batch.MacdValue, maxRel);
        Eq("MacdSignal", hub.MacdSignal, batch.MacdSignal, maxRel);
        Eq("MacdHistogram", hub.MacdHistogram, batch.MacdHistogram, maxRel);
        Eq("StochOscillator", hub.StochOscillator, batch.StochOscillator, maxRel);
        Eq("StochSignal", hub.StochSignal, batch.StochSignal, maxRel);
        Eq("PSar", hub.PSar, batch.PSar, maxRel);
        var hubVbs = hub.GetPluginData<VbsCandleData>();
        var batchVbs = batch.GetPluginData<VbsCandleData>();
        Eq("VbsAtrSl", hubVbs?.AtrSl, batchVbs?.AtrSl, maxRel);
        Eq("VbsBasis", hubVbs?.Basis, batchVbs?.Basis, maxRel);
        Eq("VbsUpper", hubVbs?.Upper, batchVbs?.Upper, maxRel);
        Eq("VbsLower", hubVbs?.Lower, batchVbs?.Lower, maxRel);
        Eq("VbsVwStdev", hubVbs?.VwStdev, batchVbs?.VwStdev, maxRel);
        Eq("Ema50", hub.Ema50, batch.Ema50, maxRel);
        Eq("Atr14", hub.Atr14, batch.Atr14, maxRel);
        Eq("Adx14", hub.Adx14, batch.Adx14, maxRel);
        Eq("Wma05Low", hub.Wma05Low, batch.Wma05Low, maxRel);
        Eq("Wma05High", hub.Wma05High, batch.Wma05High, maxRel);
        Eq("Wma10Low", hub.Wma10Low, batch.Wma10Low, maxRel);
        Eq("Wma10High", hub.Wma10High, batch.Wma10High, maxRel);
    }

    private static void Eq(string field, double? a, double? b, Dictionary<string, double> maxRel)
    {
        double rel;
        if (a.HasValue != b.HasValue)
            rel = double.PositiveInfinity;
        else if (!a.HasValue)
            rel = 0;
        else
            rel = Math.Abs(a.Value - b!.Value) / Math.Max(Math.Max(Math.Abs(a.Value), Math.Abs(b.Value)), 1e-9);

        if (rel > maxRel.GetValueOrDefault(field))
            maxRel[field] = rel;
    }

    private static string Describe(Dictionary<string, double> maxRel) =>
        string.Join(", ", maxRel.Where(m => m.Value > 0).OrderByDescending(m => m.Value).Select(m => $"{m.Key}={m.Value:E2}"));

    [TestMethod]
    public void Test3_Hub_Lux_Matches_Batch_Lux()
    {
        List<CryptoCandle> candles = MakeCandles5m(400);

        // Hub: feed all candles incrementally.
        var hub = new IntervalIndicatorHub();
        var hubLux = new List<short?>(candles.Count);
        foreach (CryptoCandle candle in candles)
        {
            hub.Add(candle);
            hubLux.Add(hub.BuildCurrent().Lux5mValue);
        }

        // Batch reference: mirror the LuxIndicator.CalculateNew algorithm over the same series.
        int mismatches = 0;
        int compared = 0;
        for (int t = 100; t < candles.Count; t++)
        {
            int startIdx = t - 99;
            int luxMin = 10, luxMax = 20, luxN = luxMax - luxMin + 1;
            double[] num = new double[luxN];
            double[] den = new double[luxN];
            int overbuy = 0, oversell = 0;
            double prevClose = 0;
            bool hasPrev = false;

            for (int j = startIdx; j <= t; j++)
            {
                double close = (double)candles[j].Close;
                if (hasPrev)
                {
                    double diff = close - prevClose;
                    overbuy = 0;
                    oversell = 0;
                    for (int k = 0; k < luxN; k++)
                    {
                        double alpha = 1.0 / (luxMin + k);
                        num[k] = alpha * diff + (1.0 - alpha) * num[k];
                        den[k] = alpha * Math.Abs(diff) + (1.0 - alpha) * den[k];
                        double rsi = den[k] == 0.0 ? 50.0 : 50.0 * num[k] / den[k] + 50.0;
                        if (rsi > 70) overbuy++;
                        if (rsi < 30) oversell++;
                    }
                }
                prevClose = close;
                hasPrev = true;
            }

            int batchOversold = (int)(100.0 * oversell / luxN);
            int batchOverbought = (int)(100.0 * overbuy / luxN);
            int batchValue = 0;
            if (batchOverbought > 0) batchValue += batchOverbought;
            if (batchOversold > 0) batchValue -= batchOversold;

            short hubValue = hubLux[t] ?? 0;
            if (hubValue != (short)batchValue)
                mismatches++;
            compared++;
        }

        // The hub sees ALL prior candles (continuous RMA), while the batch only sees the last 100.
        // This means the hub's RMA warmup uses the full history, whereas the batch restarts
        // from zero each time. For early windows this can cause minor divergence (±1 count at
        // the 70/30 RSI thresholds). The assertion allows a small mismatch percentage.
        double mismatchPct = 100.0 * mismatches / compared;
        Assert.IsTrue(mismatchPct < 5.0,
            $"Hub Lux should closely match batch Lux. Mismatches: {mismatches}/{compared} ({mismatchPct:F1}%). " +
            $"Small differences are expected due to RMA warmup divergence (hub sees full history, batch sees 100 candles).");
    }

    private static List<CryptoCandle> MakeCandles5m(int count)
    {
        var list = new List<CryptoCandle>(count);
        decimal prevClose = 100m;
        for (int i = 0; i < count; i++)
        {
            double mid = 100 + 10 * Math.Sin(i * 0.10) + 3 * Math.Sin(i * 0.37) + (i % 7) * 0.10;
            decimal close = Math.Round((decimal)mid, 2);
            decimal high = close + 0.50m + (i % 5) * 0.05m;
            decimal low = close - 0.50m - (i % 3) * 0.05m;
            list.Add(new CryptoCandle
            {
                TickDecimals = 2,
                OpenTime = new CandleTime((uint)(i * 5)),
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

    /// <summary>Deterministic synthetic candle series with enough variation for every indicator.</summary>
    private static List<CryptoCandle> MakeCandles(int count)
    {
        var list = new List<CryptoCandle>(count);
        decimal prevClose = 100m;
        for (int i = 0; i < count; i++)
        {
            double mid = 100 + 10 * Math.Sin(i * 0.10) + 3 * Math.Sin(i * 0.37) + (i % 7) * 0.10;
            decimal close = Math.Round((decimal)mid, 2);
            decimal high = close + 0.50m + (i % 5) * 0.05m;
            decimal low = close - 0.50m - (i % 3) * 0.05m;
            list.Add(new CryptoCandle
            {
                TickDecimals = 2,
                OpenTime = new CandleTime((uint)(i * 15)),
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

    /// <summary>
    /// The registry contract: a registered plugin always gets the indicators it declares, and the
    /// hub builds nothing beyond the base set plus those declarations.
    ///
    /// This is what replaced the hand-maintained #if DEBUG coupling. Ema50/Wma/Atr14 exist here
    /// because BbmaPlugin (registered in ClassInit) declares them — not because of a build flag.
    /// </summary>
    [TestMethod]
    public void Test3_Hub_Builds_Exactly_The_Declared_Indicators()
    {
        var hub = new IntervalIndicatorHub();
        var built = hub.BuiltIndicators.ToHashSet();

        var g = GlobalData.Settings.General;

        // Base set — every strategy reads these off CandleData.
        IndicatorKey[] baseSet =
        [
            IndicatorKey.BollingerBands(g.SettingsBb.Length, g.SettingsBb.Deviation),
            IndicatorKey.Sma(50), IndicatorKey.Sma(100), IndicatorKey.Sma(200),
            IndicatorKey.Rsi(g.SettingsRsi.Length),
            IndicatorKey.Macd(12, 26, 9),
            IndicatorKey.Stoch(g.SettingsStoch.Length, g.SettingsStoch.SmoothingD, g.SettingsStoch.SmoothingK),
            IndicatorKey.ParabolicSar(0.02, 0.2),
        ];
        foreach (IndicatorKey key in baseSet)
            Assert.IsTrue(built.Contains(key), $"base indicator {key} must always be built");

        // Declared by every registered plugin.
        foreach (var plugin in PluginManager.LoadedPlugins.Values.Distinct())
        {
            foreach (IndicatorKey key in plugin.RequiredIndicators)
                Assert.IsTrue(built.Contains(key),
                    $"{plugin.StrategyName} declares {key} but the hub did not build it");
        }

        // Nothing else: anything extra means something is computed that nobody asked for.
        // Plugin extensions legitimately add to the set too — VBS asks the registry for its
        // Atr(Length), which is exactly the sharing the registry exists for — so replay the
        // extensions onto a scratch registry to learn what they contribute.
        var scratch = new IndicatorRegistry(300);
        foreach (var plugin in PluginManager.LoadedPlugins.Values.Distinct())
        {
            bool anyEnabled = plugin.Strategies.Any(st =>
                GlobalData.Settings.Signal.Long.Strategy.Contains(st.Name) ||
                GlobalData.Settings.Signal.Short.Strategy.Contains(st.Name));
            if (anyEnabled)
                plugin.CreateIndicatorExtension()?.Init(scratch);
        }

        var expected = baseSet
            .Concat(PluginManager.LoadedPlugins.Values.Distinct().SelectMany(p => p.RequiredIndicators))
            .Concat(scratch.Keys)
            .ToHashSet();
        var unexpected = built.Except(expected).ToList();
        Assert.AreEqual(0, unexpected.Count,
            "hub built indicators nobody declared: " + string.Join(", ", unexpected));

        Console.WriteLine($"Hub built {built.Count} indicators: {string.Join(", ", built.OrderBy(k => k.ToString()))}");
    }
}
