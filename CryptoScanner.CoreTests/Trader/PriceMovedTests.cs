using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// Tests for PositionMonitor.PriceMoved, the question the exit orders are repriced on: has the
/// calculated price really moved, or is this the price grid shifting underneath an unchanged
/// calculation? A cancel and a replace cost the order its place in the queue and two calls to the
/// exchange, so a difference the exchange cannot even represent must not trigger one.
/// </summary>
[TestClass]
public class PriceMovedTests
{
    private const decimal Tick = 0.0001m;

    [TestMethod]
    public void TheSamePriceHasNotMoved()
    {
        Assert.IsFalse(PositionMonitor.PriceMoved(0.784m, 0.784m, Tick));
    }

    /// <summary>
    /// The case of 30-08-2026: the tick of DOT went from 0.0001 to 0.00001 over nothing but the
    /// spelling of the mark price, and the take profit order of every open position was cancelled
    /// and placed again half a tick away, reported over Telegram as a changed break-even price.
    /// </summary>
    [TestMethod]
    public void HalfATickIsNotAMove()
    {
        Assert.IsFalse(PositionMonitor.PriceMoved(0.784m, 0.78405m, Tick));
        Assert.IsFalse(PositionMonitor.PriceMoved(0.9167m, 0.91676m, Tick));
    }

    [TestMethod]
    public void ExactlyOneTickIsNotAMoveButMoreThanOneIs()
    {
        Assert.IsFalse(PositionMonitor.PriceMoved(0.7840m, 0.7841m, Tick));
        Assert.IsTrue(PositionMonitor.PriceMoved(0.7840m, 0.78411m, Tick));
    }

    [TestMethod]
    public void ARealRepriceStillCounts()
    {
        Assert.IsTrue(PositionMonitor.PriceMoved(0.784m, 0.79m, Tick));
        Assert.IsTrue(PositionMonitor.PriceMoved(0.79m, 0.784m, Tick), "and in the other direction");
    }

    /// <summary>
    /// An exchange that states no tick size falls back on the exact comparison this replaced -
    /// nothing is allowed to slip through unnoticed there.
    /// </summary>
    [TestMethod]
    public void WithoutATickSizeTheComparisonIsExact()
    {
        Assert.IsFalse(PositionMonitor.PriceMoved(0.784m, 0.784m, 0m));
        Assert.IsTrue(PositionMonitor.PriceMoved(0.784m, 0.78401m, 0m));
    }

    /// <summary>
    /// A stop loss price is optional. Gaining or losing one is a change whatever the tick size says.
    /// </summary>
    [TestMethod]
    public void AppearingOrDisappearingIsAlwaysAMove()
    {
        Assert.IsFalse(PositionMonitor.PriceMoved(null, null, Tick));
        Assert.IsTrue(PositionMonitor.PriceMoved(null, 0.9167m, Tick));
        Assert.IsTrue(PositionMonitor.PriceMoved(0.9167m, null, Tick));
    }
}
