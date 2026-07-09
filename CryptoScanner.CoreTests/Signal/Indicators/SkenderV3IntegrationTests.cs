using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal.Indicators;

using Skender.Stock.Indicators;

namespace CryptoScanner.CoreTests.Signal.Indicators;

/// <summary>
/// Validates correct integration with Skender.Stock.Indicators v3 (QuoteHub).
///
/// Test coverage:
/// 1. IQuote mapping — CryptoCandle tick-based storage round-trips correctly to Skender Quote OHLCV.
/// 2. Nullable result handling — v3 returns nullable doubles; verify null during warmup, non-null after.
/// 3. Warmup periods — each indicator family produces null for the expected number of initial candles.
/// </summary>
[TestClass]
public class SkenderV3IntegrationTests
{
    // ── 1. IQuote Mapping ─────────────────────────────────────────────────

    [TestMethod]
    public void CryptoCandle_IQuote_Properties_Match_Assigned_Values()
    {
        var candle = new CryptoCandle
        {
            TickDecimals = 4,
            OpenTime = new CandleTime(1000),
            Open = 1.2345m,
            High = 1.5678m,
            Low = 1.0001m,
            Close = 1.4000m,
            Volume = 9999.5m,
        };

        IQuote quote = candle;
        Assert.AreEqual(1.2345m, quote.Open, "Open must survive tick round-trip");
        Assert.AreEqual(1.5678m, quote.High, "High must survive tick round-trip");
        Assert.AreEqual(1.0001m, quote.Low, "Low must survive tick round-trip");
        Assert.AreEqual(1.4000m, quote.Close, "Close must survive tick round-trip");
        Assert.AreEqual(9999.5m, quote.Volume, "Volume must survive double round-trip");
        Assert.AreEqual(candle.Date, quote.Timestamp, "Timestamp must equal Date");
    }

    [TestMethod]
    public void CryptoCandle_TickStorage_Preserves_Precision_Per_TickDecimals()
    {
        // Prices with different decimal depths — each must round-trip through tick storage.
        var testCases = new (byte tickDecimals, decimal price)[]
        {
            (0, 42m),
            (1, 3.5m),
            (2, 99.99m),
            (4, 0.0001m),
            (6, 0.000001m),
            (8, 0.00000001m),
        };

        foreach (var (tickDecimals, price) in testCases)
        {
            var candle = new CryptoCandle { TickDecimals = tickDecimals, Close = price };
            Assert.AreEqual(price, candle.Close,
                $"TickDecimals={tickDecimals}, price={price}: tick round-trip failed");
        }
    }

    [TestMethod]
    public void CryptoCandle_ZeroVolume_Does_Not_Break_IQuote()
    {
        var candle = new CryptoCandle
        {
            TickDecimals = 2,
            OpenTime = new CandleTime(500),
            Open = 100m,
            High = 105m,
            Low = 95m,
            Close = 102m,
            Volume = 0m,
        };

        IQuote quote = candle;
        Assert.AreEqual(0m, quote.Volume);
    }

    [TestMethod]
    public void AsQuotes_Boxing_Preserves_All_Fields()
    {
        var candles = new List<CryptoCandle>
        {
            new()
            {
                TickDecimals = 4,
                OpenTime = new CandleTime(100),
                Open = 50.1234m, High = 51.5678m, Low = 49.0001m, Close = 50.5000m,
                Volume = 12345.67m,
            },
            new()
            {
                TickDecimals = 4,
                OpenTime = new CandleTime(101),
                Open = 50.5000m, High = 52.0000m, Low = 50.0000m, Close = 51.2345m,
                Volume = 0m,
            },
        };

        IReadOnlyList<IQuote> quotes = candles.AsQuotes();

        Assert.AreEqual(candles.Count, quotes.Count);
        for (int i = 0; i < candles.Count; i++)
        {
            Assert.AreEqual(candles[i].Open, quotes[i].Open, $"[{i}] Open mismatch after boxing");
            Assert.AreEqual(candles[i].High, quotes[i].High, $"[{i}] High mismatch after boxing");
            Assert.AreEqual(candles[i].Low, quotes[i].Low, $"[{i}] Low mismatch after boxing");
            Assert.AreEqual(candles[i].Close, quotes[i].Close, $"[{i}] Close mismatch after boxing");
            Assert.AreEqual(candles[i].Volume, quotes[i].Volume, $"[{i}] Volume mismatch after boxing");
            Assert.AreEqual(candles[i].Timestamp, quotes[i].Timestamp, $"[{i}] Timestamp mismatch after boxing");
        }
    }

    // ── 2. Nullable result handling ───────────────────────────────────────

    [TestMethod]
    public void Hub_BuildCurrent_Returns_Null_Fields_During_Warmup()
    {
        GlobalData.Settings = new SettingsBasic();
        var hub = new IntervalIndicatorHub();

        // Feed just 1 candle — every indicator with period > 1 should still be null.
        hub.Add(MakeQuote(0, 100));
        CryptoData data = hub.BuildCurrent();

        Assert.IsNull(data.Sma50, "SMA(50) must be null after 1 candle");
        Assert.IsNull(data.Sma100, "SMA(100) must be null after 1 candle");
        Assert.IsNull(data.Sma200, "SMA(200) must be null after 1 candle");
    }

    [TestMethod]
    public void Hub_BuildCurrent_Returns_NonNull_Fields_After_Sufficient_Candles()
    {
        GlobalData.Settings = new SettingsBasic();
        var hub = new IntervalIndicatorHub();

        // Feed 250 candles — SMA(50/100/200), RSI, MACD, Stoch should all have values.
        // SMA(200) needs exactly 200 candles, RSI(14) needs ~15, MACD(12,26,9) needs ~34.
        for (int i = 0; i < 250; i++)
            hub.Add(MakeQuote(i, 100 + Math.Sin(i * 0.1) * 10));

        CryptoData data = hub.BuildCurrent();

        Assert.IsNotNull(data.Sma20, "SMA(20) must have a value after 250 candles");
        Assert.IsNotNull(data.Sma50, "SMA(50) must have a value after 250 candles");
        Assert.IsNotNull(data.Sma100, "SMA(100) must have a value after 250 candles");
        Assert.IsNotNull(data.Sma200, "SMA(200) must have a value after 250 candles");
        Assert.IsNotNull(data.Rsi, "RSI must have a value after 250 candles");
        Assert.IsNotNull(data.MacdValue, "MACD value must have a value after 250 candles");
        Assert.IsNotNull(data.MacdSignal, "MACD signal must have a value after 250 candles");
        Assert.IsNotNull(data.MacdHistogram, "MACD histogram must have a value after 250 candles");
        Assert.IsNotNull(data.StochOscillator, "Stoch oscillator must have a value after 250 candles");
        Assert.IsNotNull(data.StochSignal, "Stoch signal must have a value after 250 candles");
        Assert.IsNotNull(data.PSar, "Parabolic SAR must have a value after 250 candles");
        Assert.IsNotNull(data.BollingerBandsDeviation, "BB deviation must have a value after 250 candles");
        Assert.IsNotNull(data.BollingerBandsPercentage, "BB percentage must have a value after 250 candles");
    }

    [TestMethod]
    public void Hub_Nullable_Values_Are_Not_Silently_Zero()
    {
        GlobalData.Settings = new SettingsBasic();
        var hub = new IntervalIndicatorHub();

        // After enough candles with varying prices, indicators should not return exactly 0.0
        // (a common v3 migration bug: reading .Value on null → 0 instead of propagating null).
        for (int i = 0; i < 250; i++)
            hub.Add(MakeQuote(i, 80 + 40 * Math.Sin(i * 0.08)));

        CryptoData data = hub.BuildCurrent();

        Assert.IsTrue(data.Sma50 > 0, "SMA(50) should be a positive price, not 0");
        Assert.IsTrue(data.Rsi > 0, "RSI should be > 0 for a varying series");
        Assert.IsTrue(data.BollingerBandsDeviation > 0, "BB deviation should be > 0 for a varying series");
    }

    // ── 3. Warmup periods ─────────────────────────────────────────────────

    [TestMethod]
    public void Sma_Produces_Null_Before_Period_And_Value_At_Period()
    {
        GlobalData.Settings = new SettingsBasic();
        IReadOnlyList<IQuote> quotes = MakeQuotes(55, 100);

        var sma50 = quotes.ToSma(50).ToList();

        // SMA(50) must be null for the first 49 candles (indices 0..48).
        for (int i = 0; i < 49; i++)
            Assert.IsNull(sma50[i].Sma, $"SMA(50) at index {i} should be null (warmup)");

        // At index 49 (the 50th candle) it must have a value.
        Assert.IsNotNull(sma50[49].Sma, "SMA(50) at index 49 should have a value");
    }

    [TestMethod]
    public void Rsi_Produces_Null_During_Warmup()
    {
        GlobalData.Settings = new SettingsBasic();
        int rsiLength = GlobalData.Settings.General.SettingsRsi.Length; // typically 14
        IReadOnlyList<IQuote> quotes = MakeQuotes(rsiLength + 10, 100);

        var rsi = quotes.ToRsi(rsiLength).ToList();

        // RSI needs rsiLength candles before producing a value.
        for (int i = 0; i < rsiLength; i++)
            Assert.IsNull(rsi[i].Rsi, $"RSI({rsiLength}) at index {i} should be null (warmup)");

        Assert.IsNotNull(rsi[rsiLength].Rsi, $"RSI({rsiLength}) at index {rsiLength} should have a value");
    }

    [TestMethod]
    public void BollingerBands_Produces_Null_During_Warmup()
    {
        GlobalData.Settings = new SettingsBasic();
        int bbLength = GlobalData.Settings.General.SettingsBb.Length; // typically 20
        IReadOnlyList<IQuote> quotes = MakeQuotes(bbLength + 5, 100);

        var bb = quotes.ToBollingerBands(bbLength, GlobalData.Settings.General.SettingsBb.Deviation).ToList();

        for (int i = 0; i < bbLength - 1; i++)
            Assert.IsNull(bb[i].Sma, $"BB SMA at index {i} should be null (warmup)");

        Assert.IsNotNull(bb[bbLength - 1].Sma, $"BB SMA at index {bbLength - 1} should have a value");
        Assert.IsNotNull(bb[bbLength - 1].UpperBand, "BB upper band should have a value at end of warmup");
        Assert.IsNotNull(bb[bbLength - 1].LowerBand, "BB lower band should have a value at end of warmup");
    }

    [TestMethod]
    public void Macd_Produces_Null_During_Warmup()
    {
        GlobalData.Settings = new SettingsBasic();
        // MACD(12,26,9): slow EMA needs 26 candles, signal needs 9 more → 34 for full signal.
        IReadOnlyList<IQuote> quotes = MakeQuotes(40, 100);

        var macd = quotes.ToMacd(12, 26, 9).ToList();

        // MACD line appears after 26 candles (index 25).
        for (int i = 0; i < 25; i++)
            Assert.IsNull(macd[i].Macd, $"MACD line at index {i} should be null (warmup)");

        Assert.IsNotNull(macd[25].Macd, "MACD line at index 25 should have a value");

        // Signal line appears after 26 + 9 - 1 = 34 candles (index 33).
        for (int i = 25; i < 33; i++)
            Assert.IsNull(macd[i].Signal, $"MACD signal at index {i} should be null (signal warmup)");

        Assert.IsNotNull(macd[33].Signal, "MACD signal at index 33 should have a value");
    }

    [TestMethod]
    public void Stoch_Produces_Null_During_Warmup()
    {
        GlobalData.Settings = new SettingsBasic();
        var s = GlobalData.Settings.General.SettingsStoch;
        // Stoch(14,3,3): needs lookback + smoothing periods.
        IReadOnlyList<IQuote> quotes = MakeQuotes(25, 100);

        var stoch = quotes.ToStoch(s.Length, s.SmoothingD, s.SmoothingK).ToList();

        // Oscillator (%K) needs Length - 1 + SmoothingK - 1 candles.
        int kWarmup = s.Length + s.SmoothingK - 2;
        for (int i = 0; i < kWarmup; i++)
            Assert.IsNull(stoch[i].Oscillator, $"Stoch %K at index {i} should be null (warmup)");

        Assert.IsNotNull(stoch[kWarmup].Oscillator, $"Stoch %K at index {kWarmup} should have a value");
    }

    [TestMethod]
    public void Hub_Warmup_Transition_From_Null_To_Value()
    {
        GlobalData.Settings = new SettingsBasic();
        var hub = new IntervalIndicatorHub();

        // Track when SMA(200) transitions from null to non-null.
        int? sma200FirstNonNull = null;
        for (int i = 0; i < 250; i++)
        {
            hub.Add(MakeQuote(i, 100 + 5 * Math.Sin(i * 0.1)));
            CryptoData data = hub.BuildCurrent();

            if (sma200FirstNonNull == null && data.Sma200 != null)
                sma200FirstNonNull = i;
        }

        Assert.IsNotNull(sma200FirstNonNull, "SMA(200) should eventually produce a value");
        Assert.AreEqual(199, sma200FirstNonNull.Value,
            "SMA(200) should first produce a value at the 200th candle (index 199)");
    }

    // ── Known-value sanity check ──────────────────────────────────────────

    [TestMethod]
    public void Sma_Known_Values_Match_Manual_Calculation()
    {
        // 5 candles with known closes: SMA(3) at index 2 = avg(10, 20, 30) = 20.
        var quotes = new List<Quote>
        {
            new(DateTime.UtcNow.AddMinutes(-4), 10, 10, 10, 10, 100),
            new(DateTime.UtcNow.AddMinutes(-3), 20, 20, 20, 20, 100),
            new(DateTime.UtcNow.AddMinutes(-2), 30, 30, 30, 30, 100),
            new(DateTime.UtcNow.AddMinutes(-1), 40, 40, 40, 40, 100),
            new(DateTime.UtcNow, 50, 50, 50, 50, 100),
        };

        var sma = quotes.Cast<IQuote>().ToList().ToSma(3).ToList();

        Assert.IsNull(sma[0].Sma);
        Assert.IsNull(sma[1].Sma);
        Assert.AreEqual(20.0, sma[2].Sma!.Value, 1e-10, "SMA(3) at index 2 = avg(10,20,30)");
        Assert.AreEqual(30.0, sma[3].Sma!.Value, 1e-10, "SMA(3) at index 3 = avg(20,30,40)");
        Assert.AreEqual(40.0, sma[4].Sma!.Value, 1e-10, "SMA(3) at index 4 = avg(30,40,50)");
    }

    [TestMethod]
    public void BollingerBands_Upper_Greater_Than_Lower()
    {
        GlobalData.Settings = new SettingsBasic();
        IReadOnlyList<IQuote> quotes = MakeQuotes(30, 100);

        var bb = quotes.ToBollingerBands(20, 2).ToList();

        // After warmup, upper band must always be >= lower band.
        for (int i = 19; i < bb.Count; i++)
        {
            Assert.IsNotNull(bb[i].UpperBand, $"UpperBand at {i} should not be null");
            Assert.IsNotNull(bb[i].LowerBand, $"LowerBand at {i} should not be null");
            Assert.IsTrue(bb[i].UpperBand >= bb[i].LowerBand,
                $"Upper ({bb[i].UpperBand}) must be >= Lower ({bb[i].LowerBand}) at index {i}");
        }
    }

    [TestMethod]
    public void Rsi_Stays_Within_0_100_Range()
    {
        GlobalData.Settings = new SettingsBasic();
        IReadOnlyList<IQuote> quotes = MakeQuotes(200, 100);

        var rsi = quotes.ToRsi(14).ToList();

        for (int i = 0; i < rsi.Count; i++)
        {
            if (rsi[i].Rsi == null) continue;
            Assert.IsTrue(rsi[i].Rsi >= 0 && rsi[i].Rsi <= 100,
                $"RSI at index {i} = {rsi[i].Rsi} is outside [0, 100]");
        }
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    private static Quote MakeQuote(int index, double mid)
    {
        DateTime ts = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddMinutes(index);
        decimal close = Math.Round((decimal)mid, 2);
        decimal high = close + 0.50m + (index % 5) * 0.05m;
        decimal low = close - 0.50m - (index % 3) * 0.05m;
        decimal open = close - 0.10m + (index % 4) * 0.05m;
        return new Quote(ts, open, high, low, close, 1000m + (index % 13) * 50m);
    }

    private static IReadOnlyList<IQuote> MakeQuotes(int count, double baseMid)
    {
        var list = new List<IQuote>(count);
        for (int i = 0; i < count; i++)
        {
            double mid = baseMid + 10 * Math.Sin(i * 0.10) + 3 * Math.Sin(i * 0.37);
            list.Add(MakeQuote(i, mid));
        }
        return list;
    }


    // ── 5. QuoteHub maxCacheSize experiments ──────────────────────────────

    [TestMethod]
    public void QuoteHub_MaxCacheSize1_Rejected_By_Skender()
    {
        // Skender validates that maxCacheSize >= longest lookback period.
        // maxCacheSize=1 is too small for any useful indicator.
        var hub = new QuoteHub(maxCacheSize: 1);
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => hub.ToSmaHub(20),
            "Skender should reject cache smaller than the indicator's lookback");
    }

    [TestMethod]
    [DataRow(201, DisplayName = "Minimum (SMA200 + 1)")]
    [DataRow(300, DisplayName = "Small buffer (300)")]
    [DataRow(500, DisplayName = "Medium buffer (500)")]
    public void QuoteHub_SmallCache_AllIndicators_Match_Unlimited(int cacheSize)
    {
        // SMA(200) is our longest indicator → minimum cache = 200.
        // Compare a bounded hub against an unlimited hub after 2500 candles.
        const int candleCount = 2500;
        var quotes = MakeQuotes(candleCount, 100);

        var hubU = new QuoteHub();
        var bbU = hubU.ToBollingerBandsHub(20, 2.0);
        var sma50U = hubU.ToSmaHub(50);
        var sma200U = hubU.ToSmaHub(200);
        var rsiU = hubU.ToRsiHub(14);
        var macdU = hubU.ToMacdHub(12, 26, 9);
        var stochU = hubU.ToStochHub(14, 3, 3);
        var psarU = hubU.ToParabolicSarHub(0.02, 0.2);
        var atrU = hubU.ToAtrHub(14);

        var hubT = new QuoteHub(maxCacheSize: cacheSize);
        var bbT = hubT.ToBollingerBandsHub(20, 2.0);
        var sma50T = hubT.ToSmaHub(50);
        var sma200T = hubT.ToSmaHub(200);
        var rsiT = hubT.ToRsiHub(14);
        var macdT = hubT.ToMacdHub(12, 26, 9);
        var stochT = hubT.ToStochHub(14, 3, 3);
        var psarT = hubT.ToParabolicSarHub(0.02, 0.2);
        var atrT = hubT.ToAtrHub(14);

        for (int i = 0; i < candleCount; i++)
        {
            hubU.Add(quotes[i]);
            hubT.Add(quotes[i]);
        }

        // Compare last values
        AssertLastEqual(sma50U.Results, sma50T.Results, r => r.Sma, "SMA(50)");
        AssertLastEqual(sma200U.Results, sma200T.Results, r => r.Sma, "SMA(200)");
        AssertLastEqual(rsiU.Results, rsiT.Results, r => r.Rsi, "RSI");
        AssertLastEqual(atrU.Results, atrT.Results, r => r.Atr, "ATR");

        // BB
        Assert.AreEqual(bbU.Results[^1].Sma!.Value, bbT.Results[^1].Sma!.Value, 1e-10, "BB SMA mismatch");
        Assert.AreEqual(bbU.Results[^1].UpperBand!.Value, bbT.Results[^1].UpperBand!.Value, 1e-10, "BB Upper mismatch");
        Assert.AreEqual(bbU.Results[^1].LowerBand!.Value, bbT.Results[^1].LowerBand!.Value, 1e-10, "BB Lower mismatch");

        // MACD
        Assert.AreEqual(macdU.Results[^1].Macd!.Value, macdT.Results[^1].Macd!.Value, 1e-10, "MACD value mismatch");
        Assert.AreEqual(macdU.Results[^1].Signal!.Value, macdT.Results[^1].Signal!.Value, 1e-10, "MACD signal mismatch");

        // Stoch
        Assert.AreEqual(stochU.Results[^1].Oscillator!.Value, stochT.Results[^1].Oscillator!.Value, 1e-10, "Stoch %K mismatch");
        Assert.AreEqual(stochU.Results[^1].Signal!.Value, stochT.Results[^1].Signal!.Value, 1e-10, "Stoch %D mismatch");

        // PSar
        Assert.AreEqual(psarU.Results[^1].Sar!.Value, psarT.Results[^1].Sar!.Value, 1e-10, "PSar mismatch");
    }

    [TestMethod]
    public void QuoteHub_SmallCache_NoPerformanceDegradation_At_100k()
    {
        // Feed 100k candles with bounded cache (300). Should complete fast if
        // the pruning at a small cache size doesn't trigger O(n²) behavior.
        const int candleCount = 100_000;
        const int cacheSize = 300;
        var hub = new QuoteHub(maxCacheSize: cacheSize);
        var sma = hub.ToSmaHub(200);
        var rsi = hub.ToRsiHub(14);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < candleCount; i++)
            hub.Add(MakeQuote(i, 100 + Math.Sin(i * 0.01) * 20));
        sw.Stop();

        Assert.IsNotNull(rsi.Results[^1].Rsi, "RSI should have a value");
        Assert.IsNotNull(sma.Results[^1].Sma, "SMA should have a value");

        Console.WriteLine($"100k candles with maxCacheSize={cacheSize}: {sw.ElapsedMilliseconds}ms");
        Assert.IsTrue(sw.ElapsedMilliseconds < 10_000,
            $"100k candles took {sw.ElapsedMilliseconds}ms — possible O(n²) pruning");
    }

    [TestMethod]
    public void QuoteHub_Unlimited_Performance_At_100k()
    {
        // Baseline: 100k candles with unlimited cache. Compare timing.
        const int candleCount = 100_000;
        var hub = new QuoteHub();
        var sma = hub.ToSmaHub(200);
        var rsi = hub.ToRsiHub(14);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        for (int i = 0; i < candleCount; i++)
            hub.Add(MakeQuote(i, 100 + Math.Sin(i * 0.01) * 20));
        sw.Stop();

        Assert.IsNotNull(rsi.Results[^1].Rsi, "RSI should have a value");
        Assert.IsNotNull(sma.Results[^1].Sma, "SMA should have a value");

        Console.WriteLine($"100k candles UNLIMITED cache: {sw.ElapsedMilliseconds}ms");
    }

    private static void AssertLastEqual<T>(IReadOnlyList<T> a, IReadOnlyList<T> b, Func<T, double?> selector, string label)
    {
        double? va = a.Count > 0 ? selector(a[^1]) : null;
        double? vb = b.Count > 0 ? selector(b[^1]) : null;
        Assert.IsNotNull(va, $"{label} unlimited should have a value");
        Assert.IsNotNull(vb, $"{label} tiny-cache should have a value");
        Assert.AreEqual(va!.Value, vb!.Value, 1e-10, $"{label} mismatch between unlimited and maxCacheSize=1");
    }
}
