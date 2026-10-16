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
    public void SignalSl_Long_StopBelowEntryPrice()
    {
        var input = LongBase();
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
        // Long 3% SL from entry 100 → stop = 100 - 1*100*0.03 = 97
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
        // Signal SL is always preferred regardless of DCA state, anchored on entry
        var input = LongBase() with { ExtremeDcaPrice = 95m };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
        // Signal SL anchors on entry (100), not on DCA (95)
        Assert.AreEqual(97m, result.Stop, "Signal SL must anchor on entry, not DCA");
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
            DeepestDcaPercentage = 10m,
        };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Global, result.Source);
        // 5% from anchor 90 → stop = 90 - 90*0.05 = 85.5
        //Assert.AreEqual(85.5m, result.Stop);
        // Since 15-10-2026 added and measured from the entry, as Altrady does: 100 - (10 + 5)% = 85
        Assert.AreEqual(85m, result.Stop);
        Assert.AreEqual(84m, result.Limit, "the limit the same way: 100 - (10 + 6)%");
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
    public void SignalSl_Long_AnchorsOnEntryNotDca()
    {
        var input = LongBase() with { ExtremeDcaPrice = 95m };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
        // Signal SL anchors on entry (100), not DCA (95): stop = 100 - 100*0.03 = 97
        Assert.AreEqual(97m, result.Stop,
            "Signal SL must anchor on entry, ignoring ExtremeDcaPrice");
    }

    [TestMethod]
    public void SignalSl_Short_AnchorsOnEntryNotDca()
    {
        var input = ShortBase() with { ExtremeDcaPrice = 105m };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
        // Signal SL anchors on entry (100), not DCA (105): stop = 100 + 100*0.03 = 103
        Assert.AreEqual(103m, result.Stop,
            "Signal SL must anchor on entry, ignoring ExtremeDcaPrice");
    }

    /// <summary>
    /// Signal SL is tighter than the DCA distance. The caller must not place
    /// DCAs beyond the SL — they would never fill. The SL stays at the strategy
    /// percentage from entry, unchanged.
    /// </summary>
    [TestMethod]
    public void SignalSl_Long_TighterThanDca_StaysAtEntry()
    {
        var input = LongBase() with
        {
            SlPercentage = 1.5m,
            ExtremeDcaPrice = 95m,  // DCA beyond the 1.5% SL — should not exist in practice
        };
        var result = Calculate(input);

        // stop = 100 - 100*0.015 = 98.5 (from entry, not DCA)
        Assert.AreEqual(98.5m, result.Stop,
            "Signal SL anchors on entry even when DCA exists beyond it");
    }

    [TestMethod]
    public void SignalSl_Short_TighterThanDca_StaysAtEntry()
    {
        var input = ShortBase() with
        {
            SlPercentage = 1.5m,
            ExtremeDcaPrice = 105m,
        };
        var result = Calculate(input);

        // stop = 100 + 100*0.015 = 101.5
        Assert.AreEqual(101.5m, result.Stop,
            "Signal SL anchors on entry even when DCA exists beyond it");
    }

    [TestMethod]
    public void GlobalSl_Long_NeverBetweenEntryAndDca()
    {
        var input = LongBase() with
        {
            SlPercentage = null,
            ExtremeDcaPrice = 95m,
            DeepestDcaPercentage = 5m,
            GlobalStopLossPercentage = 1.5m,
            GlobalStopLossLimitPercentage = 2.5m,
        };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Global, result.Source);
        // stop = 95 - 95*0.015 = 93.575
        //Assert.AreEqual(93.575m, result.Stop);
        // Since 15-10-2026: 100 - (5 + 1.5)% = 93.5, as Altrady
        Assert.AreEqual(93.5m, result.Stop);
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
            DeepestDcaPercentage = 5m,
            GlobalStopLossPercentage = 1.5m,
            GlobalStopLossLimitPercentage = 2.5m,
        };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Global, result.Source);
        // stop = 105 + 105*0.015 = 106.575
        //Assert.AreEqual(106.575m, result.Stop);
        // Since 15-10-2026: 100 + (5 + 1.5)% = 106.5, the mirror image of the long above, as Altrady
        Assert.AreEqual(106.5m, result.Stop);
        Assert.IsTrue(result.Stop > 105m,
            $"Short global SL ({result.Stop}) must be above DCA (105)");
    }

    /// <summary>
    /// Signal SL anchors on entry, not on DCA. DCA filtering is the caller's
    /// responsibility (PositionMonitor / AltradyWebhook skip DCAs beyond the SL).
    /// </summary>
    [TestMethod]
    public void SignalSl_Long_AlwaysAnchorsOnEntry()
    {
        var input = LongBase() with
        {
            SlPercentage = 3m,
            ExtremeDcaPrice = 95m,
        };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
        // 3% from entry 100 → stop = 97
        Assert.AreEqual(97m, result.Stop);
    }

    /// <summary>
    /// Signal SL always takes precedence over global SL. Both use entry as anchor.
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
        // Signal SL: 2% from entry 100 → stop = 98
        Assert.AreEqual(98m, result.Stop);
    }


    // ═══════════════════════════════════════════════════════════════════════
    //  BRE-specific: band-width percentage as signal SL
    // ═══════════════════════════════════════════════════════════════════════

    /// <summary>
    /// BRE typically produces small band-width percentages (1-5%). Signal SL anchors on
    /// entry. DCAs beyond the SL are not placed (caller responsibility), so the SL is
    /// always correct at the strategy-computed distance from entry.
    /// </summary>
    [TestMethod]
    public void Bre_Long_SmallBandWidth_AnchorsOnEntry()
    {
        decimal entry = 150m;
        var input = LongBase() with
        {
            SlPercentage = 1.8m,
            EntryPrice = entry,
            ExtremeDcaPrice = null,  // DCAs beyond 1.8% are not placed
        };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
        // 1.8% from entry 150 → stop = 150 - 2.7 = 147.3
        decimal expected = entry - entry * 1.8m / 100m;
        Assert.AreEqual(expected, result.Stop);
    }

    [TestMethod]
    public void Bre_Short_SmallBandWidth_AnchorsOnEntry()
    {
        decimal entry = 150m;
        var input = ShortBase() with
        {
            SlPercentage = 1.8m,
            EntryPrice = entry,
            ExtremeDcaPrice = null,
        };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Signal, result.Source);
        // 1.8% from entry 150 → stop = 150 + 2.7 = 152.7
        decimal expected = entry + entry * 1.8m / 100m;
        Assert.AreEqual(expected, result.Stop);
    }

    /// <summary>
    /// BRE with UseStopLoss=false: SlPercentage is null, falls back to global SL.
    /// Global SL anchors on ExtremeDcaPrice (all DCAs are placed when no signal SL).
    /// </summary>
    [TestMethod]
    public void Bre_Long_UseStopLossDisabled_FallsBackToGlobal()
    {
        decimal entry = 100m;
        decimal deepestDca = 95.5m;
        var input = LongBase() with
        {
            SlPercentage = null,  // BRE UseStopLoss=false
            EntryPrice = entry,
            ExtremeDcaPrice = deepestDca,
            DeepestDcaPercentage = 4.5m,
            GlobalStopLossPercentage = 5m,
        };
        var result = Calculate(input);

        Assert.AreEqual(SlSource.Global, result.Source);
        // Global SL anchors on DCA: 5% from 95.5 → stop = 95.5 - 4.775 = 90.725
        //Assert.AreEqual(95.5m - 95.5m * 5m / 100m, result.Stop);
        // Since 15-10-2026: added and measured from the entry, 100 - (4.5 + 5)% = 90.5
        Assert.AreEqual(90.5m, result.Stop);
        Assert.IsTrue(result.Stop < deepestDca,
            $"Global SL ({result.Stop}) must be below deepest DCA ({deepestDca})");
    }


    /// <summary>
    /// The global stop is the deepest DCA percentage plus the stop percentage, measured from the
    /// entry - the number the Altrady webhook sends as stop_loss_percentage. Long and short are the
    /// same distance from the entry (Altrady measured symmetric on 25-09-2026).
    /// </summary>
    [TestMethod]
    public void GlobalSl_IsTheSameDistanceAsAltrady_LongAndShort()
    {
        var longResult = Calculate(LongBase() with
        {
            SlPercentage = null, ExtremeDcaPrice = 90m, DeepestDcaPercentage = 10m,
            GlobalStopLossPercentage = 20m, GlobalStopLossLimitPercentage = 21m,
        });
        var shortResult = Calculate(ShortBase() with
        {
            SlPercentage = null, ExtremeDcaPrice = 110m, DeepestDcaPercentage = 10m,
            GlobalStopLossPercentage = 20m, GlobalStopLossLimitPercentage = 21m,
        });

        Assert.AreEqual(70m, longResult.Stop, "stacking gave 72.00");
        Assert.AreEqual(130m, shortResult.Stop, "stacking gave 132.00");
        Assert.AreEqual(69m, longResult.Limit);
        Assert.AreEqual(131m, shortResult.Limit);
    }

    [TestMethod]
    public void DeepestDcaPercentage_SkipsLevelsBeyondTheSignalStop()
    {
        List<CryptoScanner.Core.Settings.CryptoDcaEntry> dcaList =
        [
            new() { Percentage = 2m, Factor = 200m },
            new() { Percentage = 4m, Factor = 400m },
        ];

        Assert.AreEqual(4m, DeepestDcaPercentage(dcaList, null));
        Assert.AreEqual(2m, DeepestDcaPercentage(dcaList, 3m), "the 4% level lies beyond a 3% signal stop and is never placed");
        Assert.IsNull(DeepestDcaPercentage([], null));
    }
}
