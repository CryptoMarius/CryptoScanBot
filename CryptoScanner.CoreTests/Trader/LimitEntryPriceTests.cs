using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// The price of a limit entry with SettingsTrading.EntryLimitOffsetPercentage: the order waits a
/// percentage below the signal price for a long and above it for a short.
/// </summary>
[TestClass]
public class LimitEntryPriceTests
{
    [TestMethod]
    public void WithoutAnOffset_TheOrderSitsOnTheSignalPrice()
        => Assert.AreEqual(100m, PositionMonitor.LimitEntryPrice(CryptoTradeSide.Long, 100m, 0m));

    [TestMethod]
    public void Long_WaitsBelowTheSignalPrice()
        => Assert.AreEqual(98m, PositionMonitor.LimitEntryPrice(CryptoTradeSide.Long, 100m, 2m));

    [TestMethod]
    public void Short_WaitsAboveTheSignalPrice()
        => Assert.IsTrue(PositionMonitor.LimitEntryPrice(CryptoTradeSide.Short, 100m, 2m) > 100m);

    [TestMethod]
    public void AnImpossibleOffset_IsIgnored()
        => Assert.AreEqual(100m, PositionMonitor.LimitEntryPrice(CryptoTradeSide.Long, 100m, 100m));
}
