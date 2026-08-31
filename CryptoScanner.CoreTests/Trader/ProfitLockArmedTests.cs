using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Tests for PositionMonitor.ProfitLockArmed, the moment the stop loss is allowed to move to
/// break-even. The whole candle has to be past the trigger price: the low for a long, the high for
/// a short. A candle that only wicks through the trigger and pulls back leaves the stop where it is.
/// </summary>
[TestClass]
public class ProfitLockArmedTests
{
    // Break-even 100, trigger 2% => long arms at 102, short arms at 98.
    private const decimal BreakEven = 100m;
    private const decimal Trigger = 2m;

    private static bool ArmedLong(decimal low, decimal high)
        => PositionMonitor.ProfitLockArmed(CryptoTradeSide.Long, BreakEven, Trigger, low, high, out _);

    private static bool ArmedShort(decimal low, decimal high)
        => PositionMonitor.ProfitLockArmed(CryptoTradeSide.Short, BreakEven, Trigger, low, high, out _);


    // ── Long ────────────────────────────────────────────────────────────────

    [TestMethod]
    public void Long_WholeCandleAboveTrigger_Arms()
    {
        Assert.IsTrue(ArmedLong(low: 102.5m, high: 104m));
    }

    [TestMethod]
    public void Long_LowExactlyOnTrigger_Arms()
    {
        Assert.IsTrue(ArmedLong(low: 102m, high: 103m));
    }

    /// <summary>
    /// The case this rule exists for: the high runs 3% into profit but the candle closes back at
    /// break-even, so the position was never really 2% in profit.
    /// </summary>
    [TestMethod]
    public void Long_OnlyTheWickThroughTheTrigger_DoesNotArm()
    {
        Assert.IsFalse(ArmedLong(low: 99.5m, high: 103m));
    }

    [TestMethod]
    public void Long_LowJustUnderTheTrigger_DoesNotArm()
    {
        Assert.IsFalse(ArmedLong(low: 101.99m, high: 105m));
    }

    [TestMethod]
    public void Long_WholeCandleBelowBreakEven_DoesNotArm()
    {
        Assert.IsFalse(ArmedLong(low: 96m, high: 99m));
    }


    // ── Short ───────────────────────────────────────────────────────────────

    [TestMethod]
    public void Short_WholeCandleBelowTrigger_Arms()
    {
        Assert.IsTrue(ArmedShort(low: 96m, high: 97.5m));
    }

    [TestMethod]
    public void Short_HighExactlyOnTrigger_Arms()
    {
        Assert.IsTrue(ArmedShort(low: 97m, high: 98m));
    }

    [TestMethod]
    public void Short_OnlyTheWickThroughTheTrigger_DoesNotArm()
    {
        Assert.IsFalse(ArmedShort(low: 97m, high: 100.5m));
    }

    [TestMethod]
    public void Short_HighJustAboveTheTrigger_DoesNotArm()
    {
        Assert.IsFalse(ArmedShort(low: 95m, high: 98.01m));
    }


    // ── Reported profit percentage and guards ───────────────────────────────

    [TestMethod]
    public void ProfitPercentageIsMeasuredOnTheClosestSideOfTheCandle()
    {
        PositionMonitor.ProfitLockArmed(CryptoTradeSide.Long, BreakEven, Trigger, 103m, 108m, out decimal longProfit);
        Assert.AreEqual(3m, longProfit);

        PositionMonitor.ProfitLockArmed(CryptoTradeSide.Short, BreakEven, Trigger, 92m, 97m, out decimal shortProfit);
        Assert.AreEqual(3m, shortProfit);
    }

    [TestMethod]
    public void WithoutABreakEvenPriceNothingArms()
    {
        Assert.IsFalse(PositionMonitor.ProfitLockArmed(CryptoTradeSide.Long, 0m, Trigger, 102m, 104m, out decimal profit));
        Assert.AreEqual(0m, profit);
    }
}
