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


    // ── Take profit filled at its target or by the stop ────────────────────

    private static CryptoScanner.Core.Model.CryptoPositionStep Step(decimal price, decimal? stopPrice, decimal averagePrice)
        => new() { Price = price, StopPrice = stopPrice, AveragePrice = averagePrice };

    [TestMethod]
    public void TakeProfitStep_FilledAtTheTarget_CountsAsFilled()
        => Assert.IsTrue(PositionMonitor.IsFilledAtTarget(Step(105m, 95m, 105m)));

    [TestMethod]
    public void TakeProfitStep_FilledByTheStop_DoesNotCount()
    {
        // A stopped-out position closes its take profit parts too; that is no TP1 fill and must not
        // move the stop of the rest to break-even.
        Assert.IsFalse(PositionMonitor.IsFilledAtTarget(Step(105m, 95m, 95m)));
    }

    [TestMethod]
    public void TakeProfitStep_Short_FilledByTheStop_DoesNotCount()
        => Assert.IsFalse(PositionMonitor.IsFilledAtTarget(Step(95m, 105m, 105m)));

    [TestMethod]
    public void TakeProfitStep_WithoutAFill_DoesNotCount()
        => Assert.IsFalse(PositionMonitor.IsFilledAtTarget(Step(105m, 95m, 0m)));


    // ── Wake-up price with a trailing last take profit ─────────────────────

    private static List<(int Level, CryptoScanner.Core.Model.CryptoPositionPart Part, decimal Price, decimal Quantity)> Targets(params (int level, decimal price)[] items)
        => items.Select(i => (i.level, (CryptoScanner.Core.Model.CryptoPositionPart)null!, i.price, 1m)).ToList();

    [TestMethod]
    public void NearestTakeProfit_UsesTheArmingPriceInsteadOfTheParkedLimit()
    {
        // TP1 at 105 still open, the trailing TP2 is parked at 1240 and arms at 124
        var targets = Targets((0, 105m), (1, 1240m));
        var trail = new PositionMonitor.TakeProfitTrail(null, null, 124m, 124m);
        Assert.AreEqual(105m, PositionMonitor.NearestTakeProfitPrice(CryptoTradeSide.Long, targets, 1, trail));

        targets = Targets((1, 1240m));
        Assert.AreEqual(124m, PositionMonitor.NearestTakeProfitPrice(CryptoTradeSide.Long, targets, 1, trail));
    }

    [TestMethod]
    public void NearestTakeProfit_Short_UsesTheArmingPrice()
    {
        var targets = Targets((0, 9.5m));
        var trail = new PositionMonitor.TakeProfitTrail(null, null, 80m, 80m);
        Assert.AreEqual(80m, PositionMonitor.NearestTakeProfitPrice(CryptoTradeSide.Short, targets, 0, trail));
    }

    // ── A moved stop never beyond the price ─────────────────────────────────

    [TestMethod]
    public void Long_StopAboveThePrice_IsRefused()
    {
        // Position 1461 of run 2508: price 225.73, stop moved to 301.24 (the TP4 level)
        Assert.IsFalse(PositionMonitor.StopIsBehindThePrice(CryptoTradeSide.Long, 301.24m, 225.73m));
        Assert.IsTrue(PositionMonitor.StopIsBehindThePrice(CryptoTradeSide.Long, 215.17m, 225.73m));
    }

    [TestMethod]
    public void Short_StopBelowThePrice_IsRefused()
    {
        Assert.IsFalse(PositionMonitor.StopIsBehindThePrice(CryptoTradeSide.Short, 90m, 100m));
        Assert.IsTrue(PositionMonitor.StopIsBehindThePrice(CryptoTradeSide.Short, 110m, 100m));
    }
}
