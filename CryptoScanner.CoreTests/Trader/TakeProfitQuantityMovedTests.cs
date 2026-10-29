using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Tests for PositionMonitor.TakeProfitQuantityMoved, the question the take profit orders are
/// replaced on for their quantity: does the order still hold what would be placed now? On spot the
/// position quantity carries the fee of the buy in the coin itself and has more decimals than the
/// quantity step, while the order was placed rounded down onto that step.
/// </summary>
[TestClass]
public class TakeProfitQuantityMovedTests
{
    private const decimal Step = 0.00000001m;

    /// <summary>
    /// ETHEUR.SPOT on Bitvavo, 25-10-2026: 0.0078024 bought with a 0.25% fee leaves 0.0077828940,
    /// the order was placed for 0.00778289 and was cancelled and placed again every hour.
    /// </summary>
    [TestMethod]
    public void SpotDustBelowTheStepIsNotAChange()
    {
        Assert.IsFalse(PositionMonitor.TakeProfitQuantityMoved(0.00778289m, 0.0077828940m, CryptoTradingType.Spot, Step));
    }

    [TestMethod]
    public void SpotRealChangeStillCounts()
    {
        // A DCA filled: the position grew, the order did not
        Assert.IsTrue(PositionMonitor.TakeProfitQuantityMoved(0.00778289m, 0.0235856481m, CryptoTradingType.Spot, Step));
        // One whole step less is a change too
        Assert.IsTrue(PositionMonitor.TakeProfitQuantityMoved(0.00778289m, 0.00778288m, CryptoTradingType.Spot, Step));
    }

    /// <summary>
    /// Why the comparison rounds onto the step only and leaves the minimum alone: a minimum that is
    /// filled in wrongly (10 ETH here) turns a Clamp of the same 0.0077828940 into 10.
    /// </summary>
    [TestMethod]
    public void AWrongMinimumWouldLiftAClampToAnAbsurdQuantity()
    {
        Assert.AreEqual(10m, 0.0077828940m.Clamp(10m, 0m, Step));
        Assert.IsFalse(PositionMonitor.TakeProfitQuantityMoved(0.00778289m, 0.0077828940m, CryptoTradingType.Spot, Step));
    }

    [TestMethod]
    public void WithoutAStepTheComparisonIsExact()
    {
        Assert.IsFalse(PositionMonitor.TakeProfitQuantityMoved(0.0077828940m, 0.0077828940m, CryptoTradingType.Spot, 0m));
        Assert.IsTrue(PositionMonitor.TakeProfitQuantityMoved(0.00778289m, 0.0077828940m, CryptoTradingType.Spot, 0m));
    }

    /// <summary>
    /// Perpetual places the quantity as it is, so there the comparison stays exact.
    /// </summary>
    [TestMethod]
    public void PerpetualComparesExactly()
    {
        Assert.IsFalse(PositionMonitor.TakeProfitQuantityMoved(12.5m, 12.5m, CryptoTradingType.Perpetual, 0.1m));
        Assert.IsTrue(PositionMonitor.TakeProfitQuantityMoved(12.5m, 12.54m, CryptoTradingType.Perpetual, 0.1m));
    }
}
