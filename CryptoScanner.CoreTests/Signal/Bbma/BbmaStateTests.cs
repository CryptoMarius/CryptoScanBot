using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal;
using CryptoScanner.Core.Signal.Bbma;

using static CryptoScanner.Core.Signal.Bbma.SignalBbmaBase;

namespace CryptoScanner.CoreTests.Signal.Bbma;

/// <summary>
/// Unit tests for BbmaStateLong and BbmaStateShort.
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
    // BbmaStateLong — state detection for Long setups (WMA on lows)
    // BB: Lower=95, Mid=100, Upper=105
    // ==============================================================

    [TestMethod]
    public void StateLong_None_WhenNoPatternMatches()
    {
        // WMA5Low=WMA10Low=97 (both inside BB, equal → no Mlv), Low=101 (above both WMAs)
        var data = MakeData(101, 102, 101, 101, wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.None, BbmaStateLong(data));
    }

    [TestMethod]
    public void StateLong_Extreme_TypeA_WhenWma5BelowLowerBand()
    {
        // WMA5Low (94) < Lower (95), WMA10Low (96) inside band → Extreme TypeA
        var data = MakeData(100, 101, 99, 100, wma5Low: 94, wma10Low: 96, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Extreme, BbmaStateLong(data));
    }

    [TestMethod]
    public void StateLong_MagicExtreme_WhenBothWmasBelowLowerBand()
    {
        // WMA5Low (94) < Lower (95) AND WMA10Low (94.5) < Lower (95)
        var data = MakeData(100, 101, 99, 100, wma5Low: 94, wma10Low: 94.5, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.MagicExtreme, BbmaStateLong(data));
    }

    [TestMethod]
    public void StateLong_MagicExtreme_TakesPriorityOver_Extreme()
    {
        // Both MAs below lower band → MagicExtreme wins over plain Extreme
        var data = MakeData(100, 101, 94, 100, wma5Low: 94, wma10Low: 94, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.MagicExtreme, BbmaStateLong(data));
    }

    [TestMethod]
    public void StateLong_Extreme_TypeA_TakesPriorityOver_Reentry()
    {
        // WMA5Low (94) < Lower (95) → TypeA. Low (94) <= WMA5Low (94) would also be Reentry,
        // but TypeA is checked first and wins.
        var data = MakeData(99, 101, 94, 99, wma5Low: 94, wma10Low: 96, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Extreme, BbmaStateLong(data));
    }

    [TestMethod]
    public void StateLong_Extreme_TypeB_WickBelowLowerBand()
    {
        // WMAs inside BB. Wick dips below Lower (94 < 95), but close and open are inside the band.
        // TypeB is checked before Reentry, so even though Low (94) <= WMA5Low (97) → Extreme wins.
        // Ema50 = EmaFarBelow to prevent Advance check from interfering.
        var data = MakeData(98, 99, 94, 97, wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Extreme, BbmaStateLong(data));
    }

    [TestMethod]
    public void StateLong_Extreme_TypeB_NotDetected_WhenWickDetectionOff()
    {
        // Same values as TypeB test above, but wick detection disabled → no Extreme TypeB
        var data = MakeData(98, 99, 94, 97, wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103);
        Assert.AreNotEqual(BbmaState.Extreme, BbmaStateLong(data, allowWickDetection: false));
    }

    [TestMethod]
    public void StateLong_Extreme_TypeB_FallsToReentry_WhenWickDetectionOff()
    {
        // TypeB suppressed, and Low (94) <= WMA5Low (97) → falls through to Reentry
        var data = MakeData(98, 99, 94, 97, wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Reentry, BbmaStateLong(data, allowWickDetection: false));
    }

    [TestMethod]
    public void StateLong_Extreme_Advance_WickBelowEma50()
    {
        // Ema50 = 98. Low (97.5) < 98, Close (99) > 98, Open (99) > 98 → Advance Extreme.
        // WMAs at 97 — no TypeA. Low (97.5) > WMA5Low (97) — no Reentry conflict before Advance.
        var data = MakeData(99, 100, 97.5m, 99,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: 98.0);
        Assert.AreEqual(BbmaState.Extreme, BbmaStateLong(data));
    }

    [TestMethod]
    public void StateLong_Extreme_Advance_NotDetected_WhenWickDetectionOff()
    {
        // Same values, wick detection disabled → Advance suppressed → None
        var data = MakeData(99, 100, 97.5m, 99,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: 98.0);
        Assert.AreNotEqual(BbmaState.Extreme, BbmaStateLong(data, allowWickDetection: false));
    }

    [TestMethod]
    public void StateLong_Extreme_Advance_NotFired_WhenCloseAlsoBelowEma50()
    {
        // Ema50 = 98. Low (97.5) < 98, but Close (97) < 98 too → body not above Ema50 → no Advance
        // No TypeA/TypeB. Low (97.5) <= WMA5Low (97)? No. Low (97.5) <= WMA10Low (97)? No → None
        var data = MakeData(97, 98, 97.5m, 97,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: 98.0);
        Assert.AreEqual(BbmaState.None, BbmaStateLong(data));
    }

    [TestMethod]
    public void StateLong_Reentry_WhenLowExactlyAtWma5()
    {
        // Low (97) = WMA5Low (97). Ema50 = EmaFarBelow to avoid Advance interference.
        var data = MakeData(99, 101, 97, 99, wma5Low: 97, wma10Low: 98, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Reentry, BbmaStateLong(data));
    }

    [TestMethod]
    public void StateLong_Reentry_WhenLowBelowWma5()
    {
        // Low (96.5) < WMA5Low (97). No TypeA (WMA5Low >= Lower 95). Ema50 far below.
        var data = MakeData(99, 101, 96.5m, 99, wma5Low: 97, wma10Low: 98, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Reentry, BbmaStateLong(data));
    }

    [TestMethod]
    public void StateLong_Reentry_WhenLowBelowWma10ButAboveWma5()
    {
        // WMA5Low (96), WMA10Low (97.5). Low (97) > WMA5Low (96) but Low (97) <= WMA10Low (97.5).
        // Ema50 = EmaFarBelow to avoid Advance interference.
        var data = MakeData(99, 101, 97, 99, wma5Low: 96, wma10Low: 97.5, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Reentry, BbmaStateLong(data));
    }

    [TestMethod]
    public void StateLong_Reentry_TakesPriorityOver_Mlv()
    {
        // WMA5Low (96) >= Lower (95) AND WMA5Low (96) < WMA10Low (97) → Mlv condition true.
        // But Low (96.5) <= WMA10Low (97) → Reentry is checked first and wins.
        // Ema50 = EmaFarBelow, Close/Open < EmaFarBelow is irrelevant; Advance won't fire anyway.
        var data = MakeData(99, 101, 96.5m, 97, wma5Low: 96, wma10Low: 97, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Reentry, BbmaStateLong(data));
    }

    [TestMethod]
    public void StateLong_Mlv_WhenWma5BetweenLowerBandAndWma10()
    {
        // WMA5Low (96) >= Lower (95) AND WMA5Low (96) < WMA10Low (97).
        // Low (101) well above both WMAs → no Reentry. Ema50 far below → no Advance.
        var data = MakeData(101, 102, 101, 101, wma5Low: 96, wma10Low: 97, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.Mlv, BbmaStateLong(data));
    }

    [TestMethod]
    public void StateLong_NotMlv_WhenWma5EqualsWma10()
    {
        // Mlv condition requires WMA5Low < WMA10Low (strictly). Equal → no Mlv → None.
        var data = MakeData(101, 102, 101, 101, wma5Low: 96, wma10Low: 96, wma5High: 103, wma10High: 103);
        Assert.AreEqual(BbmaState.None, BbmaStateLong(data));
    }


    // ==============================================================
    // BbmaStateShort — state detection for Short setups (WMA on highs)
    // BB: Lower=95, Mid=100, Upper=105
    // ==============================================================

    [TestMethod]
    public void StateShort_None_WhenNoPatternMatches()
    {
        // WMA5High=WMA10High=103 (equal → no Mlv), High=98 (below both WMAs).
        // Ema50 far above → no Advance.
        var data = MakeData(99, 98, 96, 99,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.None, BbmaStateShort(data));
    }

    [TestMethod]
    public void StateShort_Extreme_TypeA_WhenWma5AboveUpperBand()
    {
        // WMA5High (106) > Upper (105), WMA10High (104) inside band → Extreme TypeA
        var data = MakeData(100, 101, 99, 100,
            wma5Low: 97, wma10Low: 97, wma5High: 106, wma10High: 104, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Extreme, BbmaStateShort(data));
    }

    [TestMethod]
    public void StateShort_MagicExtreme_WhenBothWmasAboveUpperBand()
    {
        // WMA5High (106) > Upper (105) AND WMA10High (106.5) > Upper (105)
        var data = MakeData(100, 101, 99, 100,
            wma5Low: 97, wma10Low: 97, wma5High: 106, wma10High: 106.5, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.MagicExtreme, BbmaStateShort(data));
    }

    [TestMethod]
    public void StateShort_MagicExtreme_TakesPriorityOver_Extreme()
    {
        // Both MAs above upper band → MagicExtreme wins over plain Extreme
        var data = MakeData(100, 106, 99, 100,
            wma5Low: 97, wma10Low: 97, wma5High: 106, wma10High: 106, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.MagicExtreme, BbmaStateShort(data));
    }

    [TestMethod]
    public void StateShort_Extreme_TypeA_TakesPriorityOver_Reentry()
    {
        // WMA5High (106) > Upper (105) → TypeA. High (106) >= WMA5High (106) → also Reentry,
        // but TypeA is checked first and wins.
        var data = MakeData(101, 106, 100, 101,
            wma5Low: 97, wma10Low: 97, wma5High: 106, wma10High: 104, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Extreme, BbmaStateShort(data));
    }

    [TestMethod]
    public void StateShort_Extreme_TypeB_WickAboveUpperBand()
    {
        // WMAs inside BB. Wick pierces Upper (106 > 105), close and open inside the band.
        // TypeB is checked before Reentry, so even though High (106) >= WMA5High (103) → Extreme wins.
        // Ema50 = EmaFarAbove to prevent Advance check from interfering.
        var data = MakeData(103, 106, 102, 103,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103.5, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Extreme, BbmaStateShort(data));
    }

    [TestMethod]
    public void StateShort_Extreme_TypeB_NotDetected_WhenWickDetectionOff()
    {
        // Same values, wick detection disabled → no Extreme TypeB
        var data = MakeData(103, 106, 102, 103,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103.5, ema50: EmaFarAbove);
        Assert.AreNotEqual(BbmaState.Extreme, BbmaStateShort(data, allowWickDetection: false));
    }

    [TestMethod]
    public void StateShort_Extreme_TypeB_FallsToReentry_WhenWickDetectionOff()
    {
        // TypeB suppressed, and High (106) >= WMA5High (103) → falls through to Reentry
        var data = MakeData(103, 106, 102, 103,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103.5, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Reentry, BbmaStateShort(data, allowWickDetection: false));
    }

    [TestMethod]
    public void StateShort_Extreme_Advance_WickAboveEma50()
    {
        // Ema50 = 102. High (102.5) > 102, Close (101) < 102, Open (101.5) < 102 → Advance Extreme.
        // WMAs at 103 → High (102.5) < WMA5High (103) → no Reentry conflict before Advance.
        var data = MakeData(101.5m, 102.5m, 100, 101,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: 102.0);
        Assert.AreEqual(BbmaState.Extreme, BbmaStateShort(data));
    }

    [TestMethod]
    public void StateShort_Extreme_Advance_NotDetected_WhenWickDetectionOff()
    {
        // Same values, wick detection disabled → Advance suppressed → None
        var data = MakeData(101.5m, 102.5m, 100, 101,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: 102.0);
        Assert.AreNotEqual(BbmaState.Extreme, BbmaStateShort(data, allowWickDetection: false));
    }

    [TestMethod]
    public void StateShort_Extreme_Advance_NotFired_WhenCloseAlsoAboveEma50()
    {
        // Ema50 = 102. High (102.5) > 102, but Close (103) > 102 too → body not below Ema50 → no Advance.
        // High (102.5) < WMA5High (103) → no Reentry. WMA5High (103) <= Upper (105), WMA5High not > WMA10High (103) → no Mlv → None
        var data = MakeData(103, 102.5m, 100, 103,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: 102.0);
        Assert.AreEqual(BbmaState.None, BbmaStateShort(data));
    }

    [TestMethod]
    public void StateShort_Reentry_WhenHighExactlyAtWma5()
    {
        // High (103) = WMA5High (103). Ema50 = EmaFarAbove to avoid Advance interference.
        var data = MakeData(101, 103, 100, 101,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103.5, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Reentry, BbmaStateShort(data));
    }

    [TestMethod]
    public void StateShort_Reentry_WhenHighAboveWma5()
    {
        // High (103.5) > WMA5High (103). No TypeA (WMA5High <= Upper 105). Ema50 far above.
        var data = MakeData(101, 103.5m, 100, 101,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103.5, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Reentry, BbmaStateShort(data));
    }

    [TestMethod]
    public void StateShort_Reentry_WhenHighAboveWma10ButBelowWma5()
    {
        // WMA5High (104), WMA10High (102.5). High (103) > WMA10High (102.5) but < WMA5High (104).
        // Ema50 = EmaFarAbove to avoid Advance interference.
        var data = MakeData(101, 103, 100, 101,
            wma5Low: 97, wma10Low: 97, wma5High: 104, wma10High: 102.5, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Reentry, BbmaStateShort(data));
    }

    [TestMethod]
    public void StateShort_Reentry_TakesPriorityOver_Mlv()
    {
        // WMA5High (104) > WMA10High (103) → Mlv condition true.
        // High (103.5) >= WMA10High (103) → Reentry is checked first and wins.
        // Ema50 = EmaFarAbove to prevent Advance interference.
        var data = MakeData(101, 103.5m, 100, 101,
            wma5Low: 97, wma10Low: 97, wma5High: 104, wma10High: 103, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Reentry, BbmaStateShort(data));
    }

    [TestMethod]
    public void StateShort_Mlv_WhenWma5BetweenUpperBandAndWma10()
    {
        // WMA5High (104) <= Upper (105) AND WMA5High (104) > WMA10High (103).
        // High (98) below both WMAs → no Reentry. Ema50 far above → no Advance.
        var data = MakeData(99, 98, 97, 99,
            wma5Low: 97, wma10Low: 97, wma5High: 104, wma10High: 103, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.Mlv, BbmaStateShort(data));
    }

    [TestMethod]
    public void StateShort_NotMlv_WhenWma5EqualsWma10()
    {
        // Mlv condition requires WMA5High > WMA10High (strictly). Equal → no Mlv → None.
        var data = MakeData(99, 98, 97, 99,
            wma5Low: 97, wma10Low: 97, wma5High: 103, wma10High: 103, ema50: EmaFarAbove);
        Assert.AreEqual(BbmaState.None, BbmaStateShort(data));
    }
}
