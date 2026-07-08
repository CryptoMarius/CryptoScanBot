using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Bbma;

// BbmaState enum lives in SignalBbmaBase; GetBbmaState/Short are in the derived classes.
using static CryptoScanner.Core.Signal.Bbma.SignalBbmaBase;

namespace CryptoScanner.CoreTests.Signal.Bbma;

/// <summary>
/// Unit tests for GetBbmaState and GetBbmaState.
///
/// Standard BB setup used throughout: Mid=100, Deviation=5 → Upper=105, Lower=95.
///
/// Ema50 is chosen per test to avoid unintended interference with the Advance Extreme check
/// (which fires when a wick pierces Ema50 but the candle body stays above/below it).
/// For Reentry and Mlv tests, Ema50 is placed well below/above the price range so it
/// never accidentally triggers the Advance check.
/// </summary>
[TestClass]
public class BbmaStateTests
{
    // Standard Bollinger Band parameters shared by all tests
    private const double Mid = 100.0;
    private const double Dev = 5.0;     // → Upper = 105.0, Lower = 95.0
    private const double Upper = Mid + Dev;   // 105.0  (used in comments for readability)
    private const double Lower = Mid - Dev;   // 95.0

    // Ema50 values
    // EmaFarBelow: used in Long tests to keep Advance check from firing on Reentry/Mlv scenarios
    // EmaFarAbove: used in Short tests for the same reason
    private const double EmaFarBelow = 85.0;
    private const double EmaFarAbove = 115.0;

    /// <summary>
    /// Builds a MyData test object with the specified candle OHLC and indicator values.
    /// TickDecimals = 2 gives a 0.01 tick size — sufficient for 2-decimal prices.
    /// BollingerBandsUpperBand = Sma20 + BollingerBandsDeviation (computed property).
    /// BollingerBandsLowerBand = Sma20 - BollingerBandsDeviation (computed property).
    /// </summary>
    private static MyData MakeData(
        decimal open, decimal high, decimal low, decimal close,
        double wma5Low, double wma10Low,
        double wma5High, double wma10High,
        double sma20 = Mid, double bbDeviation = Dev, double ema50 = EmaFarBelow)
    {
        var candle = new CryptoCandle
        {
            TickDecimals = 2,
            Open = open,
            High = high,
            Low = low,
            Close = close,
        };

        var data = new CryptoData
        {
            Sma20 = sma20,
            BollingerBandsDeviation = bbDeviation,
            BollingerBandsPercentage = 1.0,
            Ema50 = ema50,
            Wma05Low = wma5Low,
            Wma10Low = wma10Low,
            Wma05High = wma5High,
            Wma10High = wma10High,
        };

        return new MyData { Candle = candle, CandleData = data };
    }


    // ==============================================================
    // GetBbmaState — state detection for Long setups (WMA on lows)
    // BB: Lower=95, Mid=100, Upper=105
    // ==============================================================

    [TestMethod]
    public void StateLong_None_WhenNoPatternMatches()
    {
        // WMA5Low=WMA10Low=97 (both inside BB, equal → no Mlv), Low=101 (above both WMAs)
        var data = MakeData(101, 102, 101, 101, wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.None, SignalBbmaLong.GetBbmaState(data));
    }

    [TestMethod]
    public void StateLong_Extreme_WhenMaAndWickBelowLowerBand()
    {
        // WMA5Low (94) < Lower (95) AND wick: Low (93) < Lower (95), Close (100) > Lower → Extreme
        var data = MakeData(100, 101, 93, 100, wma5Low: 94, wma10Low: 96, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Extreme, SignalBbmaLong.GetBbmaState(data));
    }

    [TestMethod]
    public void StateLong_Extreme_NotFired_WhenMaOnlyBelowLowerBand()
    {
        // WMA5Low (94) < Lower (95) but candle is entirely inside the band (Low=99 > Lower=95)
        // Pine requires both MA and wick — MA alone is not enough.
        var data = MakeData(100, 101, 99, 100, wma5Low: 94, wma10Low: 96, wma5High: 103, wma10High: 103);
        Assert.AreNotEqual(BbmaState.Extreme, SignalBbmaLong.GetBbmaState(data));
    }

    [TestMethod]
    public void StateLong_MagicExtreme_MergedIntoExtreme_WhenBothWmasBelowLowerBand()
    {
        // WMA5Low (94) < Lower (95) AND WMA10Low (94.5) < Lower (95)
        // MagicExtreme was merged into Extreme in production (no downstream code uses the distinction).
        var data = MakeData(100, 101, 99, 100, wma5Low: 94, wma10Low: 94.5, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Extreme, SignalBbmaLong.GetBbmaState(data));
    }

    [TestMethod]
    public void StateLong_MagicExtreme_MergedIntoExtreme_TakesPriorityOver_PlainExtreme()
    {
        // Both MAs below lower band → returns Extreme (MagicExtreme merged into Extreme)
        var data = MakeData(100, 101, 94, 100, wma5Low: 94, wma10Low: 94, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Extreme, SignalBbmaLong.GetBbmaState(data));
    }

    [TestMethod]
    public void StateLong_Extreme_TakesPriorityOver_Reentry()
    {
        // WMA5Low (94) < Lower (95) AND wick: Low (94) < Lower → Extreme.
        // Low (94) <= WMA5Low (94) would also satisfy Reentry, but Extreme is checked first and wins.
        var data = MakeData(99, 101, 94, 99, wma5Low: 94, wma10Low: 96, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Extreme, SignalBbmaLong.GetBbmaState(data));
    }

    [TestMethod]
    public void StateLong_Extreme_NotFired_WhenWickOnlyBelowLowerBand()
    {
        // WMAs inside BB (wma5Low=97 > Lower=95). Wick dips below Lower (94 < 95), close inside.
        // Pine requires MA outside band — wick alone is not enough.
        var data = MakeData(98, 99, 94, 97, wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103);
        Assert.AreNotEqual(BbmaState.Extreme, SignalBbmaLong.GetBbmaState(data));
    }

    [TestMethod]
    public void StateLong_Extreme_Advance_Disabled_ReturnsNone()
    {
        // Advance Extreme is currently disabled in production (if (false && ...)).
        // When re-enabled: Ema50=98, Low(97.5)<98, Close(99)>98, Open(99)>98 → Advance Extreme.
        var data = MakeData(99, 100, 97.5m, 99,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: 98.0);
        Assert.AreEqual(BbmaState.None, SignalBbmaLong.GetBbmaState(data));
    }

    [TestMethod]
    public void StateLong_Extreme_Advance_NotFired_WhenCloseAlsoBelowEma50()
    {
        // Ema50 = 98. Low (97.5) < 98, but Close (97) < 98 too → body not above Ema50 → no Advance
        // No MA-outside-band. Low (97.5) <= WMA5Low (97)? No. Low (97.5) <= WMA10Low (97)? No → None
        var data = MakeData(97, 98, 97.5m, 97,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: 98.0);
        Assert.AreEqual(BbmaState.None, SignalBbmaLong.GetBbmaState(data));
    }

    [TestMethod]
    public void StateLong_Reentry_WhenLowExactlyAtWma5()
    {
        // Low (97) = WMA5Low (97). Close (101) >= middleBand (100) and > ema50 (85) for uptrend.
        var data = MakeData(101, 102, 97, 101, wma5Low: 97, wma10Low: 98, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Reentry, SignalBbmaLong.GetBbmaState(data));
    }

    [TestMethod]
    public void StateLong_Reentry_WhenLowBelowWma5()
    {
        // Low (96.5) < WMA5Low (97). Close (101) >= middleBand (100). No MA-outside-band.
        var data = MakeData(101, 102, 96.5m, 101, wma5Low: 97, wma10Low: 98, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Reentry, SignalBbmaLong.GetBbmaState(data));
    }

    [TestMethod]
    public void StateLong_Reentry_WhenLowBelowWma10ButAboveWma5()
    {
        // WMA5Low (96), WMA10Low (97.5). Low (97) > WMA5Low (96) but Low (97) <= WMA10Low (97.5).
        // Close (101) >= middleBand (100).
        var data = MakeData(101, 102, 97, 101, wma5Low: 96, wma10Low: 97.5, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Reentry, SignalBbmaLong.GetBbmaState(data));
    }

    [TestMethod]
    public void StateLong_Reentry_TakesPriorityOver_Mlv()
    {
        // Low (96.5) <= WMA10Low (97) → Reentry. Low (96.5) > bbLower (95) → no Mlv (wick-based).
        // Close (101) >= middleBand (100), close > ema50 (85) for uptrend.
        var data = MakeData(101, 102, 96.5m, 101, wma5Low: 96, wma10Low: 97, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Reentry, SignalBbmaLong.GetBbmaState(data));
    }

    [TestMethod]
    public void StateLong_Mlv_WhenWickPiercesLowerBandAndMa5Inside()
    {
        // MHV (Pine-aligned): wick below Lower (94 < 95), close recovered inside (97 > 95), MA5 inside band (wma5Low=97 >= Lower=95).
        // Ema50 far below → no Advance. Close (97) < Mid (100) → no Reentry.
        var data = MakeData(98, 99, 94, 97, wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Mlv, SignalBbmaLong.GetBbmaState(data));
    }

    [TestMethod]
    public void StateLong_Mlv_TakesPriorityOver_Reentry()
    {
        // MHV condition (low < lower, close > lower) fires before Reentry even when both apply.
        // Close (101) >= Mid (100), Low (94) <= WMA zone — Reentry would also match, but MHV wins.
        var data = MakeData(101, 102, 94, 101, wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Mlv, SignalBbmaLong.GetBbmaState(data));
    }

    [TestMethod]
    public void StateLong_None_WhenNoWickBelowLowerBand()
    {
        // MA5 inside band, wick does not pierce Lower (Low=96 > Lower=95) → no MHV → None.
        var data = MakeData(98, 99, 96, 98, wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.None, SignalBbmaLong.GetBbmaState(data));
    }


    // ==============================================================
    // GetBbmaState — state detection for Short setups (WMA on highs)
    // BB: Lower=95, Mid=100, Upper=105
    // ==============================================================

    [TestMethod]
    public void StateShort_None_WhenNoPatternMatches()
    {
        // WMA5High=WMA10High=103 (equal → no Mlv), High=98 (below both WMAs).
        // Ema50 far above → no Advance.
        var data = MakeData(99, 98, 96, 99,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.None, SignalBbmaShort.GetBbmaState(data));
    }

    [TestMethod]
    public void StateShort_Extreme_WhenMaAndWickAboveUpperBand()
    {
        // WMA5High (106) > Upper (105) AND wick: High (107) > Upper (105), Close (100) < Upper → Extreme
        var data = MakeData(100, 107, 99, 100,
            wma5Low: 97, wma10Low: 97, wma5High: 106, wma10High: 104, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Extreme, SignalBbmaShort.GetBbmaState(data));
    }

    [TestMethod]
    public void StateShort_Extreme_NotFired_WhenMaOnlyAboveUpperBand()
    {
        // WMA5High (106) > Upper (105) but candle is entirely inside the band (High=101 < Upper=105)
        // Pine requires both MA and wick — MA alone is not enough.
        var data = MakeData(100, 101, 99, 100,
            wma5Low: 97, wma10Low: 97, wma5High: 106, wma10High: 104, ema50: EmaFarAbove);
        Assert.AreNotEqual(BbmaState.Extreme, SignalBbmaShort.GetBbmaState(data));
    }

    [TestMethod]
    public void StateShort_MagicExtreme_MergedIntoExtreme_WhenBothWmasAboveUpperBand()
    {
        // WMA5High (106) > Upper (105) AND WMA10High (106.5) > Upper (105)
        // MagicExtreme was merged into Extreme in production.
        var data = MakeData(100, 101, 99, 100,
            wma5Low: 97, wma10Low: 97, wma5High: 106, wma10High: 106.5, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Extreme, SignalBbmaShort.GetBbmaState(data));
    }

    [TestMethod]
    public void StateShort_MagicExtreme_MergedIntoExtreme_TakesPriorityOver_PlainExtreme()
    {
        // Both MAs above upper band → returns Extreme (MagicExtreme merged into Extreme)
        var data = MakeData(100, 106, 99, 100,
            wma5Low: 97, wma10Low: 97, wma5High: 106, wma10High: 106, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Extreme, SignalBbmaShort.GetBbmaState(data));
    }

    [TestMethod]
    public void StateShort_Extreme_TakesPriorityOver_Reentry()
    {
        // WMA5High (106) > Upper (105) AND wick: High (106) > Upper → Extreme.
        // High (106) >= WMA5High (106) → also Reentry, but Extreme is checked first and wins.
        var data = MakeData(101, 106, 100, 101,
            wma5Low: 97, wma10Low: 97, wma5High: 106, wma10High: 104, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Extreme, SignalBbmaShort.GetBbmaState(data));
    }

    [TestMethod]
    public void StateShort_Extreme_NotFired_WhenWickOnlyAboveUpperBand()
    {
        // WMAs inside BB (wma5High=103 < Upper=105). Wick pierces Upper (106 > 105), close inside.
        // Pine requires MA outside band — wick alone is not enough.
        var data = MakeData(103, 106, 102, 103,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103.5, ema50: EmaFarAbove);
        Assert.AreNotEqual(BbmaState.Extreme, SignalBbmaShort.GetBbmaState(data));
    }

    [TestMethod]
    public void StateShort_Extreme_Advance_Disabled_ReturnsNone()
    {
        // Advance Extreme is currently disabled in production (if (false && ...)).
        // When re-enabled: Ema50=102, High(102.5)>102, Close(101)<102, Open(101.5)<102 → Advance Extreme.
        var data = MakeData(101.5m, 102.5m, 100, 101,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: 102.0);
        Assert.AreEqual(BbmaState.None, SignalBbmaShort.GetBbmaState(data));
    }

    [TestMethod]
    public void StateShort_Extreme_Advance_NotFired_WhenCloseAlsoAboveEma50()
    {
        // Ema50 = 102. High (102.5) > 102, but Close (103) > 102 too → body not below Ema50 → no Advance.
        // High (102.5) < WMA5High (103) → no Reentry. WMA5High (103) <= Upper (105), WMA5High not > WMA10High (103) → no Mlv → None
        var data = MakeData(103, 102.5m, 100, 103,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: 102.0);
        Assert.AreEqual(BbmaState.None, SignalBbmaShort.GetBbmaState(data));
    }

    [TestMethod]
    public void StateShort_Reentry_WhenHighExactlyAtWma5()
    {
        // High (103) = WMA5High (103). Close (99) <= middleBand (100) and < ema50 (115) for downtrend.
        var data = MakeData(99, 103, 98, 99,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103.5, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Reentry, SignalBbmaShort.GetBbmaState(data));
    }

    [TestMethod]
    public void StateShort_Reentry_WhenHighAboveWma5()
    {
        // High (103.5) > WMA5High (103). Close (99) <= middleBand (100). No MA-outside-band.
        var data = MakeData(99, 103.5m, 98, 99,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103.5, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Reentry, SignalBbmaShort.GetBbmaState(data));
    }

    [TestMethod]
    public void StateShort_Reentry_WhenHighAboveWma10ButBelowWma5()
    {
        // WMA5High (104), WMA10High (102.5). High (103) > WMA10High (102.5) but < WMA5High (104).
        // Close (99) <= middleBand (100).
        var data = MakeData(99, 103, 98, 99,
            wma5Low: 97, wma10Low: 97, wma5High: 104, wma10High: 102.5, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Reentry, SignalBbmaShort.GetBbmaState(data));
    }

    [TestMethod]
    public void StateShort_Reentry_TakesPriorityOver_Mlv()
    {
        // High (103.5) >= WMA10High (103) → Reentry. High (103.5) < bbUpper (105) → no Mlv (wick-based).
        // Close (99) <= middleBand (100), close < ema50 (115) for downtrend.
        var data = MakeData(99, 103.5m, 98, 99,
            wma5Low: 97, wma10Low: 97, wma5High: 104, wma10High: 103, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Reentry, SignalBbmaShort.GetBbmaState(data));
    }

    [TestMethod]
    public void StateShort_Mlv_WhenWickPiercesUpperBandAndMa5Inside()
    {
        // MHV (Pine-aligned): wick above Upper (106 > 105), close recovered inside (103 < 105), MA5 inside band (wma5High=103 <= Upper=105).
        // Ema50 far above → no Advance. Close (103) > Mid (100) → no Reentry.
        var data = MakeData(103, 106, 102, 103,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Mlv, SignalBbmaShort.GetBbmaState(data));
    }

    [TestMethod]
    public void StateShort_Mlv_TakesPriorityOver_Reentry()
    {
        // MHV condition (high > upper, close < upper) fires before Reentry even when both apply.
        // Close (99) <= Mid (100), High (106) >= WMA zone — Reentry would also match, but MHV wins.
        var data = MakeData(99, 106, 97, 99,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Mlv, SignalBbmaShort.GetBbmaState(data));
    }

    [TestMethod]
    public void StateShort_None_WhenNoWickAboveUpperBand()
    {
        // MA5 inside band, wick does not pierce Upper (High=104 < Upper=105) → no MHV → None.
        var data = MakeData(101, 104, 100, 101,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.None, SignalBbmaShort.GetBbmaState(data));
    }
}
