using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Exchanges;

/// <summary>
/// The precision pass that runs once per refresh cycle, right after the exchange handed out new tick
/// sizes.
/// <para>
/// A candle keeps the number of decimals it was written with, and that number comes from
/// symbol.PriceDecimals. Until 16-09-2026 the field was only computed when a symbol was loaded or
/// inserted, never when a refresh assigned a new tick size to a symbol already in memory - so a
/// corrected tick size reached the candles one scanner start later. It cost the Bitvavo history once
/// and a night of HyperLiquid Spot: 23.027 impossible candles, 16 of 39 coins stored as nothing but
/// zeros.
/// </para>
/// </summary>
[TestClass]
public class SymbolPrecisionRefreshTests
{
    private static CryptoSymbol Add(Exchange exchange, string name, string basis, decimal priceTickSize, byte decimals)
    {
        CryptoSymbol symbol = new()
        {
            Id = exchange.SymbolListName.Count + 1,
            Status = 1,
            Base = basis,
            Quote = "USDC",
            Name = name,
            Exchange = exchange,
            ExchangeName = name,
            QuoteData = new CryptoQuoteData { Name = "USDC" },
            PriceTickSize = priceTickSize,
            QuantityTickSize = 0.01m,
            PriceDecimals = decimals,
        };
        exchange.SymbolListName.Add(name, symbol);
        return symbol;
    }


    /// <summary>
    /// The case this exists for: the symbol was loaded with the wrong tick size of a previous build,
    /// the refresh assigned the real one, and the decimals have to follow in the same cycle.
    /// </summary>
    [TestMethod]
    public void ANewTickSize_ReachesTheDecimalsInTheSameCycle()
    {
        Exchange exchange = new() { Id = 1, Name = "TestExchange" };
        CryptoSymbol symbol = Add(exchange, "HYPEUSDC", "HYPE", priceTickSize: 1m, decimals: 0);
        symbol.PriceTickSize = 0.001m; // what the refresh just assigned

        var (changed, coarser) = CandleBase.UpdateSymbolPrecision(exchange);

        Assert.AreEqual((byte)3, symbol.PriceDecimals);
        Assert.AreEqual("N3", symbol.PriceDisplayFormat);
        CollectionAssert.AreEqual(new List<string> { "HYPEUSDC 0->3" }, changed,
            "en het hoort in het log te komen, want de candles van daarvoor staan nog op de oude decimalen");
        Assert.AreEqual(1, coarser, "de al opgeslagen candles zijn grover dan wat er vanaf nu binnenkomt");
    }


    /// <summary>
    /// A barometer symbol holds a percentage. Its two decimals are set by hand and it has no tick
    /// size to derive anything from, so deriving would put it back on zero - the very damage this
    /// pass prevents elsewhere.
    /// </summary>
    [TestMethod]
    public void ABarometerSymbol_KeepsTheDecimalsItWasGiven()
    {
        Exchange exchange = new() { Id = 1, Name = "TestExchange" };
        CryptoSymbol barometer = Add(exchange, "$BMPUSDC", "$BMP", priceTickSize: 0m, decimals: 2);

        var (changed, _) = CandleBase.UpdateSymbolPrecision(exchange);

        Assert.AreEqual((byte)2, barometer.PriceDecimals);
        Assert.AreEqual(0, changed.Count);
    }


    /// <summary>
    /// A tick size of zero means the exchange told us nothing, not that this coin trades in whole
    /// units. Deriving from it would round every price to a whole number.
    /// </summary>
    [TestMethod]
    public void ASymbolWithoutATickSize_IsLeftAlone()
    {
        Exchange exchange = new() { Id = 1, Name = "TestExchange" };
        CryptoSymbol symbol = Add(exchange, "PURRUSDC", "PURR", priceTickSize: 0m, decimals: 4);

        var (changed, _) = CandleBase.UpdateSymbolPrecision(exchange);

        Assert.AreEqual((byte)4, symbol.PriceDecimals);
        Assert.AreEqual(0, changed.Count);
    }


    /// <summary>Running it again reports nothing, so the log only speaks when something moved.</summary>
    [TestMethod]
    public void RunningItAgain_ReportsNothing()
    {
        Exchange exchange = new() { Id = 1, Name = "TestExchange" };
        CryptoSymbol symbol = Add(exchange, "HYPEUSDC", "HYPE", priceTickSize: 1m, decimals: 0);
        symbol.PriceTickSize = 0.001m;

        Assert.AreEqual(1, CandleBase.UpdateSymbolPrecision(exchange).Changed.Count);
        Assert.AreEqual(0, CandleBase.UpdateSymbolPrecision(exchange).Changed.Count);
    }


    /// <summary>
    /// The other direction costs nothing and must not be reported as if it did: a candle stored with
    /// MORE decimals than the symbol now needs is simply more precise than it has to be, and it reads
    /// back as what it was - a candle divides by its own tick size, not by the symbol's.
    /// </summary>
    [TestMethod]
    public void ACoarserTickSize_IsNotReportedAsALossOfPrecision()
    {
        Exchange exchange = new() { Id = 1, Name = "TestExchange" };
        CryptoSymbol symbol = Add(exchange, "HYPEUSDC", "HYPE", priceTickSize: 0.00001m, decimals: 5);
        symbol.PriceTickSize = 0.001m; // the exchange widened its tick size

        var (changed, coarser) = CandleBase.UpdateSymbolPrecision(exchange);

        Assert.AreEqual((byte)3, symbol.PriceDecimals);
        CollectionAssert.AreEqual(new List<string> { "HYPEUSDC 5->3" }, changed, "het verschil wordt wel gemeld");
        Assert.AreEqual(0, coarser, "maar niet als verlies: de opgeslagen candles zijn juist fijner dan nodig");
    }
}
