using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;

using CryptoExchange.Net.SharedApis;

// CryptoExchange is a package namespace as well, so the model type gets an alias here
using TestExchange = CryptoScanner.Core.Model.CryptoExchange;

namespace CryptoScanner.CoreTests.Core;

/// <summary>
/// A product (the code behind the dot in a symbol name) can be switched off as a whole in the
/// settings, so a user who does not want the markets outside parties deployed on HyperLiquid
/// (GOLDUSDC.XYZ) does not have to black list a hundred symbols one by one. These tests pin down
/// the two halves: the settings list that fills itself as products come by, and the symbol refresh
/// that refuses an instrument of a product that is off - which is what deactivates it.
/// </summary>
[TestClass]
public class ProductSettingsTests
{
    private SettingsBasic _savedSettings = null!;

    [TestInitialize]
    public void Setup()
    {
        // Every test starts with settings that have never seen a product
        _savedSettings = GlobalData.Settings;
        GlobalData.Settings = new SettingsBasic();
    }

    [TestCleanup]
    public void Cleanup()
    {
        GlobalData.Settings = _savedSettings;
    }


    [TestMethod]
    public void ANewProductIsActiveAndLandsInTheSettings()
    {
        CryptoProductData product = GlobalData.AddProductData("xyz");

        Assert.IsTrue(product.Active);
        Assert.AreEqual("XYZ", product.Name);
        Assert.IsTrue(GlobalData.Settings.Products.ContainsKey("XYZ"));
        Assert.IsTrue(GlobalData.IsProductActive("XYZ"));
    }


    /// <summary>
    /// Paragon is not carried by Altrady, so it starts switched off - the default the hard coded
    /// exclusion turned into on 05-09-2026.
    /// </summary>
    [TestMethod]
    public void ParagonStartsSwitchedOff()
    {
        Assert.IsFalse(GlobalData.IsProductActive("para"));
        Assert.IsFalse(GlobalData.Settings.Products["PARA"].Active);
    }


    /// <summary>
    /// What the user set is what counts: a product switched off stays off on the next startup, and
    /// Paragon switched on stays on. The default only applies the first time a product comes by.
    /// </summary>
    [TestMethod]
    public void TheUsersChoiceIsNeverOverwritten()
    {
        GlobalData.AddProductData("XYZ").Active = false;
        GlobalData.AddProductData("PARA").Active = true;

        Assert.IsFalse(GlobalData.IsProductActive("XYZ"));
        Assert.IsTrue(GlobalData.IsProductActive("PARA"));
    }


    /// <summary>
    /// A barometer symbol carries no product, and there is nothing to switch off for it.
    /// </summary>
    [TestMethod]
    public void AnEmptyProductIsAlwaysActive()
    {
        Assert.IsTrue(GlobalData.IsProductActive(""));
        Assert.AreEqual(0, GlobalData.Settings.Products.Count);
    }


    private static TestExchange CreateExchange() => new()
    {
        Id = 1,
        Name = "Test",
        TradingType = CryptoTradingType.Perpetual,
    };


    /// <summary>
    /// The refresh accepts an instrument of a product that is on, and refuses one of a product that
    /// is off. Refusing is what deactivates it: the caller never sees the instrument, so it does not
    /// land in the active list and the deactivation loop puts an existing symbol on status 0.
    /// </summary>
    [TestMethod]
    public void AnInstrumentOfASwitchedOffProductIsRefused()
    {
        TestExchange exchange = CreateExchange();
        SymbolBase.SymbolInfo gold = SymbolBase.ParseSymbol("xyz:GOLD", "GOLD", "USDC", "xyz");
        SymbolBase.SymbolInfo btc = SymbolBase.ParseSymbol("BTC", "BTC", "USDC", CryptoProduct.Perpetual);

        // Both products are on: both instruments are accepted
        Assert.IsTrue(SymbolBase.IsSymbolAccepted(exchange, gold, null!, TradingMode.PerpetualLinear, out CryptoSymbol? goldSymbol));
        Assert.IsTrue(SymbolBase.IsSymbolAccepted(exchange, btc, null!, TradingMode.PerpetualLinear, out _));
        Assert.IsNotNull(goldSymbol);
        Assert.AreEqual("GOLDUSDC.XYZ", goldSymbol.Name);

        // XYZ off: its instrument is refused, the exchange's own market is untouched
        GlobalData.Settings.Products["XYZ"].Active = false;
        Assert.IsFalse(SymbolBase.IsSymbolAccepted(exchange, gold, null!, TradingMode.PerpetualLinear, out CryptoSymbol? refused));
        Assert.IsNull(refused);
        Assert.IsTrue(SymbolBase.IsSymbolAccepted(exchange, btc, null!, TradingMode.PerpetualLinear, out _));
    }
}
