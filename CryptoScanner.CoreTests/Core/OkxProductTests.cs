using CryptoScanner.Core.Exchange.Okx;
using CryptoScanner.Core.Model;

using OKX.Net.Enums;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// Okx runs one market that holds two questions at once: what kind of contract an instrument is,
/// and what it is on. A third of the Okx Perpetual market follows a share, an index or a commodity
/// rather than a coin, and until 28-08-2026 every one of them read as an ordinary coin in the grids
/// - AAOIUSDC (Applied Optoelectronics on the Nasdaq) sat between the coins with the same badge as
/// AAVEUSDC. These tests guard the rule that tells the two apart.
/// </summary>
[TestClass]
public class OkxProductTests
{
    /// <summary>
    /// A coin keeps the code of the contract type it was found under, so the swaps stay PERP and
    /// the X-Perps stay XPERP.
    /// </summary>
    [TestMethod]
    public void CryptoKeepsTheContractType()
    {
        Assert.AreEqual(CryptoProduct.Perpetual, OkxProduct.Of(SymbolCategory.Crypto, CryptoProduct.Perpetual));
        Assert.AreEqual(CryptoProduct.XPerp, OkxProduct.Of(SymbolCategory.Crypto, CryptoProduct.XPerp));
    }


    /// <summary>
    /// Everything that is not a coin becomes TRADFI, whichever of the two lists it came out of.
    /// AAOI is a share and arrives as a swap (AAOI-USDT-SWAP) as well as an X-Perp
    /// (AAOI-USD_UM_XPERP-310711); both have to end up on the same side of the line.
    /// </summary>
    [TestMethod]
    public void EverythingElseBecomesTradFi()
    {
        Assert.AreEqual(CryptoProduct.TradFi, OkxProduct.Of(SymbolCategory.Stocks, CryptoProduct.Perpetual));
        Assert.AreEqual(CryptoProduct.TradFi, OkxProduct.Of(SymbolCategory.Stocks, CryptoProduct.XPerp));
        Assert.AreEqual(CryptoProduct.TradFi, OkxProduct.Of(SymbolCategory.Commodities, CryptoProduct.Perpetual));
        Assert.AreEqual(CryptoProduct.TradFi, OkxProduct.Of(SymbolCategory.Commodities, CryptoProduct.XPerp));
    }


    /// <summary>
    /// Forex and Bonds are in the enum of the library but Okx lists neither today. They belong with
    /// the shares and the commodities, so the rule may not be written as a list of two categories
    /// that a third one silently escapes.
    /// </summary>
    [TestMethod]
    public void ForexAndBondsCountAsTradFiAsWell()
    {
        Assert.AreEqual(CryptoProduct.TradFi, OkxProduct.Of(SymbolCategory.Forex, CryptoProduct.Perpetual));
        Assert.AreEqual(CryptoProduct.TradFi, OkxProduct.Of(SymbolCategory.Bonds, CryptoProduct.XPerp));
    }


    /// <summary>
    /// An absent category counts as a coin. Okx fills the field on every instrument of both lists,
    /// so this only fires if the exchange stops sending it - and then keeping the contract type
    /// changes nothing, while the other way round would rename the whole market in one refresh.
    /// </summary>
    [TestMethod]
    public void AnAbsentCategoryKeepsTheContractType()
    {
        Assert.AreEqual(CryptoProduct.Perpetual, OkxProduct.Of(null, CryptoProduct.Perpetual));
        Assert.AreEqual(CryptoProduct.XPerp, OkxProduct.Of(null, CryptoProduct.XPerp));
    }


    /// <summary>
    /// The badge has to be its own colour, otherwise the split is invisible in the grid - which is
    /// the entire reason for it. TRADFI is also one of ours and not a party that deployed a market,
    /// so the black and white list has to recognise it as reserved.
    /// </summary>
    [TestMethod]
    public void TradFiHasItsOwnBadge()
    {
        Assert.AreNotEqual(CryptoProduct.ColorOf(CryptoProduct.Perpetual), CryptoProduct.ColorOf(CryptoProduct.TradFi));
        Assert.AreNotEqual(CryptoProduct.ColorOf(CryptoProduct.XPerp), CryptoProduct.ColorOf(CryptoProduct.TradFi));
        Assert.IsTrue(CryptoProduct.IsReserved(CryptoProduct.TradFi));
    }


    /// <summary>
    /// The pair stays what it always was, so a rule in the black and white list written as AAOIUSDC
    /// keeps matching after the product behind the dot changed.
    /// </summary>
    [TestMethod]
    public void TheProductDoesNotChangeThePair()
    {
        Assert.AreEqual("AAOIUSDC", CryptoProduct.PairOf("AAOIUSDC." + CryptoProduct.TradFi));
    }
}
