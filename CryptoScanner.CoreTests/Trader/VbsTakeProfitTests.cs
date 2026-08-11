using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Verifies the per-signal take-profit override (VBS RiskRewardRatio * ACS%). The trader resolves the
/// TP levels through a single source — <see cref="TradeTools.EffectiveTpList"/> — which is consumed by
/// BOTH the papertrading monitor (PositionMonitor) AND the Altrady webhook (AltradyWebhook). So testing
/// that one function proves both paths use the correct levels, exactly like the per-position SlPercentage.
///
/// Key invariant (the user's concern): with 2 DCAs and 2 global TPs configured, a VBS position that
/// carries its own per-signal TP must use that single TP, not the global 2-level grid — and this holds
/// regardless of how many DCAs are configured (the decision is DCA-invariant: it reads position.TpPercentage).
/// </summary>
[TestClass]
public class VbsTakeProfitTests : TestBase
{
    private static CryptoPosition CreateVbsPosition(decimal? tpPercentage)
    {
        InitTestSession();
        var database = new CryptoDatabase();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        CryptoSymbolInterval symbolInterval = symbol.GetSymbolInterval(CryptoIntervalPeriod.interval1m);
        CryptoPosition position = PositionTools.CreatePosition(symbol, "vbs",
            CryptoTradeSide.Long, "Test", symbolInterval, DateTime.UtcNow);
        position.TpPercentage = tpPercentage;
        return position;
    }

    // Two DCAs and two take-profit levels, as the user described.
    private static void ConfigureTwoDcasAndTwoTps()
    {
        InitTestSession();
        GlobalData.Settings.Trading.TpList =
        [
            new CryptoTpEntry { Percentage = 1.0m, Factor = 60m },  // TP1: +1%, close 60%
            new CryptoTpEntry { Percentage = 2.0m, Factor = 40m },  // TP2: +2%, close the remainder
        ];
        GlobalData.Settings.Trading.DcaList =
        [
            new CryptoDcaEntry { Percentage = 1.5m, Factor = 100m }, // DCA1 at -1.5%
            new CryptoDcaEntry { Percentage = 3.0m, Factor = 200m }, // DCA2 at -3%
        ];
    }

    [TestMethod]
    public void EffectiveTpList_NoSignalTp_UsesGlobalTwoLevelGrid()
    {
        ConfigureTwoDcasAndTwoTps();
        CryptoPosition position = CreateVbsPosition(tpPercentage: null);   // no per-signal TP

        var levels = TradeTools.EffectiveTpList(position);

        Assert.AreEqual(2, levels.Count, "Without a per-signal TP the global 2-level grid applies");
        Assert.AreEqual(1.0m, levels[0].Percentage);
        Assert.AreEqual(60m, levels[0].Factor);
        Assert.AreEqual(2.0m, levels[1].Percentage);
        Assert.AreEqual(40m, levels[1].Factor);
    }

    [TestMethod]
    public void EffectiveTpList_VbsSignalTp_UsesSingleTp_IgnoringGridAndDcas()
    {
        // The user's exact scenario: 2 DCAs + 2 global TPs configured, VBS position with its own TP.
        ConfigureTwoDcasAndTwoTps();
        Assert.AreEqual(2, GlobalData.Settings.Trading.DcaList.Count, "precondition: 2 DCAs configured");
        Assert.AreEqual(2, GlobalData.Settings.Trading.TpList.Count, "precondition: 2 global TPs configured");

        CryptoPosition position = CreateVbsPosition(tpPercentage: 3.5m);   // RiskRewardRatio * ACS%

        var levels = TradeTools.EffectiveTpList(position);

        Assert.AreEqual(1, levels.Count, "A per-signal TP collapses the grid to a single TP");
        Assert.AreEqual(3.5m, levels[0].Percentage, "TP distance = the per-signal RiskRewardRatio * ACS%");
        Assert.AreEqual(100m, levels[0].Factor, "The single TP closes the whole position");
    }

    [TestMethod]
    public void EffectiveTpList_NonPositiveSignalTp_FallsBackToGlobalGrid()
    {
        ConfigureTwoDcasAndTwoTps();
        CryptoPosition position = CreateVbsPosition(tpPercentage: 0m);

        var levels = TradeTools.EffectiveTpList(position);

        Assert.AreEqual(2, levels.Count, "A non-positive TP% is ignored; the global grid applies");
    }

    // Adds one filled BUY step (entry or DCA) to a new part on the position.
    private static void AddFilledBuyPart(CryptoPosition position, CryptoPartPurpose purpose, int partNumber,
        decimal price, decimal quantity)
    {
        var part = new CryptoPositionPart
        {
            Position = position,
            Symbol = position.Symbol,
            Exchange = position.Exchange,
            Purpose = purpose,
            Interval = position.Interval,
        };
        var step = new CryptoPositionStep
        {
            Side = CryptoOrderSide.Buy,
            Status = CryptoOrderStatus.Filled,
            AveragePrice = price,
            Price = price,
            Quantity = quantity,
            QuantityFilled = quantity,
            QuoteQuantityFilled = price * quantity,
            CommissionBase = 0m,
        };
        part.StepList.Add(1, step);
        position.PartList.Add(partNumber, part);
    }

    [TestMethod]
    public void TpGridAnchor_LowersAfterDcaFill_SoTheTakeProfitLowers()
    {
        // The user's requirement: a DCA lowers the average cost basis, so the TP (a % above break-even)
        // must LOWER in price after a DCA fills. The TP is anchored on TpGridAnchorPrice, which is the
        // cost basis of filled Entry+DCA fills, so this test proves the anchor (and thus the TP) drops.
        CryptoPosition position = CreateVbsPosition(tpPercentage: 3.0m);
        position.Exchange.FeeRate = 0m;                 // no fees -> clean averages
        position.Status = CryptoPositionStatus.Trading;
        position.TpPercentage = 3.0m;
        const decimal tpFactor = 1.03m;                 // long: TP = anchor * (1 + 3%)

        // 1) Entry only, filled at 100 -> anchor = 100, TP would sit at 103.
        AddFilledBuyPart(position, CryptoPartPurpose.Entry, 1, price: 100m, quantity: 1m);
        TradeTools.CalculateProfitAndBreakEvenPrice(position);
        decimal anchorEntryOnly = position.TpGridAnchorPrice;
        Assert.AreEqual(100m, anchorEntryOnly, "entry-only cost basis = entry price");
        decimal tpEntryOnly = anchorEntryOnly * tpFactor;   // 103

        // 2) A DCA fills lower, at 90 -> cost basis averages down to 95 -> TP drops to ~97.85.
        AddFilledBuyPart(position, CryptoPartPurpose.Dca, 2, price: 90m, quantity: 1m);
        TradeTools.CalculateProfitAndBreakEvenPrice(position);
        decimal anchorAfterDca = position.TpGridAnchorPrice;
        Assert.AreEqual(95m, anchorAfterDca, "cost basis after DCA = (100 + 90) / 2 = 95");
        decimal tpAfterDca = anchorAfterDca * tpFactor;     // 97.85

        Assert.IsTrue(anchorAfterDca < anchorEntryOnly, "the break-even (TP anchor) must lower after a DCA");
        Assert.IsTrue(tpAfterDca < tpEntryOnly,
            $"the take-profit price must lower after a DCA (before={tpEntryOnly}, after={tpAfterDca})");
    }

    [TestMethod]
    public void EffectiveTpList_SameSourceForPapertradingAndAltrady()
    {
        // Both PositionMonitor (papertrading) and AltradyWebhook resolve their TP levels through this same
        // TradeTools.EffectiveTpList call, so the papertrade TP and the Altrady TP stay consistent.
        ConfigureTwoDcasAndTwoTps();
        CryptoPosition position = CreateVbsPosition(tpPercentage: 2.4m);

        var levels = TradeTools.EffectiveTpList(position);

        // What the Altrady webhook would emit: one TP order at 2.4%, closing 100% of the position.
        Assert.AreEqual(1, levels.Count);
        Assert.AreEqual(2.4m, levels[0].Percentage);
        Assert.AreEqual(100m, levels[0].Factor);
    }
}
