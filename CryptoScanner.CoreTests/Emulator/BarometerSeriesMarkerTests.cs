using CryptoScanner.Core.Model;
using CryptoScanner.Emulator.Engine;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The marker that says which coin list a stored barometer series belongs to.
/// <para>
/// Since 04-09-2026 a replay measures its barometer once and every later run over the same coins
/// reads the $BMP/$BMX candles back instead of computing them again. That is only correct while the
/// series really does describe the same market, and this marker is the whole guard: get it wrong and
/// a run silently trades against the barometer of another coin set, which is the kind of error that
/// shows up months later in a measurement that cannot be explained.
/// </para>
/// </summary>
[TestClass]
public class BarometerSeriesMarkerTests
{
    private static readonly CryptoScanner.Core.Model.CryptoExchange TestExchange = new() { Name = "Test" };

    private static CryptoSymbol Coin(string name, CryptoQuoteData quoteData)
    {
        return new CryptoSymbol
        {
            Exchange = TestExchange,
            ExchangeName = TestExchange.Name,
            Name = name,
            Base = name.Replace(quoteData.Name, ""),
            Quote = quoteData.Name,
            QuoteData = quoteData,
        };
    }


    private static (CryptoQuoteData Quote, List<CryptoSymbol> Coins) Set(double minimalVolume, params string[] names)
    {
        CryptoQuoteData quote = new() { Name = "USDT", MinimalVolume = minimalVolume };
        return (quote, [.. names.Select(n => Coin(n, quote))]);
    }


    /// <summary>
    /// The same coins in another order are the same market. The run configuration is edited by hand
    /// and the symbol list comes out in whatever order the exchange hands it over, so an order that
    /// happens to differ must not throw away a series that is perfectly valid.
    /// </summary>
    [TestMethod]
    public void TheOrderOfTheCoinsDoesNotMatter()
    {
        var a = Set(15000000, "BTCUSDT", "ETHUSDT", "SOLUSDT");
        var b = Set(15000000, "SOLUSDT", "BTCUSDT", "ETHUSDT");

        Assert.AreEqual(BarometerReplay.MarkerFor(a.Quote, a.Coins),
                        BarometerReplay.MarkerFor(b.Quote, b.Coins));
    }


    /// <summary>A coin added or removed is another market, so the series has to be measured again.</summary>
    [TestMethod]
    public void AddingOrRemovingACoinChangesTheMarker()
    {
        var basis = Set(15000000, "BTCUSDT", "ETHUSDT");
        var erbij = Set(15000000, "BTCUSDT", "ETHUSDT", "SOLUSDT");
        var eraf = Set(15000000, "BTCUSDT");

        Assert.AreNotEqual(BarometerReplay.MarkerFor(basis.Quote, basis.Coins),
                           BarometerReplay.MarkerFor(erbij.Quote, erbij.Coins));
        Assert.AreNotEqual(BarometerReplay.MarkerFor(basis.Quote, basis.Coins),
                           BarometerReplay.MarkerFor(eraf.Quote, eraf.Coins));
    }


    /// <summary>
    /// The volume threshold decides which of those coins takes part at each moment, so it changes the
    /// outcome over an identical coin list. This is not theory: the emulator ran from 02-09-2026 with
    /// the threshold at 0 instead of 15 million because a HyperLiquid settings file was left behind,
    /// and that alone made the barometer a different series.
    /// </summary>
    [TestMethod]
    public void TheVolumeThresholdIsPartOfTheMarker()
    {
        var streng = Set(15000000, "BTCUSDT", "ETHUSDT");
        var open = Set(0, "BTCUSDT", "ETHUSDT");

        Assert.AreNotEqual(BarometerReplay.MarkerFor(streng.Quote, streng.Coins),
                           BarometerReplay.MarkerFor(open.Quote, open.Coins));
    }


    /// <summary>Each quote coin keeps its own series, so each has its own key.</summary>
    [TestMethod]
    public void EveryQuoteCoinHasItsOwnKey()
    {
        Assert.AreNotEqual(BarometerReplay.MarkerKey("USDT"), BarometerReplay.MarkerKey("USDC"));
        StringAssert.Contains(BarometerReplay.MarkerKey("USDT"), "USDT");
    }
}
