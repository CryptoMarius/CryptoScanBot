using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// The product is what makes a symbol name unique. An exchange can offer one pair as several
/// instruments - BTC-USDT next to BTC-USDT-SWAP - and both parse to the pair BTCUSDT; the product
/// behind the dot is what keeps them apart. These tests guard the two halves of that: composing the
/// name, and the black and white list that has to keep matching rules written before the dot existed.
/// </summary>
[TestClass]
public class SymbolProductTests
{
    [TestMethod]
    public void NameCarriesTheProduct()
    {
        var info = SymbolBase.ParseSymbol("BTC-USDT-SWAP", "BTC", "USDT", CryptoProduct.Perpetual);

        Assert.AreEqual("BTCUSDT.PERP", info.ScannerName);
        Assert.AreEqual("BTC-USDT-SWAP", info.ExchangeName);
        Assert.AreEqual("BTC", info.Base);
        Assert.AreEqual("USDT", info.Quote);
        Assert.AreEqual("PERP", info.Product);
    }


    /// <summary>
    /// The same pair as two instruments has to give two names, which is the whole point.
    /// </summary>
    [TestMethod]
    public void SamePairDifferentProductGivesDifferentNames()
    {
        var spot = SymbolBase.ParseSymbol("BTC-USDT", "BTC", "USDT", CryptoProduct.Spot);
        var perp = SymbolBase.ParseSymbol("BTC-USDT-SWAP", "BTC", "USDT", CryptoProduct.Perpetual);

        Assert.AreEqual("BTCUSDT.SPOT", spot.ScannerName);
        Assert.AreEqual("BTCUSDT.PERP", perp.ScannerName);
        Assert.AreNotEqual(spot.ScannerName, perp.ScannerName);
    }


    /// <summary>
    /// A market an outside party deployed carries that party as its product, and the base stays the
    /// coin itself - "hyna:BTC" is BTC against USDC on HyENA's order book.
    /// </summary>
    [TestMethod]
    public void DeployedMarketCarriesItsDeployer()
    {
        var info = SymbolBase.ParseSymbol("hyna:BTC", "BTC", "USDC", "hyna");

        Assert.AreEqual("BTCUSDC.HYNA", info.ScannerName);
        Assert.AreEqual("BTC", info.Base);
        Assert.AreEqual("HYNA", info.Product);
    }


    /// <summary>
    /// A barometer symbol is ours, not an instrument of any exchange, so it keeps the name it always
    /// had. Its "$BM" prefix already cannot collide with anything.
    /// </summary>
    [TestMethod]
    public void SymbolWithoutProductKeepsThePlainName()
    {
        var info = SymbolBase.ParseSymbol("$BMPUSDT", "$BMP", "USDT", "");

        Assert.AreEqual("$BMPUSDT", info.ScannerName);
        Assert.AreEqual("", info.Product);
    }


    private static SettingsCompiled ListWith(params string[] entries)
    {
        var settings = new SettingsCompiled();
        foreach (string entry in entries)
            settings.BlackList.Add(entry, true);
        return settings;
    }


    /// <summary>
    /// A rule that names no product covers the pair, so it blocks every instrument on it. That is
    /// what someone typing a coin means, and it is why the lists needed no migration when the
    /// product moved into the name.
    /// </summary>
    [TestMethod]
    public void RuleWithoutProductCoversEveryProduct()
    {
        var settings = ListWith("BTCUSDT");

        Assert.AreEqual(MatchBlackAndWhiteList.Present, settings.InBlackList("BTCUSDT.PERP"));
        Assert.AreEqual(MatchBlackAndWhiteList.Present, settings.InBlackList("BTCUSDT.SPOT"));
        Assert.AreEqual(MatchBlackAndWhiteList.Present, settings.InBlackList("BTCUSDT.INVERSE"));
        Assert.AreEqual(MatchBlackAndWhiteList.NotPresent, settings.InBlackList("ETHUSDT.PERP"));
    }


    /// <summary>
    /// A rule that does name a product covers only that one, so it stays possible to block the
    /// perpetual and keep trading the spot.
    /// </summary>
    [TestMethod]
    public void RuleWithProductCoversOnlyThatOne()
    {
        var settings = ListWith("BTCUSDT.PERP");

        Assert.AreEqual(MatchBlackAndWhiteList.Present, settings.InBlackList("BTCUSDT.PERP"));
        Assert.AreEqual(MatchBlackAndWhiteList.NotPresent, settings.InBlackList("BTCUSDT.SPOT"));
    }


    /// <summary>
    /// An empty list means the rule does not apply at all, which is a different answer from "not in
    /// the list" - the caller treats the two differently.
    /// </summary>
    [TestMethod]
    public void EmptyListSaysEmpty()
    {
        var settings = new SettingsCompiled();

        Assert.AreEqual(MatchBlackAndWhiteList.Empty, settings.InBlackList("BTCUSDT.PERP"));
    }

    /// <summary>
    /// Altrady names an Okx X-Perp after the instrument of the exchange, expiry and all:
    /// "APR-USD_UM_XPERP-310815" is OKEXF_USDC_APR_UM-XPERP-310815 there. Confirmed against their
    /// own code on 28-08-2026, which is where the last part of the link comes from.
    /// </summary>
    [TestMethod]
    public void ExpiryComesFromTheInstrumentName()
    {
        Assert.AreEqual("310815", CryptoExternalUrlList.ExpiryOf("APR-USD_UM_XPERP-310815"));
        Assert.AreEqual("310404", CryptoExternalUrlList.ExpiryOf("BTC-USD_UM_XPERP-310404"));
        Assert.AreEqual("260828", CryptoExternalUrlList.ExpiryOf("BTC-USD_UM-260828"));
    }


    /// <summary>
    /// An instrument without an expiry answers with nothing. The tail of "BTC-USDT-SWAP" is SWAP,
    /// and a link that pasted that where a date belongs would open the wrong market rather than none.
    /// </summary>
    [TestMethod]
    public void InstrumentWithoutExpiryGivesNothing()
    {
        Assert.AreEqual("", CryptoExternalUrlList.ExpiryOf("BTC-USDT-SWAP"));
        Assert.AreEqual("", CryptoExternalUrlList.ExpiryOf("BTC-USDT"));
        Assert.AreEqual("", CryptoExternalUrlList.ExpiryOf("BTCUSDT"));
        Assert.AreEqual("", CryptoExternalUrlList.ExpiryOf("hyna:BTC"));
        Assert.AreEqual("", CryptoExternalUrlList.ExpiryOf("PF_XBTUSD"));
    }
}
