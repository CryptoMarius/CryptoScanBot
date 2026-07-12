using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Trader;

using static CryptoScanner.Core.Trader.StopLossCalculator;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Unit tests for <see cref="StopLossCalculator"/>, the pure stop-loss price calculator
/// extracted from PositionMonitor.CalculateSlPrices.
///
/// Signal SL is always preferred when present (regardless of DCA state). Global SL is the
/// fallback when no signal SL is available. The anchor is always ExtremeDcaPrice when set,
/// ensuring the SL sits beyond all DCA levels.
/// </summary>
[TestClass]
public class StopLossCalculatorTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────

    private static SlInput LongBase() => new()
    {
        Side = CryptoTradeSide.Long,
        SlPercentage = 3m,       // 3% signal SL
        EntryPrice = 100m,
        ExtremeDcaPrice = null,
        GlobalStopLossPercentage = 5m,
        GlobalStopLossLimitPercentage = 6m,
    };

    private static SlInput ShortBase() => new()
    {
        Side = CryptoTradeSide.Short,
        SlPercentage = 3m,
        EntryPrice = 100m,
        ExtremeDcaPrice = null,
        GlobalStopLossPercentage = 5m,
        GlobalStopLossLimitPercentage = 6m,
    };


    // ═══════════════════════════════════════════════════════════════════════
    //  Priority 1: Signal SL
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void SignalSl_Long_StopBelowSignalPrice()
    {
        var input = LongBase();
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
        // Long 3% SL from 100 → stop = 100 - 1*100*0.03 = 97
        Assert.AreEqual(97m, result.Stop);
        Assert.IsNotNull(result.Limit);
        // Limit should be below the stop (further from entry for a long)
        Assert.IsTrue(result.Limit < result.Stop,
            $"Long signal-SL limit ({result.Limit}) should be below stop ({result.Stop})");
    }

    [TestMethod]
    public void SignalSl_Short_StopAboveSignalPrice()
    {
        var input = ShortBase();
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
        // Short 3% SL from 100 → stop = 100 - (-1)*100*0.03 = 103
        Assert.AreEqual(103m, result.Stop);
        Assert.IsNotNull(result.Limit);
        // Limit should be above the stop (further from entry for a short)
        Assert.IsTrue(result.Limit > result.Stop,
            $"Short signal-SL limit ({result.Limit}) should be above stop ({result.Stop})");
    }

    [TestMethod]
    public void SignalSl_LimitHas1PercentBuffer()
    {
        var input = LongBase();
        var result = Calculate(input);

        // stop = 97, limit = 97 - 1*97*0.01 = 97 - 0.97 = 96.03
        decimal expectedLimit = 97m - 97m * 0.01m;
        Assert.AreEqual(expectedLimit, result.Limit);
    }

    [TestMethod]
    public void SignalSl_AlwaysUsedWhenPresent()
    {
        // Signal SL is always preferred regardless of DCA state
        var input = LongBase() with { ExtremeDcaPrice = 95m };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  Priority 2: Global SL (signal SL not available)
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void GlobalSl_UsedWhenNoSignalSl()
    {
        var input = LongBase() with { SlPercentage = null };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Global, result.Source);
        // Anchor = EntryPrice = 100, 5% SL → stop = 100 - 100*0.05 = 95
        Assert.AreEqual(95m, result.Stop);
    }

    [TestMethod]
    public void GlobalSl_AnchorOnExtremeDcaPrice()
    {
        var input = LongBase() with
        {
            SlPercentage = null,
            ExtremeDcaPrice = 90m,  // lowest DCA buy at 90
        };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Global, result.Source);
        // 5% from anchor 90 → stop = 90 - 90*0.05 = 85.5
        Assert.AreEqual(85.5m, result.Stop);
    }

    [TestMethod]
    public void GlobalSl_FallsBackToEntryPriceWhenNoDca()
    {
        var input = LongBase() with
        {
            SlPercentage = null,
            ExtremeDcaPrice = null,
            EntryPrice = 100m,
        };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Global, result.Source);
        Assert.AreEqual(95m, result.Stop);  // 5% from 100
    }

    [TestMethod]
    public void GlobalSl_Short_StopAboveAnchor()
    {
        var input = ShortBase() with { SlPercentage = null };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Global, result.Source);
        // Short 5% SL from 100 → stop = 100 - (-1)*100*0.05 = 105
        Assert.AreEqual(105m, result.Stop);
    }

    [TestMethod]
    public void GlobalSl_LimitBeyondStop()
    {
        var input = LongBase() with { SlPercentage = null };
        var result = Calculate(input);

        // stop = 95, limit at 6% from 100 = 94
        Assert.AreEqual(94m, result.Limit);
        Assert.IsTrue(result.Limit < result.Stop,
            "Long global-SL limit should be below (further from entry than) stop");
    }

    [TestMethod]
    public void GlobalSl_MisconfiguredLimitPercentage_FallsBackToStopPlus1()
    {
        // StopLossLimitPercentage <= StopLossPercentage is a misconfiguration
        var input = LongBase() with
        {
            SlPercentage = null,
            GlobalStopLossPercentage = 5m,
            GlobalStopLossLimitPercentage = 4m,  // wrong: limit < stop
        };
        var result = Calculate(input);

        // Fallback: limitPct = 5 + 1 = 6 → limit = 100 - 100*0.06 = 94
        Assert.AreEqual(94m, result.Limit);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  No SL
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void NoSl_WhenNoSignalAndGlobalDisabled()
    {
        var input = LongBase() with
        {
            SlPercentage = null,
            GlobalStopLossPercentage = 0m,
        };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.None, result.Source);
        Assert.IsNull(result.Stop);
        Assert.IsNull(result.Limit);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  SL must NEVER be between entry and DCA
    // ═══════════════════════════════════════════════════════════════════════

    [TestMethod]
    public void SignalSl_Long_AnchorsOnDcaWhenPresent()
    {
        var input = LongBase() with { ExtremeDcaPrice = 95m };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
        // 3% from DCA anchor 95 → stop = 95 - 95*0.03 = 92.15
        Assert.AreEqual(92.15m, result.Stop);
        Assert.IsTrue(result.Stop < 95m,
            $"Long SL ({result.Stop}) must be below the most extreme DCA (95)");
    }

    [TestMethod]
    public void SignalSl_Short_AnchorsOnDcaWhenPresent()
    {
        var input = ShortBase() with { ExtremeDcaPrice = 105m };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
        // 3% from DCA anchor 105 → stop = 105 + 105*0.03 = 108.15
        Assert.AreEqual(108.15m, result.Stop);
        Assert.IsTrue(result.Stop > 105m,
            $"Short SL ({result.Stop}) must be above the most extreme DCA (105)");
    }

    /// <summary>
    /// Core regression: small signal SL% with large DCA distance. Without the fix the SL
    /// would sit at 98.5 (between entry 100 and DCA 95) — a guaranteed early stop-out that
    /// defeats the purpose of the DCA.
    /// </summary>
    [TestMethod]
    public void SignalSl_Long_NeverBetweenEntryAndDca()
    {
        var input = LongBase() with
        {
            SlPercentage = 1.5m,
            ExtremeDcaPrice = 95m,
        };
        var result = Calculate(input);

        // stop = 95 - 95*0.015 = 93.575
        Assert.AreEqual(93.575m, result.Stop);
        Assert.IsTrue(result.Stop < 95m,
            $"Long SL ({result.Stop}) must be below DCA (95), not between entry (100) and DCA");
    }

    [TestMethod]
    public void SignalSl_Short_NeverBetweenEntryAndDca()
    {
        var input = ShortBase() with
        {
            SlPercentage = 1.5m,
            ExtremeDcaPrice = 105m,
        };
        var result = Calculate(input);

        // stop = 105 + 105*0.015 = 106.575
        Assert.AreEqual(106.575m, result.Stop);
        Assert.IsTrue(result.Stop > 105m,
            $"Short SL ({result.Stop}) must be above DCA (105), not between entry (100) and DCA");
    }

    [TestMethod]
    public void GlobalSl_Long_NeverBetweenEntryAndDca()
    {
        var input = LongBase() with
        {
            SlPercentage = null,
            ExtremeDcaPrice = 95m,
            GlobalStopLossPercentage = 1.5m,
            GlobalStopLossLimitPercentage = 2.5m,
        };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Global, result.Source);
        // stop = 95 - 95*0.015 = 93.575
        Assert.AreEqual(93.575m, result.Stop);
        Assert.IsTrue(result.Stop < 95m,
            $"Long global SL ({result.Stop}) must be below DCA (95)");
    }

    [TestMethod]
    public void GlobalSl_Short_NeverBetweenEntryAndDca()
    {
        var input = ShortBase() with
        {
            SlPercentage = null,
            ExtremeDcaPrice = 105m,
            GlobalStopLossPercentage = 1.5m,
            GlobalStopLossLimitPercentage = 2.5m,
        };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Global, result.Source);
        // stop = 105 + 105*0.015 = 106.575
        Assert.AreEqual(106.575m, result.Stop);
        Assert.IsTrue(result.Stop > 105m,
            $"Short global SL ({result.Stop}) must be above DCA (105)");
    }

    /// <summary>
    /// Multiple DCA levels: 98.5, 97, 95 — ExtremeDcaPrice is the most extreme (95).
    /// SL must always be beyond the deepest DCA, regardless of how many levels exist.
    /// </summary>
    [TestMethod]
    public void SignalSl_Long_BeyondDeepestDcaLevel()
    {
        var input = LongBase() with
        {
            SlPercentage = 3m,
            ExtremeDcaPrice = 95m,
        };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
        // 3% from 95 → stop = 95 - 2.85 = 92.15
        Assert.AreEqual(92.15m, result.Stop);
        Assert.IsTrue(result.Stop < 95m,
            $"Long SL ({result.Stop}) must be below the deepest DCA level (95)");
    }

    [TestMethod]
    public void SignalSl_AnchorsOnDcaEvenWithExtremeDcaPrice()
    {
        var input = LongBase() with { ExtremeDcaPrice = 95m };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
        // 3% from 95 → stop = 92.15
        Assert.AreEqual(92.15m, result.Stop);
        Assert.IsTrue(result.Stop < 95m,
            "Signal SL must be below DCA even when DCA orders are pending");
    }

    /// <summary>
    /// Signal SL always takes precedence. With both signal and global available, signal wins.
    /// The anchor still moves to ExtremeDcaPrice so the SL is beyond all DCA levels.
    /// </summary>
    [TestMethod]
    public void SignalSl_AlwaysPreferredOverGlobal()
    {
        var input = LongBase() with
        {
            SlPercentage = 2m,
            ExtremeDcaPrice = 95m,
            GlobalStopLossPercentage = 2m,
        };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
        // Both must be below the DCA at 95
        Assert.IsTrue(result.Stop < 95m,
            $"SL ({result.Stop}) must be below DCA (95)");
    }
}
