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
    // ─── coins ───────────────────────────────────────────────────────────────

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


    // ─── interval of the target gap (chart and TradingView) ──────────────────

    [TestMethod]
    public void TopIs4h_TellsTheIntervalOfTheGap()
    {
        List<FvgLadderZone> zones =
        [
            new(Top: 100m, Bottom: 98m, Is4h: true),
            new(Top: 90m, Bottom: 88m, Is4h: false),
        ];

        Assert.IsTrue(FvgLadderRules.TopIs4h(zones, 0.4m, 100m));
        Assert.IsFalse(FvgLadderRules.TopIs4h(zones, 0.4m, 90m));
    }

    [TestMethod]
    public void TopIs4h_TheSameTopOnBothIntervalsIs4h()
    {
        List<FvgLadderZone> zones =
        [
            new(Top: 100m, Bottom: 99m, Is4h: false),
            new(Top: 100m, Bottom: 98m, Is4h: true),
        ];

        Assert.IsTrue(FvgLadderRules.TopIs4h(zones, 0.4m, 100m));
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
        ], 0.4m);

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
        ], 0.4m);

        CollectionAssert.AreEqual(new List<decimal> { 101m, 98m }, tops);
    }


    // ─── 3.1 / 3.2 entry on the nearest gap top ─────────────────────────────

    private static readonly List<decimal> Tops = [105m, 99m, 98m, 97m, 96m, 95m];

    [TestMethod]
    public void Entry_GoesOnTheHighestTopUnderThePrice()
    {
        Assert.AreEqual(99m, FvgLadderRules.EntryTarget(Tops, 100m, 4, null));
    }

    [TestMethod]
    public void Entry_UnderTheLimitOfTheNextPositionRule()
    {
        Assert.AreEqual(97m, FvgLadderRules.EntryTarget(Tops, 100m, 4, 97.5m));
    }

    [TestMethod]
    public void Entry_OnlyTheGapsInViewTakePart()
    {
        // Under 95.5 only 95 is left, and that is the fifth top under the price
        Assert.IsNull(FvgLadderRules.EntryTarget(Tops, 100m, 4, 95.5m));
        Assert.AreEqual(95m, FvgLadderRules.EntryTarget(Tops, 100m, 5, 95.5m));
    }


    // ─── 3 entry only after a close above the gap on its own interval ───────

    [TestMethod]
    public void EntryTops_AGapWithoutACloseAboveIsNoEntry()
    {
        // HYPE 03-11-2026: the price crossed the 1h gap 77.256 - 78.347 in a few minute candles, the
        // last closed 1h candle (77.962) was still under its top
        List<FvgLadderZone> zones =
        [
            new FvgLadderZone(78.347m, 77.256m, false, false),
            new FvgLadderZone(75.237m, 74.967m, true, true),
        ];

        CollectionAssert.AreEqual(new List<decimal> { 75.237m }, FvgLadderRules.EntryTops(zones, 0.3m));
        Assert.AreEqual(75.237m, FvgLadderRules.EntryTarget(FvgLadderRules.EntryTops(zones, 0.3m), 78.403m, 4, null));
    }

    [TestMethod]
    public void EntryTops_TheDcaLadderStillSeesEveryGap()
    {
        List<FvgLadderZone> zones =
        [
            new FvgLadderZone(78.347m, 77.256m, false, false),
            new FvgLadderZone(75.237m, 74.967m, true, true),
        ];

        CollectionAssert.AreEqual(new List<decimal> { 78.347m, 75.237m }, FvgLadderRules.UsableTops(zones, 0.3m));
    }

    [TestMethod]
    public void EntryTops_AnUnconfirmed4hGapStillPushesAnOverlapping1hGapAside()
    {
        // The overlap rule is applied before the close: the 1h gap does not take the place of the 4h gap
        List<FvgLadderZone> zones =
        [
            new FvgLadderZone(101m, 100m, true, false),
            new FvgLadderZone(100.5m, 99.5m, false, true),
        ];

        Assert.AreEqual(0, FvgLadderRules.EntryTops(zones, 0.4m).Count);
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


    // ─── 6.1 / 6.2 a new position next to the open ones ─────────────────────

    [TestMethod]
    public void NewPosition_WithoutPositions_AnywhereGoes()
    {
        Assert.IsTrue(FvgLadderRules.NewTradeAllowed([], 0, true, 1.5m, out decimal? limit));
        Assert.IsNull(limit);
    }

    [TestMethod]
    public void NewPosition_TheMaximumPerCoinStopsIt()
    {
        List<FvgLadderTrade> one = [new FvgLadderTrade(100m, 97.0225m, DcasDone: true)];

        // The default is one position per coin
        Assert.IsFalse(FvgLadderRules.NewTradeAllowed(one, 1, true, 1.5m, out _));
        Assert.IsTrue(FvgLadderRules.NewTradeAllowed(one, 2, true, 1.5m, out _));
        Assert.IsTrue(FvgLadderRules.NewTradeAllowed(one, 0, true, 1.5m, out _), "0 = no limit");
    }

    [TestMethod]
    public void NewPosition_NotWhileTheLowestPositionStillHasDcasToGo()
    {
        // The lowest is the one with the lowest LAST buy: 95, and its DCA's are not done
        List<FvgLadderTrade> trades =
        [
            new FvgLadderTrade(100m, 97.0225m, DcasDone: true),
            new FvgLadderTrade(95m, 95m, DcasDone: false),
        ];

        Assert.IsFalse(FvgLadderRules.NewTradeAllowed(trades, 0, true, 1.5m, out _));
    }

    [TestMethod]
    public void NewPosition_OnceTheDcasAreDone_OnlyAStepUnderTheLowestLastBuy()
    {
        List<FvgLadderTrade> trades = [new FvgLadderTrade(100m, 97.0225m, DcasDone: true)];

        Assert.IsTrue(FvgLadderRules.NewTradeAllowed(trades, 0, true, 1.5m, out decimal? limit));
        Assert.AreEqual(95.5671625m, limit);  // 97.0225 x 0.985

        // A gap top just above that limit is not taken (never inside the range), the next one under it is
        Assert.AreEqual(95m, FvgLadderRules.EntryTarget([96m, 95.6m, 95m], 97m, 4, limit));
    }

    [TestMethod]
    public void NewPosition_WithoutDca_TheSameRuleUnderTheLowestPosition()
    {
        // Without DCA a position is one buy: there are no DCA's to wait for, the step still holds
        List<FvgLadderTrade> trades = [new FvgLadderTrade(100m, 100m, DcasDone: false)];

        Assert.IsTrue(FvgLadderRules.NewTradeAllowed(trades, 0, false, 1.5m, out decimal? limit));
        Assert.AreEqual(98.5m, limit);
    }


    // ─── 1 trend ─────────────────────────────────────────────────────────────

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
        Assert.AreEqual(0.4m, settings.MinimumGapPercentage);
        Assert.AreEqual(4, settings.GapsInView);
        Assert.AreEqual(5.0m, settings.PlaceOrderWithinPercentage);
        Assert.IsTrue(settings.UseDca);
        Assert.AreEqual(2, settings.MaxDca);
        Assert.AreEqual(1.5m, settings.DcaStepPercentage);
        Assert.AreEqual(3.0m, settings.DcaFvgMaxPercentage);
        Assert.AreEqual(2.0m, settings.DcaFactor);
        Assert.AreEqual(1.0m, settings.TakeProfitPercentage);
        CollectionAssert.AreEqual(new[] { "1h" }, settings.IntervalList);
        CollectionAssert.AreEqual(new[] { "fvg" }, settings.RequiredZoneKinds.ToArray());
    }

    [TestMethod]
    public void Settings_TheAvaloniaTabSurvivesALoadAndSaveRoundTrip()
    {
        var model = new StrategyFvgLadderSettingsViewModel();
        model.LoadConfig(new FvgLadderSettings
        {
            Coins = "BTC, ETH",
            MinimumGapPercentage = 0.5m,
            GapsInView = 3,
            PlaceOrderWithinPercentage = 4m,
            UseDca = false,
            MaxDca = 3,
            DcaStepPercentage = 2m,
            DcaFvgMaxPercentage = 4m,
            DcaFactor = 1.5m,
            TakeProfitPercentage = 1.5m,
        });

        var settings = new FvgLadderSettings();
        model.SaveConfig(settings);

        Assert.AreEqual("BTC, ETH", settings.Coins);
        Assert.AreEqual(0.5m, settings.MinimumGapPercentage);
        Assert.AreEqual(3, settings.GapsInView);
        Assert.AreEqual(4m, settings.PlaceOrderWithinPercentage);
        Assert.IsFalse(settings.UseDca);
        Assert.AreEqual(3, settings.MaxDca);
        Assert.AreEqual(2m, settings.DcaStepPercentage);
        Assert.AreEqual(4m, settings.DcaFvgMaxPercentage);
        Assert.AreEqual(1.5m, settings.DcaFactor);
        Assert.AreEqual(1.5m, settings.TakeProfitPercentage);
    }
}
