using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Trader;

using static CryptoScanner.Core.Trader.StopLossCalculator;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Unit tests for <see cref="StopLossCalculator"/>, the pure stop-loss price calculator
/// extracted from PositionMonitor.CalculateSlPrices.
///
/// Why these tests exist
/// ─────────────────────
/// In July 2026 a refactoring added an <c>!ActiveDca</c> guard to the signal-SL branch,
/// which silently changed the SL source from Signal to Global whenever a DCA was pending
/// but not yet filled. This caused emulator results to diverge drastically (run 640 = +84.50
/// vs run 18 = -14.16 with identical settings). The regression went undetected because the
/// SL logic had no test coverage.
///
/// These tests pin the expected behavior for each SL priority branch so that future
/// refactorings trigger an immediate, obvious test failure.
/// </summary>
[TestClass]
public class StopLossCalculatorTests
{
    // ─── Helpers ────────────────────────────────────────────────────────────

    private static SlInput LongBase() => new()
    {
        Side = CryptoTradeSide.Long,
        SlPercentage = 3m,       // 3% signal SL
        PartCount = 0,
        ActiveDca = false,
        SignalPrice = 100m,
        EntryPrice = 100m,
        ExtremeDcaPrice = null,
        GlobalStopLossPercentage = 5m,
        GlobalStopLossLimitPercentage = 6m,
    };

    private static SlInput ShortBase() => new()
    {
        Side = CryptoTradeSide.Short,
        SlPercentage = 3m,
        PartCount = 0,
        ActiveDca = false,
        SignalPrice = 100m,
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
    public void GlobalSl_UsedWhenDcaFilled()
    {
        // Once a DCA has filled (PartCount > 0), signal SL no longer applies
        var input = LongBase() with { PartCount = 1 };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Global, result.Source);
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
    //  REGRESSION: ActiveDca must NOT block signal SL
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Regression test for the July 2026 bug: when a DCA is pending (ActiveDca=true) but
    /// not yet filled (PartCount==0), the signal SL must still apply. The refactoring that
    /// added <c>!ActiveDca</c> to the guard caused the signal SL to be abandoned prematurely,
    /// falling back to the global SL. This changed the stop anchor from SignalPrice to
    /// EntryPrice and shifted the SL distance from the strategy-specific value to the global
    /// percentage — a drastic behavioral change that caused emulator profit to drop from
    /// +84.50 to -14.16.
    /// </summary>
    [TestMethod]
    public void Regression_ActiveDca_DoesNotBlockSignalSl()
    {
        var input = LongBase() with { ActiveDca = true, PartCount = 0 };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source,
            "Signal SL must remain active when ActiveDca=true but PartCount==0. " +
            "The DCA is pending (not filled), so the signal anchor is still valid.");
        Assert.AreEqual(97m, result.Stop);
    }

    /// <summary>
    /// Counterpart: once the DCA has actually filled (PartCount > 0), the signal SL must
    /// yield to the global SL because the average entry has shifted.
    /// </summary>
    [TestMethod]
    public void SignalSl_YieldsToGlobal_WhenDcaFilled()
    {
        var input = LongBase() with { ActiveDca = false, PartCount = 1 };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Global, result.Source,
            "Once a DCA has filled (PartCount > 0), the signal SL anchor is stale and " +
            "the global SL (anchored on DCA price) must take over.");
    }

    /// <summary>
    /// Verifies that DCA filled + ActiveDca=true (a second DCA pending after the first
    /// filled) also uses global SL.
    /// </summary>
    [TestMethod]
    public void GlobalSl_WhenDcaFilledAndAnotherPending()
    {
        var input = LongBase() with { ActiveDca = true, PartCount = 1 };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Global, result.Source);
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
            PartCount = 1,
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
            PartCount = 1,
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

    /// <summary>
    /// DCA pending (ActiveDca=true, PartCount=0) with DCA order placed at 95.
    /// Signal SL must anchor on the DCA price, not on SignalPrice.
    /// </summary>
    [TestMethod]
    public void Regression_ActiveDca_SignalSlAnchorsOnDca()
    {
        var input = LongBase() with
        {
            ActiveDca = true,
            PartCount = 0,
            ExtremeDcaPrice = 95m,
        };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
        // 3% from 95 → stop = 92.15
        Assert.AreEqual(92.15m, result.Stop);
        Assert.IsTrue(result.Stop < 95m,
            "Signal SL must be below DCA even when DCA is pending (not yet filled)");
    }

    /// <summary>
    /// Transition from signal to global SL after DCA fill: both must anchor on
    /// the extreme DCA price, so the SL does not jump closer to entry.
    /// </summary>
    [TestMethod]
    public void SlTransition_AfterDcaFill_StaysBeyondDca()
    {
        // Before DCA fill: signal SL
        var beforeFill = LongBase() with
        {
            PartCount = 0,
            SlPercentage = 2m,
            ExtremeDcaPrice = 95m,
            GlobalStopLossPercentage = 2m,
        };
        var resultBefore = Calculate(beforeFill);

        // After DCA fill: global SL takes over
        var afterFill = beforeFill with { PartCount = 1, SlPercentage = 2m };
        var resultAfter = Calculate(afterFill);

        // Both must be below the DCA at 95
        Assert.IsTrue(resultBefore.Stop < 95m,
            $"Pre-fill SL ({resultBefore.Stop}) must be below DCA (95)");
        Assert.IsTrue(resultAfter.Stop < 95m,
            $"Post-fill SL ({resultAfter.Stop}) must be below DCA (95)");
    }
}
