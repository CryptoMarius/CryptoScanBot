using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Tests for <see cref="ProfitLockCalculator"/>: the trigger level, the fixed placement and the
/// trailing stop. The one rule that matters most is the ratchet - a trailing stop that can move
/// back towards the entry is not a trailing stop, it is a way to give profit away twice.
/// </summary>
[TestClass]
public class ProfitLockCalculatorTests
{
    // ── Trigger ─────────────────────────────────────────────────────────────

    [TestMethod]
    public void TriggerPrice_Long_SitsAboveBreakEven()
        => Assert.AreEqual(103m, ProfitLockCalculator.TriggerPrice(CryptoTradeSide.Long, 100m, 3m));

    [TestMethod]
    public void TriggerPrice_Short_SitsBelowBreakEven()
        => Assert.AreEqual(97m, ProfitLockCalculator.TriggerPrice(CryptoTradeSide.Short, 100m, 3m));

    [TestMethod]
    public void ProfitPercentage_Short_IsPositiveWhenPriceFalls()
        => Assert.AreEqual(3m, ProfitLockCalculator.ProfitPercentage(CryptoTradeSide.Short, 100m, 97m));

    [TestMethod]
    public void ProfitPercentage_WithoutBreakEven_IsZero()
        => Assert.AreEqual(0m, ProfitLockCalculator.ProfitPercentage(CryptoTradeSide.Long, 0m, 120m));

    // ── Fixed placement ─────────────────────────────────────────────────────

    [TestMethod]
    public void FixedStop_Long_UsesTheSlPercentage()
        => Assert.AreEqual(101.5m, ProfitLockCalculator.FixedStop(CryptoTradeSide.Long, 100m, 3m, 1.5m));

    [TestMethod]
    public void FixedStop_Short_UsesTheSlPercentage()
        => Assert.AreEqual(98.5m, ProfitLockCalculator.FixedStop(CryptoTradeSide.Short, 100m, 3m, 1.5m));

    [TestMethod]
    public void FixedStop_SlPercentageAboveTrigger_IsCappedToTheTrigger()
    {
        // A stop beyond the level that just armed the lock would fill on the spot
        Assert.AreEqual(103m, ProfitLockCalculator.FixedStop(CryptoTradeSide.Long, 100m, 3m, 5m));
        Assert.AreEqual(97m, ProfitLockCalculator.FixedStop(CryptoTradeSide.Short, 100m, 3m, 5m));
    }

    // ── Trailing ────────────────────────────────────────────────────────────

    [TestMethod]
    public void TrailingStop_Long_FirstCallSitsBelowThePrice()
    {
        // Trigger 3% on a break-even of 100 means the price is at 103 or higher; trailing 1.5%
        // behind 103 lands at 101.455, so the position is locked in profit from the first candle.
        decimal stop = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Long, 103m, 1.5m, 0m);
        Assert.AreEqual(101.455m, stop);
        Assert.IsTrue(stop > 100m, "de stop hoort boven break-even te liggen");
    }

    [TestMethod]
    public void TrailingStop_Short_FirstCallSitsAboveThePrice()
        => Assert.AreEqual(98.455m, ProfitLockCalculator.TrailingStop(CryptoTradeSide.Short, 97m, 1.5m, 0m));

    [TestMethod]
    public void TrailingStop_Long_FollowsANewHigh()
    {
        decimal stop = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Long, 103m, 1.5m, 0m);
        stop = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Long, 110m, 1.5m, stop);
        Assert.AreEqual(108.35m, stop);
    }

    [TestMethod]
    public void TrailingStop_Long_PullbackLeavesTheStopWhereItWas()
    {
        decimal stop = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Long, 110m, 1.5m, 0m);
        decimal afterPullback = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Long, 104m, 1.5m, stop);
        Assert.AreEqual(stop, afterPullback);
    }

    [TestMethod]
    public void TrailingStop_Short_PullbackLeavesTheStopWhereItWas()
    {
        decimal stop = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Short, 90m, 1.5m, 0m);
        decimal afterPullback = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Short, 96m, 1.5m, stop);
        Assert.AreEqual(stop, afterPullback);
    }

    [TestMethod]
    public void TrailingStop_Long_NeverMovesDownOverAWholeSequence()
    {
        decimal[] highs = [103m, 105m, 104m, 110m, 106m, 108m, 99m];
        decimal stop = 0m, previous = 0m;
        foreach (decimal high in highs)
        {
            stop = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Long, high, 1.5m, stop);
            Assert.IsTrue(stop >= previous, $"stop zakte van {previous} naar {stop} bij high {high}");
            previous = stop;
        }
        Assert.AreEqual(108.35m, stop, "de hoogste top van 110 bepaalt het eindniveau");
    }

    // ── Inverse, used for the trigger-price fence ───────────────────────────

    [TestMethod]
    public void PriceThatMovesTrailingStop_Long_IsTheInverseOfTheTrail()
    {
        decimal stop = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Long, 110m, 1.5m, 0m);
        decimal price = ProfitLockCalculator.PriceThatMovesTrailingStop(CryptoTradeSide.Long, stop, 1.5m);
        Assert.AreEqual(110m, Math.Round(price, 8));
    }

    [TestMethod]
    public void PriceThatMovesTrailingStop_Short_IsTheInverseOfTheTrail()
    {
        decimal stop = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Short, 90m, 1.5m, 0m);
        decimal price = ProfitLockCalculator.PriceThatMovesTrailingStop(CryptoTradeSide.Short, stop, 1.5m);
        Assert.AreEqual(90m, Math.Round(price, 8));
    }
}
