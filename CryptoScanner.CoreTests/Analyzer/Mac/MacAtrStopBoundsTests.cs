using CryptoScanner.Analyzers.Mac.Signal;

namespace CryptoScanner.CoreTests.Analyzer.Mac;

/// <summary>
/// The ATR stop of mac kept between StopAtrMinimumPercentage and StopAtrMaximumPercentage.
/// </summary>
[TestClass]
public class MacAtrStopBoundsTests
{
    [TestMethod]
    public void WithoutBounds_TheAtrStopStaysAsItIs()
        => Assert.AreEqual(30m, MacBase.BoundedStop(30m, 0m, 0m));

    [TestMethod]
    public void AQuietCoin_GetsTheMinimum()
        => Assert.AreEqual(2m, MacBase.BoundedStop(0.53m, 2m, 15m));

    [TestMethod]
    public void AWildCoin_GetsTheMaximum()
        => Assert.AreEqual(15m, MacBase.BoundedStop(30m, 2m, 15m));

    [TestMethod]
    public void AStopBetweenTheBounds_StaysAsItIs()
        => Assert.AreEqual(5.49m, MacBase.BoundedStop(5.49m, 2m, 15m));
}
