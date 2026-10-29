using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Settings;
using CryptoScanner.CoreTests;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// The Default column of the paper-assets screen: the default paper assets of the settings, shown and
/// edited next to the balances instead of in a list of their own on the trader settings tab.
/// </summary>
[TestClass]
public class PaperAssetsDefaultColumnTests : TestBase
{
    private List<CryptoPaperAssetDefault> _savedDefaults = [];
    private Action _savedSave = () => { };
    private int _saves;

    [TestInitialize]
    public void SaveSettings()
    {
        InitTestSession();
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;

        _savedDefaults = GlobalData.Settings.Trading.PaperAssetDefaults;
        GlobalData.Settings.Trading.PaperAssetDefaults = [];

        // Count the writes instead of writing the real settings file
        _savedSave = PaperAssetsEditor.SaveSettings;
        _saves = 0;
        PaperAssetsEditor.SaveSettings = () => _saves++;
    }

    [TestCleanup]
    public void RestoreSettings()
    {
        GlobalData.Settings.Trading.PaperAssetDefaults = _savedDefaults;
        PaperAssetsEditor.SaveSettings = _savedSave;
    }

    private static CryptoDatabase Arrange()
    {
        CryptoDatabase database = new();
        database.Open();
        CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);
        GlobalData.ActiveExchange!.Data.AssetList.Clear();
        return database;
    }


    /// <summary>A coin with only a default gets a row too, and a held coin shows its default next to it.</summary>
    [TestMethod]
    public void TheRowsAreTheBalancesAndTheDefaultsTogether()
    {
        using CryptoDatabase database = Arrange();
        GlobalData.Settings.Trading.PaperAssetDefaults =
        [
            new CryptoPaperAssetDefault { Name = "usdt", Total = 1000m },
            new CryptoPaperAssetDefault { Name = "ADA", Total = 50m },
        ];
        PaperAssetsEditor.Add(GlobalData.ActiveExchange, "USDT", 750m);

        List<PaperAssetRow> rows = PaperAssetsEditor.LoadRows(GlobalData.ActiveExchange);

        PaperAssetRow usdt = rows.Single(r => r.Name == "USDT");
        Assert.AreEqual(750m, usdt.Total);
        Assert.AreEqual(1000m, usdt.Default, "matched in capitals");
        PaperAssetRow ada = rows.Single(r => r.Name == "ADA");
        Assert.AreEqual(0m, ada.Total, "not held, only a default");
        Assert.AreEqual(50m, ada.Default);
    }


    /// <summary>Apply writes a changed default into the settings and saves them once.</summary>
    [TestMethod]
    public void ApplyWritesAChangedDefault()
    {
        using CryptoDatabase database = Arrange();
        GlobalData.Settings.Trading.PaperAssetDefaults = [new CryptoPaperAssetDefault { Name = "USDT", Total = 2000m }];

        PaperAssetsEditor.Apply(GlobalData.ActiveExchange,
        [
            new PaperAssetEdit { Name = "USDT", OriginalDefault = 2000m, NewDefault = 100000m },
            new PaperAssetEdit { Name = "BTC", OriginalDefault = 0m, NewDefault = 0.1m },
        ]);

        Assert.AreEqual(1, _saves, "one write for all the changes");
        Assert.AreEqual(100000m, GlobalData.Settings.Trading.PaperAssetDefaults.Single(d => d.Name == "USDT").Total);
        Assert.AreEqual(0.1m, GlobalData.Settings.Trading.PaperAssetDefaults.Single(d => d.Name == "BTC").Total);
        Assert.IsFalse(GlobalData.ActiveExchange!.Data.AssetList.ContainsKey("BTC"), "a default is not a balance");
    }


    /// <summary>A default set to zero takes the coin out of the default paper assets.</summary>
    [TestMethod]
    public void ADefaultOfZeroLeavesTheList()
    {
        using CryptoDatabase database = Arrange();
        GlobalData.Settings.Trading.PaperAssetDefaults = [new CryptoPaperAssetDefault { Name = "ADA", Total = 50m }];

        PaperAssetsEditor.Apply(GlobalData.ActiveExchange,
            [new PaperAssetEdit { Name = "ADA", OriginalDefault = 50m, NewDefault = 0m }]);

        Assert.AreEqual(0, GlobalData.Settings.Trading.PaperAssetDefaults.Count);
    }


    /// <summary>The add row may hold only a default: the coin is not held now, a reset hands it out.</summary>
    [TestMethod]
    public void AddWithOnlyADefault()
    {
        using CryptoDatabase database = Arrange();

        Assert.IsTrue(PaperAssetsEditor.Add(GlobalData.ActiveExchange, " eth ", 0m, 2m));

        Assert.AreEqual(2m, GlobalData.Settings.Trading.PaperAssetDefaults.Single(d => d.Name == "ETH").Total);
        Assert.IsFalse(GlobalData.ActiveExchange!.Data.AssetList.ContainsKey("ETH"));
        Assert.AreEqual(1, _saves);
        Assert.IsFalse(PaperAssetsEditor.Add(GlobalData.ActiveExchange, "ETH", 0m, 0m), "neither an amount nor a default");
    }


    /// <summary>Remove takes the balance and the default away together, or the row comes straight back.</summary>
    [TestMethod]
    public void RemoveTakesTheDefaultAlong()
    {
        using CryptoDatabase database = Arrange();
        PaperAssetsEditor.Add(GlobalData.ActiveExchange, "ADA", 10m, 50m);

        Assert.IsTrue(PaperAssetsEditor.Remove(GlobalData.ActiveExchange, "ADA"));

        Assert.IsFalse(GlobalData.ActiveExchange!.Data.AssetList.ContainsKey("ADA"));
        Assert.AreEqual(0, GlobalData.Settings.Trading.PaperAssetDefaults.Count);
        Assert.IsFalse(PaperAssetsEditor.LoadRows(GlobalData.ActiveExchange).Any(r => r.Name == "ADA"));
    }
}
