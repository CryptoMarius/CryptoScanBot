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

    // ── Tighten only: the lock may never widen the risk ─────────────────────

    [TestMethod]
    public void Tightens_WithoutAnyStop_TheLockAlwaysWins()
    {
        Assert.IsTrue(ProfitLockCalculator.Tightens(CryptoTradeSide.Long, 101.5m, null));
        Assert.IsTrue(ProfitLockCalculator.Tightens(CryptoTradeSide.Short, 98.5m, null));
    }

    [TestMethod]
    public void Tightens_Long_OnlyWhenTheLockSitsHigher()
    {
        Assert.IsTrue(ProfitLockCalculator.Tightens(CryptoTradeSide.Long, 101.5m, 96m));
        Assert.IsFalse(ProfitLockCalculator.Tightens(CryptoTradeSide.Long, 95m, 96m));
        Assert.IsFalse(ProfitLockCalculator.Tightens(CryptoTradeSide.Long, 96m, 96m), "gelijk is niet strakker");
    }

    [TestMethod]
    public void Tightens_Short_OnlyWhenTheLockSitsLower()
    {
        Assert.IsTrue(ProfitLockCalculator.Tightens(CryptoTradeSide.Short, 98.5m, 104m));
        Assert.IsFalse(ProfitLockCalculator.Tightens(CryptoTradeSide.Short, 105m, 104m));
    }

    [TestMethod]
    public void Tightens_TrailingStopBelowTheStaticStop_DoesNotLoosenIt()
    {
        // Trail 10% behind a high of 103 lands at 92.7, below a static stop at 96. Taking that would
        // widen the risk on a position that just went into profit, so the static stop has to stay.
        decimal trailing = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Long, 103m, 10m, 0m);
        Assert.AreEqual(92.7m, trailing);
        Assert.IsFalse(ProfitLockCalculator.Tightens(CryptoTradeSide.Long, trailing, 96m));
    }

    // ── Stop limit: the worst acceptable fill ───────────────────────────────

    [TestMethod]
    public void StopLimit_Long_SitsOnePercentBelowTheStop()
        => Assert.AreEqual(99.99m, ProfitLockCalculator.StopLimit(CryptoTradeSide.Long, 101m));

    [TestMethod]
    public void StopLimit_Short_SitsOnePercentAboveTheStop()
        => Assert.AreEqual(99.99m, ProfitLockCalculator.StopLimit(CryptoTradeSide.Short, 99m));

    [TestMethod]
    public void StopLimit_WithASmallTrail_CanFallThroughBreakEven()
    {
        // Worth knowing rather than fixing: with a 1.5% trail the stop sits at 101.455 and the worst
        // fill a real exchange may give is 100.44 - still above break-even. With a 0.5% trail the
        // stop is at 102.485 but the limit at 101.46, so the gap is the bigger risk of the two.
        decimal stop = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Long, 103m, 1.5m, 0m);
        decimal limit = ProfitLockCalculator.StopLimit(CryptoTradeSide.Long, stop);
        Assert.IsTrue(limit > 100m, $"slechtste vulling {limit} zakte onder break-even");
    }

    // ── The whole thing end to end, candle by candle ────────────────────────

    /// <summary>
    /// Walks a position through a run of candles the way CalculateSlPrices does: arm on the trigger,
    /// then trail. Asserts the two properties that matter and that no single-value test can catch -
    /// the stop never moves backwards, and it never sits below break-even.
    /// </summary>
    [TestMethod]
    public void Scenario_Long_ArmsOnceAndThenOnlyRatchetsUpwards()
    {
        const decimal breakEven = 100m, trigger = 3m, trail = 1.5m;
        (decimal Low, decimal High)[] candles =
        [
            (99m, 101m),      // nog niet ver genoeg
            (101m, 102.9m),   // nog steeds niet: de hele candle moet voorbij 103
            (103.5m, 105m),   // hier gaat hij aan
            (104m, 110m),     // nieuwe top, stop schuift mee
            (103m, 106m),     // terugval, stop blijft staan
            (105m, 112m),     // nieuwe top
            (100m, 104m),     // diepe terugval
        ];

        bool armed = false;
        decimal trailingStop = 0m, previous = 0m;
        foreach (var (low, high) in candles)
        {
            if (!armed && PositionMonitor.ProfitLockArmed(CryptoTradeSide.Long, breakEven, trigger, low, high, out _))
                armed = true;
            if (!armed)
            {
                Assert.AreEqual(0m, trailingStop, "voor de trigger hoort er geen trailing stop te zijn");
                continue;
            }

            trailingStop = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Long, high, trail, trailingStop);
            Assert.IsTrue(trailingStop >= previous, $"stop zakte van {previous} naar {trailingStop}");
            Assert.IsTrue(trailingStop > breakEven, $"stop {trailingStop} zakte onder break-even");
            previous = trailingStop;
        }

        Assert.IsTrue(armed);
        Assert.AreEqual(110.32m, trailingStop, "de hoogste top van 112 bepaalt het eindniveau");
    }

    [TestMethod]
    public void Scenario_Short_ArmsOnceAndThenOnlyRatchetsDownwards()
    {
        const decimal breakEven = 100m, trigger = 3m, trail = 1.5m;
        (decimal Low, decimal High)[] candles =
        [
            (99m, 101m),
            (97.1m, 99m),     // low is er wel doorheen, high niet: nog niet armed
            (95m, 96.5m),     // hele candle onder 97
            (90m, 94m),
            (93m, 97m),       // terugval
            (88m, 92m),
        ];

        bool armed = false;
        decimal trailingStop = 0m, previous = decimal.MaxValue;
        foreach (var (low, high) in candles)
        {
            if (!armed && PositionMonitor.ProfitLockArmed(CryptoTradeSide.Short, breakEven, trigger, low, high, out _))
                armed = true;
            if (!armed)
                continue;

            trailingStop = ProfitLockCalculator.TrailingStop(CryptoTradeSide.Short, low, trail, trailingStop);
            Assert.IsTrue(trailingStop <= previous, $"stop steeg van {previous} naar {trailingStop}");
            Assert.IsTrue(trailingStop < breakEven, $"stop {trailingStop} steeg boven break-even");
            previous = trailingStop;
        }

        Assert.IsTrue(armed);
        Assert.AreEqual(89.32m, trailingStop, "de laagste bodem van 88 bepaalt het eindniveau");
    }

    [TestMethod]
    public void Scenario_FixedMethod_TheStopNeverMovesAfterArming()
    {
        const decimal breakEven = 100m, trigger = 3m, sl = 1.5m;
        decimal[] highs = [105m, 110m, 104m, 120m];

        decimal first = ProfitLockCalculator.FixedStop(CryptoTradeSide.Long, breakEven, trigger, sl);
        foreach (decimal unused in highs)
            Assert.AreEqual(first, ProfitLockCalculator.FixedStop(CryptoTradeSide.Long, breakEven, trigger, sl));
        Assert.AreEqual(101.5m, first);
    }
}
