using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Helpers;

namespace CryptoScanner.CoreTests.Analyzer.Helpers;

/// <summary>
/// Tests that helper extension methods on MyData handle null indicator
/// values gracefully — the warmup period produces nulls for all indicators
/// and the helpers must not throw NullReferenceException.
/// </summary>
[TestClass]
public class HelperExtensionNullTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        GlobalData.Settings ??= new SettingsBasic();
    }

    private static CryptoCandle MakeCandle(decimal price)
    {
        return new CryptoCandle
        {
            TickDecimals = 4,
            Open = price,
            High = price + 1m,
            Low = price - 1m,
            Close = price,
            Volume = 100m,
        };
    }

    private static MyData MakeData(CryptoData? candleData, decimal price = 100m)
    {
        return new MyData
        {
            Candle = MakeCandle(price),
            CandleData = candleData ?? new CryptoData(),
        };
    }

    // ── RSI helpers ──────────────────────────────────────────────────

    [TestMethod]
    public void RsiOversold_NullRsi_ReturnsTrue()
    {
        // CandleData?.Rsi is null → null > 30 is false → method returns true.
        // This documents existing behavior: null is treated as oversold.
        var data = MakeData(new CryptoData { Rsi = null });
        Assert.IsTrue(data.RsiOversold());
    }

    [TestMethod]
    public void RsiOverbought_NullRsi_ReturnsTrue()
    {
        // Same pattern: null < 70 is false → returns true.
        var data = MakeData(new CryptoData { Rsi = null });
        Assert.IsTrue(data.RsiOverbought());
    }

    [TestMethod]
    public void RsiOversold_WithValue_BelowThreshold_ReturnsTrue()
    {
        var data = MakeData(new CryptoData { Rsi = 20 });
        Assert.IsTrue(data.RsiOversold());
    }

    [TestMethod]
    public void RsiOversold_WithValue_AboveThreshold_ReturnsFalse()
    {
        var data = MakeData(new CryptoData { Rsi = 50 });
        Assert.IsFalse(data.RsiOversold());
    }

    [TestMethod]
    public void RsiOverbought_WithValue_AboveThreshold_ReturnsTrue()
    {
        var data = MakeData(new CryptoData { Rsi = 80 });
        Assert.IsTrue(data.RsiOverbought());
    }

    [TestMethod]
    public void RsiOverbought_WithValue_BelowThreshold_ReturnsFalse()
    {
        var data = MakeData(new CryptoData { Rsi = 50 });
        Assert.IsFalse(data.RsiOverbought());
    }

    [TestMethod]
    public void RsiOversold_WithCorrection_ShiftsThreshold()
    {
        // Threshold = 30 - 5 = 25. Rsi=27 is above shifted threshold → not oversold.
        var data = MakeData(new CryptoData { Rsi = 27 });
        Assert.IsFalse(data.RsiOversold(correction: 5));
    }

    // ── Stochastic helpers ───────────────────────────────────────────

    [TestMethod]
    public void StochOversold_NullValues_ReturnsTrue()
    {
        // Both StochSignal and StochOscillator are null.
        // null > 20 → false for both checks → returns true.
        var data = MakeData(new CryptoData { StochSignal = null, StochOscillator = null });
        Assert.IsTrue(data.StochOversold());
    }

    [TestMethod]
    public void StochOverbought_NullValues_ReturnsTrue()
    {
        var data = MakeData(new CryptoData { StochSignal = null, StochOscillator = null });
        Assert.IsTrue(data.StochOverbought());
    }

    [TestMethod]
    public void StochOversold_BothBelowThreshold_ReturnsTrue()
    {
        var data = MakeData(new CryptoData { StochSignal = 15, StochOscillator = 10 });
        Assert.IsTrue(data.StochOversold());
    }

    [TestMethod]
    public void StochOversold_SignalAboveThreshold_ReturnsFalse()
    {
        var data = MakeData(new CryptoData { StochSignal = 50, StochOscillator = 10 });
        Assert.IsFalse(data.StochOversold());
    }

    [TestMethod]
    public void StochOverbought_BothAboveThreshold_ReturnsTrue()
    {
        var data = MakeData(new CryptoData { StochSignal = 85, StochOscillator = 90 });
        Assert.IsTrue(data.StochOverbought());
    }

    [TestMethod]
    public void StochOverbought_OscillatorBelowThreshold_ReturnsFalse()
    {
        var data = MakeData(new CryptoData { StochSignal = 85, StochOscillator = 50 });
        Assert.IsFalse(data.StochOverbought());
    }

    // ── Bollinger Bands helpers ──────────────────────────────────────

    [TestMethod]
    public void CheckBollingerBandsWidth_NullPercentage_DoesNotThrow()
    {
        // BollingerBandsPercentage is null. The method accesses CandleData!.BollingerBandsPercentage.
        // null <= boundary → false, null >= boundary → false → returns true.
        var data = MakeData(new CryptoData { BollingerBandsPercentage = null });
        bool result = data.CheckBollingerBandsWidth(1.0, 10.0);
        Assert.IsTrue(result);
    }

    [TestMethod]
    public void CheckBollingerBandsWidth_WithinRange_ReturnsTrue()
    {
        var data = MakeData(new CryptoData { BollingerBandsPercentage = 5.0 });
        Assert.IsTrue(data.CheckBollingerBandsWidth(1.0, 10.0));
    }

    [TestMethod]
    public void CheckBollingerBandsWidth_BelowMin_ReturnsFalse()
    {
        var data = MakeData(new CryptoData { BollingerBandsPercentage = 0.5 });
        Assert.IsFalse(data.CheckBollingerBandsWidth(1.0, 10.0));
    }

    [TestMethod]
    public void CheckBollingerBandsWidth_AboveMax_ReturnsFalse()
    {
        var data = MakeData(new CryptoData { BollingerBandsPercentage = 15.0 });
        Assert.IsFalse(data.CheckBollingerBandsWidth(1.0, 10.0));
    }

    [TestMethod]
    public void IsBelowBollingerBands_NullSma20_ReturnsFalse()
    {
        // Sma20 is null → band = null - deviation = null. band.HasValue → false → returns false.
        var data = MakeData(new CryptoData { Sma20 = null, BollingerBandsDeviation = 2.0 }, price: 100m);
        Assert.IsFalse(data.IsBelowBollingerBands(useLowHigh: false));
    }

    [TestMethod]
    public void IsBelowBollingerBands_NullDeviation_ReturnsFalse()
    {
        // Deviation is null → band = 100 - null = null. HasValue → false → returns false.
        var data = MakeData(new CryptoData { Sma20 = 100, BollingerBandsDeviation = null }, price: 90m);
        Assert.IsFalse(data.IsBelowBollingerBands(useLowHigh: false));
    }

    [TestMethod]
    public void IsBelowBollingerBands_PriceBelowLowerBand_ReturnsTrue()
    {
        // Lower band = 100 - 5 = 95. Price close = 90 < 95 → true.
        var data = MakeData(new CryptoData { Sma20 = 100, BollingerBandsDeviation = 5.0 }, price: 90m);
        Assert.IsTrue(data.IsBelowBollingerBands(useLowHigh: false));
    }

    [TestMethod]
    public void IsBelowBollingerBands_PriceAboveLowerBand_ReturnsFalse()
    {
        // Lower band = 100 - 5 = 95. Price = 97 > 95 → false.
        var data = MakeData(new CryptoData { Sma20 = 100, BollingerBandsDeviation = 5.0 }, price: 97m);
        Assert.IsFalse(data.IsBelowBollingerBands(useLowHigh: false));
    }

    [TestMethod]
    public void IsAboveBollingerBands_NullSma20_ReturnsFalse()
    {
        var data = MakeData(new CryptoData { Sma20 = null, BollingerBandsDeviation = 2.0 }, price: 200m);
        Assert.IsFalse(data.IsAboveBollingerBands(useLowHigh: false));
    }

    [TestMethod]
    public void IsAboveBollingerBands_PriceAboveUpperBand_ReturnsTrue()
    {
        // Upper band = 100 + 5 = 105. Price = 110 > 105 → true.
        var data = MakeData(new CryptoData { Sma20 = 100, BollingerBandsDeviation = 5.0 }, price: 110m);
        Assert.IsTrue(data.IsAboveBollingerBands(useLowHigh: false));
    }

    // ── SMA / SBM helpers ────────────────────────────────────────────

    [TestMethod]
    public void IsSbmConditionsOversold_NullSmaValues_ReturnsFalse()
    {
        // null > null → false → returns false. Safe behavior.
        var data = MakeData(new CryptoData { Sma200 = null, Sma50 = null, Sma20 = null });
        Assert.IsFalse(data.IsSbmConditionsOversold());
    }

    [TestMethod]
    public void IsSbmConditionsOversold_CorrectOrder_ReturnsTrue()
    {
        // Sma200 > Sma50 > Sma20 → bearish alignment → oversold conditions met.
        var data = MakeData(new CryptoData { Sma200 = 300, Sma50 = 200, Sma20 = 100 });
        Assert.IsTrue(data.IsSbmConditionsOversold());
    }

    [TestMethod]
    public void IsSbmConditionsOverbought_NullSmaValues_ReturnsFalse()
    {
        var data = MakeData(new CryptoData { Sma200 = null, Sma50 = null, Sma20 = null });
        Assert.IsFalse(data.IsSbmConditionsOverbought());
    }

    [TestMethod]
    public void IsSbmConditionsOverbought_CorrectOrder_ReturnsTrue()
    {
        // Sma200 < Sma50 < Sma20 → bullish alignment.
        var data = MakeData(new CryptoData { Sma200 = 100, Sma50 = 200, Sma20 = 300 });
        Assert.IsTrue(data.IsSbmConditionsOverbought());
    }

    [TestMethod]
    public void IsPercentageSma200AndSma50OkayOversold_NullSma_ReturnsTrue()
    {
        // Nullable arithmetic: null - null = null, null / null = null.
        // null < percentage → false → method returns true (claims "okay").
        // Documents existing behavior: null data passes the percentage check.
        var data = MakeData(new CryptoData { Sma200 = null, Sma50 = null });
        bool result = data.IsPercentageSma200AndSma50OkayOversold(1.0m, out string response);
        Assert.IsTrue(result);
        Assert.AreEqual("", response);
    }

    [TestMethod]
    public void IsPercentageSma200AndSma50OkayOversold_SufficientSpread_ReturnsTrue()
    {
        // Sma200=110, Sma50=100. Spread = (110-100)/((110+100)/2) * 100 ≈ 9.52%.
        var data = MakeData(new CryptoData { Sma200 = 110, Sma50 = 100 });
        bool result = data.IsPercentageSma200AndSma50OkayOversold(5.0m, out string response);
        Assert.IsTrue(result);
        Assert.AreEqual("", response);
    }

    [TestMethod]
    public void IsPercentageSma200AndSma50OkayOversold_InsufficientSpread_ReturnsFalse()
    {
        // Sma200=101, Sma50=100. Spread = (101-100)/((101+100)/2)*100 ≈ 0.99%.
        var data = MakeData(new CryptoData { Sma200 = 101, Sma50 = 100 });
        bool result = data.IsPercentageSma200AndSma50OkayOversold(5.0m, out string response);
        Assert.IsFalse(result);
        Assert.IsTrue(response.Contains("sma200 and sma50"));
    }

    [TestMethod]
    public void IsSbmConditionsPSarOversold_NullPSar_ReturnsFalse()
    {
        var data = MakeData(new CryptoData { PSar = null, Sma20 = null }, price: 100m);
        Assert.IsFalse(data.IsSbmConditionsPSarOversold());
    }

    [TestMethod]
    public void IsSbmConditionsPSarOverbought_NullPSar_ReturnsFalse()
    {
        var data = MakeData(new CryptoData { PSar = null, Sma20 = null }, price: 100m);
        Assert.IsFalse(data.IsSbmConditionsPSarOverbought());
    }

    // ── Stoch GetStochDirHigherInterval ──────────────────────────────

    [TestMethod]
    public void GetStochDirHigherInterval_1m_Returns15m()
    {
        bool result = StochHelper.GetStochDirHigherInterval(
            CryptoScanner.Core.Enums.CryptoIntervalPeriod.interval1m,
            out var higher);
        Assert.IsTrue(result);
        Assert.AreEqual(CryptoScanner.Core.Enums.CryptoIntervalPeriod.interval15m, higher);
    }

    [TestMethod]
    public void GetStochDirHigherInterval_1d_ReturnsFalse()
    {
        bool result = StochHelper.GetStochDirHigherInterval(
            CryptoScanner.Core.Enums.CryptoIntervalPeriod.interval1d,
            out _);
        Assert.IsFalse(result);
    }

    // ── Edge cases: useLowHigh flag on BB ────────────────────────────

    [TestMethod]
    public void IsBelowBollingerBands_UseLowHigh_ChecksLow()
    {
        // With useLowHigh=true, uses candle.Low instead of min(Open,Close).
        // Candle: price=97, Low=96 (price-1). Band = 100 - 3 = 97. Low(96) <= 97 → true.
        var data = MakeData(new CryptoData { Sma20 = 100, BollingerBandsDeviation = 3.0 }, price: 97m);
        Assert.IsTrue(data.IsBelowBollingerBands(useLowHigh: true));
    }

    [TestMethod]
    public void IsAboveBollingerBands_UseLowHigh_ChecksHigh()
    {
        // With useLowHigh=true, uses candle.High instead of max(Open,Close).
        // Candle: price=103, High=104 (price+1). Band = 100 + 3 = 103. High(104) >= 103 → true.
        var data = MakeData(new CryptoData { Sma20 = 100, BollingerBandsDeviation = 3.0 }, price: 103m);
        Assert.IsTrue(data.IsAboveBollingerBands(useLowHigh: true));
    }
}
