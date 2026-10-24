using CryptoScanner.Analyzers.FvgLadder;
using CryptoScanner.Analyzers.FvgLadder.Config;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Analyzer.FvgLadder;

/// <summary>
/// The rules of "BTC FVG DCA - Regels ter afstemming" one by one (FvgLadderRules), with the numbers
/// worked out by hand so a change in a rule shows up as a changed number.
/// </summary>
[TestClass]
public class FvgLadderRulesTests
{
    // ─── 0 coins ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void Coins_MatchOnTheBaseAsset()
    {
        FvgLadderSettings settings = new() { Coins = "BTC, eth" };

        Assert.IsTrue(settings.TradesCoin("BTC"));
        Assert.IsTrue(settings.TradesCoin("ETH"), "case does not matter");
        Assert.IsFalse(settings.TradesCoin("SOL"));
    }

    [TestMethod]
    public void Coins_TheDefaultIsBitcoinOnly()
    {
        FvgLadderSettings settings = new();

        Assert.IsTrue(settings.TradesCoin("BTC"));
        Assert.IsFalse(settings.TradesCoin("ETH"));
    }

    [TestMethod]
    public void Coins_AnEmptyListTradesEveryCoin()
    {
        FvgLadderSettings settings = new() { Coins = " " };

        Assert.IsTrue(settings.TradesCoin("SOL"));
    }


    // ─── 2.3 minimum gap ─────────────────────────────────────────────────────

    [TestMethod]
    public void MinimumGap_ExactlyFourTenthsCounts()
    {
        // (100.4 - 100) / 100 = 0.4%
        Assert.IsTrue(FvgLadderRules.IsWideEnough(100.4m, 100m, 0.4m));
    }

    [TestMethod]
    public void MinimumGap_JustUnderFourTenthsDoesNot()
    {
        Assert.IsFalse(FvgLadderRules.IsWideEnough(100.399m, 100m, 0.4m));
    }

    [TestMethod]
    public void MinimumGap_IsMeasuredOnTheBottom()
    {
        // 100 / 99.6 - 1 = 0.4016%: counts, although 0.4% of the top would be exactly 99.6
        Assert.IsTrue(FvgLadderRules.IsWideEnough(100m, 99.6m, 0.4m));
    }

    [TestMethod]
    public void MinimumGap_AScannerZoneOfAQuarterPercentIsDropped()
    {
        // The scanner keeps gaps from 0.25%; the strategy only takes those of 0.4% and more
        List<decimal> tops = FvgLadderRules.UsableTops(
        [
            new FvgLadderZone(100.25m, 100m, false),  // 0.25%
            new FvgLadderZone(95.5m, 95m, false),     // 0.53%
        ], 0.4m, use4h: true);

        CollectionAssert.AreEqual(new List<decimal> { 95.5m }, tops);
    }


    // ─── 2.5 overlap: the 4h gap wins ───────────────────────────────────────

    [TestMethod]
    public void Overlap_A1hGapTouchingA4hGapGivesWay()
    {
        List<decimal> tops = FvgLadderRules.UsableTops(
        [
            new FvgLadderZone(101m, 100m, true),     // 4h
            new FvgLadderZone(100.5m, 99.5m, false), // 1h, overlaps the 4h gap
            new FvgLadderZone(98m, 97m, false),      // 1h, on its own
        ], 0.4m, use4h: true);

        CollectionAssert.AreEqual(new List<decimal> { 101m, 98m }, tops);
    }

    [TestMethod]
    public void Overlap_WithoutThe4hGaps_The1hGapStays()
    {
        // The Grid preset reads the 1h gaps only, so nothing overrules them
        List<decimal> tops = FvgLadderRules.UsableTops(
        [
            new FvgLadderZone(101m, 100m, true),
            new FvgLadderZone(100.5m, 99.5m, false),
        ], 0.4m, use4h: false);

        CollectionAssert.AreEqual(new List<decimal> { 100.5m }, tops);
    }


    // ─── 3.1 / 3.2 entry on the nearest gap top ─────────────────────────────

    private static readonly List<decimal> Tops = [105m, 99m, 98m, 97m, 96m, 95m];

    [TestMethod]
    public void Entry_GoesOnTheHighestTopUnderThePrice()
    {
        Assert.AreEqual(99m, FvgLadderRules.EntryTarget(Tops, 100m, 4, null));
    }

    [TestMethod]
    public void Entry_UnderTheLimitOfTheParallelRule()
    {
        Assert.AreEqual(97m, FvgLadderRules.EntryTarget(Tops, 100m, 4, 97.5m));
    }

    [TestMethod]
    public void Entry_OnlyTheFourNearestTopsCarryAnOrder()
    {
        // Under 95.5 only 95 is left, and that is the fifth top under the price
        Assert.IsNull(FvgLadderRules.EntryTarget(Tops, 100m, 4, 95.5m));
        Assert.AreEqual(95m, FvgLadderRules.EntryTarget(Tops, 100m, 5, 95.5m));
    }


    // ─── 4.1 - 4.4 DCA levels ────────────────────────────────────────────────

    [TestMethod]
    public void Dca_WithoutGaps_StepsOneAndAHalfPercentUnderThePreviousBuy()
    {
        List<FvgLadderLevel> levels = FvgLadderRules.DcaLevels(100m, [], 2, 1.5m, 3m, 2m);

        Assert.AreEqual(2, levels.Count);
        Assert.AreEqual(98.5m, levels[0].Price);
        Assert.AreEqual(97.0225m, levels[1].Price);  // 98.5 x 0.985
    }

    [TestMethod]
    public void Dca_SizesDoubleWithFactorTwo()
    {
        List<FvgLadderLevel> levels = FvgLadderRules.DcaLevels(100m, [], 2, 1.5m, 3m, 2m);

        // 100 / 200 / 400: the factor is a percentage of the entry
        Assert.AreEqual(200m, levels[0].Factor);
        Assert.AreEqual(400m, levels[1].Factor);
    }

    [TestMethod]
    public void Dca_FactorOneAndAHalf()
    {
        List<FvgLadderLevel> levels = FvgLadderRules.DcaLevels(100m, [], 2, 1.5m, 3m, 1.5m);

        Assert.AreEqual(150m, levels[0].Factor);
        Assert.AreEqual(225m, levels[1].Factor);
    }

    [TestMethod]
    public void Dca_SnapsToTheHighestGapTopBetweenOneAndAHalfAndThreePercent()
    {
        // 98.0 and 97.2 lie in [97.0, 98.5]; the highest one takes the DCA. The second DCA is then
        // measured from 98.0: band [95.06, 96.53], where 95.5 lies.
        List<FvgLadderLevel> levels = FvgLadderRules.DcaLevels(100m, [99m, 98m, 97.2m, 95.5m], 2, 1.5m, 3m, 2m);

        Assert.AreEqual(98m, levels[0].Price);
        Assert.AreEqual(95.5m, levels[1].Price);
    }

    [TestMethod]
    public void Dca_AGapTopFurtherThanThreePercentIsNotUsed()
    {
        // 96.9 is 3.1% under the entry: the DCA stays on the step
        List<FvgLadderLevel> levels = FvgLadderRules.DcaLevels(100m, [96.9m], 1, 1.5m, 3m, 2m);

        Assert.AreEqual(98.5m, levels[0].Price);
    }

    [TestMethod]
    public void Dca_ThreeDcasWhenAsked()
    {
        List<FvgLadderLevel> levels = FvgLadderRules.DcaLevels(100m, [], 3, 1.5m, 3m, 2m);

        Assert.AreEqual(3, levels.Count);
        Assert.AreEqual(800m, levels[2].Factor);
    }

    [TestMethod]
    public void Dca_TheLadderIsHandedOverAsPercentagesUnderTheEntry()
    {
        List<CryptoDcaEntry> ladder = FvgLadderRules.ToDcaList(100m, FvgLadderRules.DcaLevels(100m, [], 2, 1.5m, 3m, 2m));

        Assert.AreEqual(1.5m, ladder[0].Percentage);
        Assert.AreEqual(2.9775m, ladder[1].Percentage);
        Assert.AreEqual(200m, ladder[0].Factor);

        // And the trader puts them back on the same prices (PricePlacement from the entry)
        Assert.AreEqual(98.5m, PricePlacement.Adverse(CryptoTradeSide.Long, 100m, ladder[0].Percentage));
        Assert.AreEqual(97.0225m, PricePlacement.Adverse(CryptoTradeSide.Long, 100m, ladder[1].Percentage));
    }


    // ─── 5.1 take profit ─────────────────────────────────────────────────────

    [TestMethod]
    public void TakeProfit_GridOnTheEntry_LandsOnEntryPlusOnePercent()
    {
        // The trader measures from entry x (1 + 2 x fee) (TradeTools.DcaGridAnchor); with a fee of
        // 0.1% that is 100.2. The handed over percentage has to bring that back to 101.
        decimal percentage = FvgLadderRules.TakeProfitFromBreakEven(1m, 0.1m);
        decimal target = PricePlacement.Favorable(CryptoTradeSide.Long, 100m * (1m + 2m * 0.1m / 100m), percentage);

        Assert.AreEqual(101m, Math.Round(target, 10));
    }


    // ─── 6.1 / 6.2 parallel positions ───────────────────────────────────────

    [TestMethod]
    public void Parallel_WithoutPositions_AnythingGoes()
    {
        Assert.IsTrue(FvgLadderRules.NewTradeAllowed([], 1.5m, out decimal? limit));
        Assert.IsNull(limit);
    }

    [TestMethod]
    public void Parallel_NotWhileTheLowestPositionStillHasDcasToGo()
    {
        // The lowest is the one with the lowest LAST buy: 95, and its DCA's are not done
        List<FvgLadderTrade> trades =
        [
            new FvgLadderTrade(100m, 97.0225m, DcasDone: true),
            new FvgLadderTrade(95m, 95m, DcasDone: false),
        ];

        Assert.IsFalse(FvgLadderRules.NewTradeAllowed(trades, 1.5m, out _));
    }

    [TestMethod]
    public void Parallel_OnceTheDcasAreDone_OneAndAHalfPercentUnderTheLastBuy()
    {
        List<FvgLadderTrade> trades = [new FvgLadderTrade(100m, 97.0225m, DcasDone: true)];

        Assert.IsTrue(FvgLadderRules.NewTradeAllowed(trades, 1.5m, out decimal? limit));
        Assert.AreEqual(95.5671625m, limit);  // 97.0225 x 0.985

        // A gap top just above that limit is not taken, the next one under it is
        Assert.AreEqual(95m, FvgLadderRules.EntryTarget([96m, 95.6m, 95m], 97m, 4, limit));
    }

    [TestMethod]
    public void Grid_EveryPositionAtLeastTheStepFromEveryOther()
    {
        List<FvgLadderTrade> open = [new FvgLadderTrade(100m, 100m, DcasDone: true)];

        Assert.IsFalse(FvgLadderRules.FarEnoughFromAll(98.6m, open, 1.5m));  // 1.4%
        Assert.IsTrue(FvgLadderRules.FarEnoughFromAll(98.5m, open, 1.5m));   // 1.5%
        Assert.IsTrue(FvgLadderRules.FarEnoughFromAll(101.5m, open, 1.5m));  // above counts too

        // 99.5 and 99 are too close to the open position at 100, so the entry goes on 98.5
        Assert.AreEqual(98.5m, FvgLadderRules.GridEntryTarget([99.5m, 99m, 98.5m, 97m], 100m, 4, open, 1.5m));
    }


    // ─── 1.1 trend ───────────────────────────────────────────────────────────

    [TestMethod]
    public void Ema_StartsOnTheFirstClose()
    {
        // span 3: weight 0.5, so 1, 2, 3 gives 1, 1.5, 2.25
        Assert.AreEqual(2.25m, FvgLadderRules.Ema([1m, 2m, 3m], 3));
        Assert.AreEqual(2.25m, FvgLadderRules.NextEma(1.5m, 3m, 3));
    }


    // ─── settings ────────────────────────────────────────────────────────────

    [TestMethod]
    public void Settings_DefaultsAreTheAgreedRules()
    {
        FvgLadderSettings settings = new();

        Assert.AreEqual("BTC", settings.Coins);
        Assert.AreEqual(FvgLadderPreset.Ladder, settings.Preset);
        Assert.AreEqual(0.4m, settings.MinimumGapPercentage);
        Assert.AreEqual(1.5m, settings.StepPercentage);
        Assert.AreEqual(1.0m, settings.TakeProfitPercentage);
        Assert.AreEqual(4, settings.OrderZones);
        Assert.AreEqual(2, settings.MaxDca);
        Assert.AreEqual(3.0m, settings.DcaFvgMaxPercentage);
        Assert.AreEqual(2.0m, settings.DcaFactor);
        CollectionAssert.AreEqual(new[] { "fvg" }, settings.RequiredZoneKinds.ToArray());
    }

    [TestMethod]
    public void Settings_TheAvaloniaTabSurvivesALoadAndSaveRoundTrip()
    {
        var model = new StrategyFvgLadderSettingsViewModel();
        model.LoadConfig(new FvgLadderSettings
        {
            Coins = "BTC, ETH",
            Preset = FvgLadderPreset.Grid,
            MinimumGapPercentage = 0.5m,
            StepPercentage = 2m,
            TakeProfitPercentage = 1.5m,
            OrderZones = 3,
            PlaceOrderDistancePercentage = 0.8m,
            WithdrawOrderDistancePercentage = 2.5m,
            MaxDca = 3,
            DcaFvgMaxPercentage = 4m,
            DcaFactor = 1.5m,
        });

        var settings = new FvgLadderSettings();
        model.SaveConfig(settings);

        Assert.AreEqual("BTC, ETH", settings.Coins);
        Assert.AreEqual(FvgLadderPreset.Grid, settings.Preset);
        Assert.AreEqual(0.5m, settings.MinimumGapPercentage);
        Assert.AreEqual(2m, settings.StepPercentage);
        Assert.AreEqual(1.5m, settings.TakeProfitPercentage);
        Assert.AreEqual(3, settings.OrderZones);
        Assert.AreEqual(0.8m, settings.PlaceOrderDistancePercentage);
        Assert.AreEqual(2.5m, settings.WithdrawOrderDistancePercentage);
        Assert.AreEqual(3, settings.MaxDca);
        Assert.AreEqual(4m, settings.DcaFvgMaxPercentage);
        Assert.AreEqual(1.5m, settings.DcaFactor);
    }
}
