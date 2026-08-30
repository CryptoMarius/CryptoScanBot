using HyperLiquidPerpetual = CryptoScanner.Core.Exchange.HyperLiquid.Perpetual.Symbol;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// Tests for the price tick of a HyperLiquid perpetual market. HyperLiquid publishes no tick size,
/// so the scanner derives one from the two rules it states for an order price: at most five
/// significant figures, and at most 6 - szDecimals decimals.
/// <para>
/// The prices below are the mark prices of 30-08-2026 14:05, the refresh that made the old
/// derivation - counting the decimals the mark price happened to be written with - cancel and
/// replace the exit orders of every open position.
/// </para>
/// </summary>
[TestClass]
public class HyperLiquidPriceTickTests
{
    private static decimal Tick(decimal markPrice, int quantityDecimals)
        => HyperLiquidPerpetual.PriceTickFromMarkPrice(markPrice, quantityDecimals);

    [TestMethod]
    public void FiveSignificantFiguresDecideBelowOne()
    {
        Assert.AreEqual(0.00001m, Tick(0.85497m, 1), "DOT");
        Assert.AreEqual(0.00001m, Tick(0.20117m, 0), "WIF");
    }

    [TestMethod]
    public void FiveSignificantFiguresDecideAboveOne()
    {
        Assert.AreEqual(0.0001m, Tick(5.1684m, 1), "UNI");
        Assert.AreEqual(0.0001m, Tick(7.392m, 2), "AVAX");
        Assert.AreEqual(0.01m, Tick(105.26m, 2), "SOL");
    }

    /// <summary>
    /// A price of five digits has spent all five significant figures before the decimal point, so
    /// nothing is left behind it. The old derivation read "78270.0" and handed back 0.1, on which
    /// HyperLiquid rejects 78270.1: six significant figures and not a whole number.
    /// </summary>
    [TestMethod]
    public void AFiveDigitPriceHasNoDecimalsLeft()
    {
        Assert.AreEqual(1m, Tick(78270.0m, 5), "BTC");
    }

    /// <summary>
    /// Small prices run out of decimals before they run out of significant figures: five figures of
    /// 0.00054 would need eight decimals, and a perpetual price carries at most 6 - szDecimals.
    /// </summary>
    [TestMethod]
    public void TheDecimalsRuleCapsTheSmallPrices()
    {
        Assert.AreEqual(0.000001m, Tick(0.00054m, 0), "MEME");
        Assert.AreEqual(0.001m, Tick(1.492m, 3), "szDecimals 3 leaves three decimals");
    }

    /// <summary>
    /// The regression this was written for. HyperLiquid drops trailing zeros, so the very same price
    /// arrives as 0.8549 one hour and 0.85497 the next. Counting the decimals of that answer moved
    /// the tick with it; the magnitude does not move.
    /// </summary>
    [TestMethod]
    public void TrailingZerosInTheMarkPriceDoNotMoveTheTick()
    {
        decimal tick = Tick(0.85497m, 1);
        Assert.AreEqual(tick, Tick(0.8549m, 1));
        Assert.AreEqual(tick, Tick(0.85m, 1));
        Assert.AreEqual(tick, Tick(0.8m, 1));
    }

    /// <summary>
    /// Only a price that crosses a power of ten is allowed to change the tick - that is the exchange
    /// changing it as well, not the spelling of the answer.
    /// </summary>
    [TestMethod]
    public void CrossingAPowerOfTenDoesMoveTheTick()
    {
        Assert.AreEqual(0.00001m, Tick(0.99999m, 1));
        Assert.AreEqual(0.0001m, Tick(1.0001m, 1));
    }

    /// <summary>
    /// Without a price there is no magnitude to measure, so only the decimals rule is left. A market
    /// without a mark price must not end up on a tick of zero: Clamp divides by it.
    /// </summary>
    [TestMethod]
    public void AMarketWithoutAPriceFallsBackOnTheDecimalsRule()
    {
        Assert.AreEqual(0.000001m, Tick(0m, 0));
        Assert.AreEqual(0.1m, Tick(0m, 5));
        Assert.AreEqual(1m, Tick(0m, 9), "szDecimals beyond the rule may not give a tick of zero");
    }
}
