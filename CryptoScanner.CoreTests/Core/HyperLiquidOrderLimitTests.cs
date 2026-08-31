using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange.HyperLiquid;
using CryptoScanner.Core.Model;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The order limits of HyperLiquid, and the minimum entry that follows from them. HyperLiquid states
/// no minimum and no maximum QUANTITY per symbol - the meta carries szDecimals and maxLeverage and
/// nothing else about size - so the minimum entry is the larger of one size tick and the minimum
/// order VALUE of ten in the quote currency.
/// <para>
/// The prices below are the mark prices of 30-08-2026, the same refresh the price tick tests use.
/// </para>
/// </summary>
[TestClass]
public class HyperLiquidOrderLimitTests : TestBase
{
    [TestInitialize]
    public void Init() => InitTestSession();


    /// <summary>
    /// Four brackets on the maximum value of a market order, ten times that for a limit order. The
    /// boundaries are the interesting part: the bracket is entered AT the leverage, not above it.
    /// </summary>
    [TestMethod]
    public void TheMaximumOrderValueFollowsTheLeverageBrackets()
    {
        Assert.AreEqual(300_000_000m, HyperLiquidOrderLimits.MaximumLimitOrderValue(40), "BTC, leverage 40");
        Assert.AreEqual(300_000_000m, HyperLiquidOrderLimits.MaximumLimitOrderValue(25), "ETH, leverage 25");
        Assert.AreEqual(50_000_000m, HyperLiquidOrderLimits.MaximumLimitOrderValue(24), "just under the top bracket");
        Assert.AreEqual(50_000_000m, HyperLiquidOrderLimits.MaximumLimitOrderValue(20), "SOL, leverage 20");
        Assert.AreEqual(20_000_000m, HyperLiquidOrderLimits.MaximumLimitOrderValue(19), "just under it");
        Assert.AreEqual(20_000_000m, HyperLiquidOrderLimits.MaximumLimitOrderValue(10), "HYPE, leverage 10");
        Assert.AreEqual(5_000_000m, HyperLiquidOrderLimits.MaximumLimitOrderValue(5), "ATOM, leverage 5");
        Assert.AreEqual(5_000_000m, HyperLiquidOrderLimits.MaximumLimitOrderValue(3), "the lowest bracket");
    }


    /// <summary>
    /// A market whose maxLeverage could not be read lands in the lowest bracket rather than in the
    /// highest. That is the case for a deployed market that answers without the field.
    /// </summary>
    [TestMethod]
    public void AMissingLeverageLandsInTheLowestBracket()
    {
        Assert.AreEqual(5_000_000m, HyperLiquidOrderLimits.MaximumLimitOrderValue(0));
    }


    /// <summary>
    /// Over the 176 active perpetual markets of 30-08-2026 there is not one where a single size tick
    /// is worth more than ten dollar - BTC has the most expensive one at 0.79 - so the minimum order
    /// value decides everywhere.
    /// </summary>
    [TestMethod]
    public void TheMinimumOrderValueDecidesOnAPerpetualMarket()
    {
        Assert.AreEqual(10m, MinEntry(quantityTick: 0.00001m, price: 79156m), "BTC");
        Assert.AreEqual(10m, MinEntry(quantityTick: 0.01m, price: 83.893m), "HYPE");
        Assert.AreEqual(10m, MinEntry(quantityTick: 1m, price: 1.4184m), "XRP, a whole coin at a time");
    }


    /// <summary>
    /// Three of the 326 spot pairs do go over it, all three a size tick of 0.01 under a price in the
    /// thousands. There one tick is the minimum entry, and it is four times the ten dollar.
    /// </summary>
    [TestMethod]
    public void TheSizeTickDecidesWhereItOutweighsTheMinimumOrderValue()
    {
        Assert.AreEqual(44.6215m, MinEntry(quantityTick: 0.01m, price: 4462.15m), "XAUT0/USDC");
        Assert.AreEqual(15.80m, MinEntry(quantityTick: 0.01m, price: 1580m), "TSLA/USDC");
    }


    /// <summary>The minimum entry of a symbol filled the way both HyperLiquid markets fill it.</summary>
    private static decimal MinEntry(decimal quantityTick, decimal price)
    {
        var exchange = new Exchange { Id = 1, Name = "HyperLiquid", FeeRate = 0.045m };
        CryptoSymbol symbol = new()
        {
            Id = 1,
            Name = "TESTUSDC",
            Base = "TEST",
            Quote = "USDC",
            Exchange = exchange,
            ExchangeId = exchange.Id,
            ExchangeName = exchange.Name,
            QuoteData = GlobalData.AddQuoteData("USDC"),
            PriceTickSize = 0.0001m,
            QuantityTickSize = quantityTick,
            QuantityMinimum = quantityTick,
            QuoteValueMinimum = HyperLiquidOrderLimits.MinimumOrderValue,
            LastPrice = price,
        };

        CryptoSignal signal = new()
        {
            Exchange = exchange,
            ExchangeId = exchange.Id,
            Symbol = symbol,
            SymbolId = symbol.Id,
            Interval = GlobalData.IntervalListId.Count > 0
                ? GlobalData.IntervalListId.Values[0]
                : new CryptoInterval { Id = 1, Name = "1m", Duration = 1 },
            IntervalId = 1,
            Candle = null,
        };
        return signal.MinEntry;
    }
}
