using CryptoScanner.Analyzers.Dbr;
using CryptoScanner.Analyzers.Dbr.Signal;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.CoreTests.Analyzer.Dbr;

/// <summary>
/// Unit tests for <see cref="DbrBandsHelper"/>: Donchian-based outer bands plus an EMA+ATR
/// middle cloud (DIDO), with optional HMA-trend / RSI / Stochastic-RSI filters.
/// Tests use deterministic synthetic candles so they need no external data files.
/// </summary>
[TestClass]
public class DbrBandsTests
{
    private const int WarmupCandles = 80;
    private const byte TickDec = 4;

    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        GlobalData.Settings ??= new SettingsBasic();
    }

    private static List<CryptoCandle> MakeCandles(int count, double basePrice = 100.0, double amplitude = 10.0)
    {
        var list = new List<CryptoCandle>(count);
        decimal prevClose = (decimal)basePrice;
        for (int i = 0; i < count; i++)
        {
            double mid = basePrice + amplitude * Math.Sin(i * 0.08) + 3 * Math.Cos(i * 0.17);
            decimal close = Math.Round((decimal)mid, TickDec);
            decimal high = close + 0.50m + (i % 5) * 0.05m;
            decimal low = close - 0.50m - (i % 3) * 0.05m;
            list.Add(new CryptoCandle
            {
                TickDecimals = TickDec,
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

    // ── ComputeBands basic behavior ──────────────────────────────────────

    [TestMethod]
    public void ComputeBands_EmptyCandles_ReturnsEmpty()
    {
        var result = DbrBandsHelper.ComputeBands([]);
        Assert.AreEqual(0, result.Length);
    }

    [TestMethod]
    public void ComputeBands_HasBands_AfterBandLength()
    {
        var candles = MakeCandles(WarmupCandles);
        var bands = DbrBandsHelper.ComputeBands(candles);

        int bandLength = DbrPlugin.Settings.BandLength;

        Assert.AreEqual(candles.Count, bands.Length);

        for (int i = 0; i < bandLength; i++)
            Assert.IsFalse(bands[i].HasBands, $"Index {i} should not have bands (warm-up)");

        Assert.IsTrue(bands[bandLength].HasBands, $"Index {bandLength} should have bands");
    }

    [TestMethod]
    public void ComputeBands_UpperAboveMiddleAboveLower()
    {
        var candles = MakeCandles(WarmupCandles);
        var bands = DbrBandsHelper.ComputeBands(candles);

        foreach (var band in bands.Where(b => b.HasBands))
        {
            Assert.IsTrue(band.Upper > band.Middle, "Upper must be above Middle");
            Assert.IsTrue(band.Middle > band.Lower, "Middle must be above Lower");
        }
    }

    [TestMethod]
    public void ComputeBands_BandWidthPct_IsPositive()
    {
        var candles = MakeCandles(WarmupCandles);
        var bands = DbrBandsHelper.ComputeBands(candles);

        foreach (var band in bands.Where(b => b.HasBands))
            Assert.IsTrue(band.BandWidthPct > 0, "BandWidthPct must be positive");
    }

    // ── Stochastic-RSI filter ────────────────────────────────────────────

    [TestMethod]
    public void ComputeBands_StochDisabled_StochKIsNull()
    {
        DbrPlugin.Settings.RequireStochOsOb = false;
        var candles = MakeCandles(WarmupCandles);
        var bands = DbrBandsHelper.ComputeBands(candles);

        Assert.IsNull(bands[^1].StochK, "StochK must be null when stoch filter is disabled");
        Assert.IsNull(bands[^1].StochD, "StochD must be null when stoch filter is disabled");
    }

    [TestMethod]
    public void ComputeBands_StochEnabled_StochKPopulated()
    {
        DbrPlugin.Settings.RequireStochOsOb = true;
        try
        {
            var candles = MakeCandles(WarmupCandles);
            var bands = DbrBandsHelper.ComputeBands(candles);

            Assert.IsNotNull(bands[^1].StochK, "StochK must be populated when stoch filter is enabled");
            Assert.IsNotNull(bands[^1].StochD, "StochD must be populated when stoch filter is enabled");
            Assert.IsTrue(bands[^1].StochK >= 0 && bands[^1].StochK <= 100,
                $"StochK must be in [0,100], got {bands[^1].StochK}");
        }
        finally
        {
            DbrPlugin.Settings.RequireStochOsOb = false;
        }
    }

    // ── IsLongBreak / IsShortBreak ───────────────────────────────────────

    [TestMethod]
    public void IsLongBreak_NoBreak_WhenPriceAboveLowerBand()
    {
        var candles = MakeCandles(WarmupCandles);
        var bands = DbrBandsHelper.ComputeBands(candles);

        int idx = candles.Count - 1;
        if (!bands[idx].HasBands)
        {
            Assert.Inconclusive("Last candle has no bands — increase WarmupCandles");
            return;
        }

        // Force the candle's Low well above the lower band
        candles[idx] = candles[idx] with { Low = (decimal)(bands[idx].Upper + 10) };

        bool result = DbrBandsHelper.IsLongBreak(candles, bands, idx,
            out double bw, out double bp, out string reason);

        Assert.IsFalse(result, $"Expected no long break, got reason: {reason}");
        Assert.AreEqual("no lower band break", reason);
    }

    [TestMethod]
    public void IsShortBreak_NoBreak_WhenPriceBelowUpperBand()
    {
        var candles = MakeCandles(WarmupCandles);
        var bands = DbrBandsHelper.ComputeBands(candles);

        int idx = candles.Count - 1;
        if (!bands[idx].HasBands)
        {
            Assert.Inconclusive("Last candle has no bands — increase WarmupCandles");
            return;
        }

        // Force the candle's High well below the upper band
        candles[idx] = candles[idx] with { High = (decimal)(bands[idx].Lower - 10) };

        bool result = DbrBandsHelper.IsShortBreak(candles, bands, idx,
            out double bw, out double bp, out string reason);

        Assert.IsFalse(result, $"Expected no short break, got reason: {reason}");
        Assert.AreEqual("no upper band break", reason);
    }

    [TestMethod]
    public void IsLongBreak_WarmingUp_ReturnsFalse()
    {
        var candles = MakeCandles(WarmupCandles);
        var bands = DbrBandsHelper.ComputeBands(candles);

        bool result = DbrBandsHelper.IsLongBreak(candles, bands, 0,
            out _, out _, out string reason);

        Assert.IsFalse(result);
        Assert.AreEqual("bands warming up", reason);
    }

    [TestMethod]
    public void IsShortBreak_WarmingUp_ReturnsFalse()
    {
        var candles = MakeCandles(WarmupCandles);
        var bands = DbrBandsHelper.ComputeBands(candles);

        bool result = DbrBandsHelper.IsShortBreak(candles, bands, 0,
            out _, out _, out string reason);

        Assert.IsFalse(result);
        Assert.AreEqual("bands warming up", reason);
    }

    [TestMethod]
    public void IsLongBreak_TriggersOnDeepLow()
    {
        DbrPlugin.Settings.AllowStack = true;

        var candles = MakeCandles(WarmupCandles);
        var bands = DbrBandsHelper.ComputeBands(candles);

        int idx = candles.Count - 1;
        if (!bands[idx].HasBands)
        {
            Assert.Inconclusive("Last candle has no bands");
            return;
        }

        // Force a very deep Low that breaks the lower band
        decimal deepLow = (decimal)(bands[idx].Lower - 5);
        candles[idx] = candles[idx] with { Low = deepLow };

        // Ensure the previous candle did NOT break its lower band
        if (idx > 0 && bands[idx - 1].HasBands)
            candles[idx - 1] = candles[idx - 1] with { Low = (decimal)(bands[idx - 1].Lower + 1) };

        bool result = DbrBandsHelper.IsLongBreak(candles, bands, idx,
            out double bw, out double bp, out string reason);

        Assert.IsTrue(result, $"Expected long break but got: {reason}");
        Assert.IsTrue(bw > 0, "BandWidthPct should be positive");
    }

    [TestMethod]
    public void IsShortBreak_TriggersOnHighBreak()
    {
        DbrPlugin.Settings.AllowStack = true;

        var candles = MakeCandles(WarmupCandles);
        var bands = DbrBandsHelper.ComputeBands(candles);

        int idx = candles.Count - 1;
        if (!bands[idx].HasBands)
        {
            Assert.Inconclusive("Last candle has no bands");
            return;
        }

        // Force a very high High that breaks the upper band
        decimal highBreak = (decimal)(bands[idx].Upper + 5);
        candles[idx] = candles[idx] with { High = highBreak };

        // Ensure the previous candle did NOT break its upper band
        if (idx > 0 && bands[idx - 1].HasBands)
            candles[idx - 1] = candles[idx - 1] with { High = (decimal)(bands[idx - 1].Upper - 1) };

        bool result = DbrBandsHelper.IsShortBreak(candles, bands, idx,
            out double bw, out double bp, out string reason);

        Assert.IsTrue(result, $"Expected short break but got: {reason}");
        Assert.IsTrue(bw > 0, "BandWidthPct should be positive");
    }

    // ── Stacking rule ────────────────────────────────────────────────────

    [TestMethod]
    public void IsLongBreak_StackingBlocked_WhenPreviousAlsoBroke()
    {
        DbrPlugin.Settings.AllowStack = false;

        var candles = MakeCandles(WarmupCandles);
        var bands = DbrBandsHelper.ComputeBands(candles);

        int idx = candles.Count - 1;
        if (!bands[idx].HasBands || idx < 1 || !bands[idx - 1].HasBands)
        {
            Assert.Inconclusive("Need at least 2 candles with bands");
            return;
        }

        // Force both candles to break below their respective lower bands
        candles[idx - 1] = candles[idx - 1] with { Low = (decimal)(bands[idx - 1].Lower - 2) };
        candles[idx] = candles[idx] with { Low = (decimal)(bands[idx].Lower - 1) };

        bool result = DbrBandsHelper.IsLongBreak(candles, bands, idx,
            out _, out _, out string reason);

        Assert.IsFalse(result, $"Expected stacking to be blocked, but got signal. Reason: {reason}");
        Assert.AreEqual("already broken on previous candle", reason);

        DbrPlugin.Settings.AllowStack = true;
    }

    // ── Donchian band values are correct ─────────────────────────────────

    [TestMethod]
    public void ComputeBands_Donchian_MiddleIsAverageOfHighLow()
    {
        var candles = MakeCandles(WarmupCandles);
        var bands = DbrBandsHelper.ComputeBands(candles);
        int bandLength = DbrPlugin.Settings.BandLength;

        for (int i = bandLength; i < candles.Count; i++)
        {
            double highestHigh = double.MinValue;
            double lowestLow = double.MaxValue;
            for (int j = i - bandLength; j < i; j++)
            {
                if ((double)candles[j].High > highestHigh) highestHigh = (double)candles[j].High;
                if ((double)candles[j].Low < lowestLow) lowestLow = (double)candles[j].Low;
            }
            double expectedMiddle = (highestHigh + lowestLow) / 2;
            Assert.AreEqual(expectedMiddle, bands[i].Middle, 1e-10,
                $"Middle at index {i} does not match Donchian midpoint");
        }
    }
}
