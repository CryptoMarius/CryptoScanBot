using CryptoScanner.Core.Barometer;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;

using ExchangeModel = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// A barometer over fewer than five coins describes no market (open point 40): it neither blocks a
/// signal nor counts in the consensus. With enough coins the same value blocks as before.
/// </summary>
[DoNotParallelize]
[TestClass]
public class BarometerTooFewSymbolsTests : TestBase
{
    private const string QuoteName = "BTFS";

    [TestInitialize]
    public void Setup() => InitTestSession();

    private static (ExchangeModel Exchange, CryptoBarometerData Data) Barometer(decimal value, int symbolCount)
    {
        GlobalData.AddQuoteData(QuoteName);
        ExchangeModel exchange = new() { Id = 98, Name = "BarometerTooFewSymbolsExchange" };
        CryptoBarometerData data = exchange.Data.GetBarometer(QuoteName, CryptoIntervalPeriod.interval1h);
        data.PriceBarometer = value;
        data.PriceSymbolCount = symbolCount;
        data.PricePercentageRising = 10m;
        return (exchange, data);
    }

    [TestMethod]
    public void EnoughCoinsStillBlocks()
    {
        var (exchange, _) = Barometer(-5m, 10);
        Assert.IsFalse(BarometerHelper.CheckValidBarometer(exchange, QuoteName, CryptoIntervalPeriod.interval1h, (0m, 999m), out string reaction));
        Assert.AreNotEqual("", reaction);
        Assert.IsFalse(BarometerHelper.CheckBreadth(exchange, QuoteName, 50m, 100m, out _));
    }

    [TestMethod]
    public void TooFewCoinsIsNeutral()
    {
        var (exchange, _) = Barometer(-5m, BarometerHelper.MinimumSymbols - 1);
        Assert.IsTrue(BarometerHelper.CheckValidBarometer(exchange, QuoteName, CryptoIntervalPeriod.interval1h, (0m, 999m), out string reaction));
        Assert.AreEqual("", reaction);
        Assert.IsTrue(BarometerHelper.CheckBreadth(exchange, QuoteName, 50m, 100m, out _));
    }
}
