using System.Globalization;

using CryptoScanner.Core.Model;

using Exchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Model;

/// <summary>
/// PriceDecimals and the display formats are derived from the tick sizes, and until 04-09-2026 that
/// went through text: format the tick size with the current culture, look for that culture's decimal
/// separator, count what is behind it. It held up only because both halves used the same culture.
/// The derivation is arithmetic now; these tests pin down that the answer is the same on a Dutch
/// Windows (decimal comma) and an American one (decimal point), and that a refresh that assigns a
/// new tick size to an existing symbol changes the decimals along with it.
/// </summary>
[TestClass]
public class SymbolDecimalsTests
{
    private static readonly CultureInfo Dutch = CultureInfo.GetCultureInfo("nl-NL");
    private static readonly CultureInfo American = CultureInfo.GetCultureInfo("en-US");

    private static CryptoSymbol CreateSymbol(decimal priceTickSize, decimal quantityTickSize)
    {
        var exchange = new Exchange { Id = 1, Name = "TestExchange" };
        return new CryptoSymbol
        {
            Id = 1,
            Status = 1,
            Base = "TEST",
            Quote = "USDC",
            Name = "TESTUSDC",
            Exchange = exchange,
            ExchangeName = exchange.Name,
            QuoteData = new CryptoQuoteData { Name = "USDC" },
            PriceTickSize = priceTickSize,
            QuantityTickSize = quantityTickSize,
        };
    }

    private static T UnderCulture<T>(CultureInfo culture, Func<T> work)
    {
        CultureInfo previous = CultureInfo.CurrentCulture;
        CultureInfo.CurrentCulture = culture;
        try
        {
            return work();
        }
        finally
        {
            CultureInfo.CurrentCulture = previous;
        }
    }

    [TestMethod]
    [DataRow("0.00001", 5)]
    [DataRow("0.000001", 6)]
    [DataRow("0.001", 3)]
    [DataRow("0.01", 2)]
    [DataRow("0.1", 1)]
    [DataRow("1", 0)]
    [DataRow("100", 0)]   // Kraken Perpetual states -2 decimals for a contract trading in steps of 100
    [DataRow("0.000000000000001", 15)]
    public void DecimalsOf_SameAnswerUnderDutchAndAmericanCulture(string tickText, int expected)
    {
        decimal tickSize = decimal.Parse(tickText, CultureInfo.InvariantCulture);

        byte dutch = UnderCulture(Dutch, () => CryptoSymbol.DecimalsOf(tickSize));
        byte american = UnderCulture(American, () => CryptoSymbol.DecimalsOf(tickSize));

        Assert.AreEqual(expected, dutch, $"nl-NL for tick {tickText}");
        Assert.AreEqual(expected, american, $"en-US for tick {tickText}");
    }

    [TestMethod]
    public void DeriveDecimalsFromTickSizes_FillsDecimalsAndBothDisplayFormats()
    {
        CryptoSymbol symbol = CreateSymbol(priceTickSize: 0.00001m, quantityTickSize: 0.01m);

        UnderCulture(Dutch, () => { symbol.DeriveDecimalsFromTickSizes(); return 0; });

        Assert.AreEqual((byte)5, symbol.PriceDecimals);
        Assert.AreEqual("N5", symbol.PriceDisplayFormat);
        Assert.AreEqual("N2", symbol.QuantityDisplayFormat);
    }

    /// <summary>
    /// The tester's case: the database held a tick size of 1 (a leftover of the previous build), the
    /// exchange refresh assigned the real tick size, and the decimals have to follow that refresh
    /// instead of staying at what the symbol was loaded with.
    /// </summary>
    [TestMethod]
    public void DeriveDecimalsFromTickSizes_FollowsANewTickSize()
    {
        CryptoSymbol symbol = CreateSymbol(priceTickSize: 1m, quantityTickSize: 1m);
        symbol.DeriveDecimalsFromTickSizes();
        Assert.AreEqual((byte)0, symbol.PriceDecimals, "loaded with a tick size of 1");

        symbol.PriceTickSize = 0.00001m;
        symbol.DeriveDecimalsFromTickSizes();

        Assert.AreEqual((byte)5, symbol.PriceDecimals, "after the refresh assigned the real tick size");
        Assert.AreEqual("N5", symbol.PriceDisplayFormat);
    }
}
