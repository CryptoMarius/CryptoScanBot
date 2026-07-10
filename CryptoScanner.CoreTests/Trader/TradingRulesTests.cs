using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

using PauseRule = CryptoScanner.Core.Core.PauseTradingRule;
using SettingsPauseRule = CryptoScanner.Core.Settings.PauseTradingRule;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Tests for <see cref="TradingRules"/>: trading pause logic, caching, and barometer checks.
///
/// TradingRules.CalculateTradingRules is tightly coupled to GlobalData (exchange, symbols,
/// candle lists) so we test the public CheckTradingRules facade which has its own caching
/// and pause-window logic that can be verified independently. The internal rule engine
/// requires a full exchange+symbol setup and is tested indirectly through the existing
/// PaperTradingTests and TraderMechanismTests integration tests.
/// </summary>
[TestClass]
[DoNotParallelize]
public class TradingRulesTests : TestBase
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        InitTestSession();
    }

    // ── CheckTradingRules: pause-window behavior ─────────────────────────

    [TestMethod]
    public void CheckTradingRules_NoPause_ReturnsTrue()
    {
        var pause = new PauseRule();
        CandleTime candleDate = new(1000);
        uint duration = 1; // 1m candle

        bool result = TradingRules.CheckTradingRules(pause, candleDate, duration);

        Assert.IsTrue(result, "Should return true when no pause rules are configured");
    }

    [TestMethod]
    public void CheckTradingRules_ActivePause_ReturnsFalse()
    {
        var pause = new PauseRule();
        CandleTime candleDate = new(1000);
        uint duration = 1;

        // Pre-set a pause that extends into the future
        DateTime closeTime = (candleDate + duration).ToDateTime();
        pause.Until = closeTime.AddMinutes(10);
        pause.Calculated = closeTime;

        bool result = TradingRules.CheckTradingRules(pause, candleDate, duration);

        Assert.IsFalse(result, "Should return false when pause is active (Until > current close time)");
    }

    [TestMethod]
    public void CheckTradingRules_ExpiredPause_ReturnsTrue()
    {
        var pause = new PauseRule();
        CandleTime candleDate = new(2000);
        uint duration = 1;

        // Pre-set a pause that has already expired
        DateTime closeTime = (candleDate + duration).ToDateTime();
        pause.Until = closeTime.AddMinutes(-5);
        pause.Calculated = closeTime;

        bool result = TradingRules.CheckTradingRules(pause, candleDate, duration);

        Assert.IsTrue(result, "Should return true when pause has expired (Until < current close time)");
    }

    [TestMethod]
    public void CheckTradingRules_Caching_SkipsRecalculation()
    {
        var pause = new PauseRule();
        CandleTime candleDate = new(1000);
        uint duration = 1;

        // First call: triggers calculation
        bool result1 = TradingRules.CheckTradingRules(pause, candleDate, duration);
        DateTime firstCalc = pause.Calculated!.Value;

        // Second call with SAME candleDate: should NOT recalculate
        bool result2 = TradingRules.CheckTradingRules(pause, candleDate, duration);

        Assert.AreEqual(firstCalc, pause.Calculated!.Value,
            "Calculated timestamp should not change when called with the same candle time");
        Assert.AreEqual(result1, result2, "Results should be the same");
    }

    [TestMethod]
    public void CheckTradingRules_NewCandle_RecalculatesIfNeeded()
    {
        var pause = new PauseRule();
        CandleTime candleDate1 = new(1000);
        CandleTime candleDate2 = new(1001);
        uint duration = 1;

        TradingRules.CheckTradingRules(pause, candleDate1, duration);
        DateTime firstCalc = pause.Calculated!.Value;

        // Advance to a new candle — should recalculate
        TradingRules.CheckTradingRules(pause, candleDate2, duration);
        DateTime secondCalc = pause.Calculated!.Value;

        Assert.AreNotEqual(firstCalc, secondCalc,
            "Calculated timestamp should update when a new candle arrives");
    }

    // ── CheckTradingRules with active PauseTradingRules ──────────────────

    [TestMethod]
    public void CheckTradingRules_WithConfiguredRule_NoMatch_ReturnsTrue()
    {
        // Add a pause rule for BTCUSDT — it won't match because the test
        // exchange may not have BTCUSDT or the candle data won't trigger the threshold.
        var originalRules = GlobalData.Settings.Trading.PauseTradingRules;
        GlobalData.Settings.Trading.PauseTradingRules =
        [
            new SettingsPauseRule
            {
                Symbol = "NONEXISTENT_SYMBOL_12345",
                Percentage = 5.0,
                Candles = 3,
                Interval = CryptoIntervalPeriod.interval1h,
                CoolDown = 60,
            }
        ];

        try
        {
            var pause = new PauseRule();
            CandleTime candleDate = new(1000);
            uint duration = 1;

            bool result = TradingRules.CheckTradingRules(pause, candleDate, duration);

            Assert.IsTrue(result,
                "Should return true when the configured symbol doesn't exist on the exchange");
        }
        finally
        {
            GlobalData.Settings.Trading.PauseTradingRules = originalRules;
        }
    }

    // ── PauseTradingRule clear ────────────────────────────────────────────

    [TestMethod]
    public void PauseTradingRule_Clear_ResetsAllFields()
    {
        var pause = new PauseRule
        {
            Calculated = DateTime.UtcNow,
            Until = DateTime.UtcNow.AddMinutes(10),
            Text = "test pause",
        };

        pause.Clear();

        Assert.IsNull(pause.Calculated);
        Assert.IsNull(pause.Until);
        Assert.AreEqual("", pause.Text);
    }
}
