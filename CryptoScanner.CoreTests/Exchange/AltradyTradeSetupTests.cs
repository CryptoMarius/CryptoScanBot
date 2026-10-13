using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange.Altrady;
using CryptoScanner.Core.Settings;

using System.Text.Json.Nodes;

namespace CryptoScanner.CoreTests.Exchanges;

/// <summary>
/// The parts of the Altrady trade setup that are calculated here instead of by Altrady: the split of
/// the entry ladder, the take profit shares, the stop block and the limit entry price.
/// </summary>
[TestClass]
public class AltradyTradeSetupTests
{
    [TestMethod]
    public void SplitPercentages_EntryAndDcaFactors_AddUpToHundred()
    {
        // Entry counts as 100, a DCA with factor 200 is twice the entry
        List<decimal> split = AltradyTradeSetup.SplitPercentages([100m, 200m]);
        CollectionAssert.AreEqual(new List<decimal> { 33.33m, 66.67m }, split);
        Assert.AreEqual(100m, split.Sum());
    }

    [TestMethod]
    public void BuildTakeProfit_LastLevelTakesTheRest_AndTrails()
    {
        List<CryptoTpEntry> tpList =
        [
            new CryptoTpEntry { Percentage = 1m, Factor = 30m },
            new CryptoTpEntry { Percentage = 2m, Factor = 100m },
        ];
        JsonObject tp = AltradyTradeSetup.BuildTakeProfit(tpList, 0.5m);

        Assert.AreEqual("percentage", (string?)tp["exitPriceType"]);
        Assert.AreEqual(0.5m, (decimal)tp["trailingDistance"]!);
        JsonArray orders = tp["orders"]!.AsArray();
        Assert.AreEqual(30m, (decimal)orders[0]!["positionPercentage"]!);
        Assert.AreEqual(70m, (decimal)orders[1]!["positionPercentage"]!);
        Assert.AreEqual(2m, (decimal)orders[1]!["pricePercentage"]!);
    }

    [TestMethod]
    public void BuildStopLoss_TrailingProfitLock_BecomesPriceProtection()
    {
        JsonObject stop = AltradyTradeSetup.BuildStopLoss(5m, true, CryptoProfitLockMethod.TrailingPercentage, 2m, 1m, 1);
        Assert.AreEqual(5m, (decimal)stop["stopPercentage"]!);
        Assert.AreEqual("price", (string?)stop["protection"]);
        Assert.AreEqual(2m, (decimal)stop["trailingPercentage"]!);
        Assert.AreEqual(1m, (decimal)stop["trailingDistance"]!);
    }

    [TestMethod]
    public void BuildStopLoss_BreakEvenAfterTakeProfit_NeedsTwoTargets()
    {
        JsonObject one = AltradyTradeSetup.BuildStopLoss(5m, true, CryptoProfitLockMethod.FollowTakeProfit, 0m, 0m, 1);
        Assert.IsNull(one["protection"]);
        JsonObject two = AltradyTradeSetup.BuildStopLoss(5m, true, CryptoProfitLockMethod.FollowTakeProfit, 0m, 0m, 2);
        Assert.AreEqual("followTakeProfit", (string?)two["protection"]);
    }

    [TestMethod]
    public void LimitEntryPrice_NeverOnOrThroughTheMarket()
    {
        // Long anchored at the market: one tick below it, so it does not fill at once
        Assert.AreEqual(99.99m, AltradyTradeSetup.LimitEntryPrice(CryptoTradeSide.Long, 100m, 0m, 100m, 0.01m));
        // Short with an offset of 1%: above the anchor
        Assert.AreEqual(101m, AltradyTradeSetup.LimitEntryPrice(CryptoTradeSide.Short, 100m, 1m, 100m, 0.01m));
    }
}
