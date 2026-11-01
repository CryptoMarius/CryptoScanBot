using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;
using CryptoScanner.CoreTests;

using Dapper;

namespace CryptoScanner.Core.Settings.Tests;

/// <summary>
/// The quotes and the default paper assets per exchange (since 31-10-2026): kept in the database, so a
/// switch to another exchange brings neither the quotes nor the paper balances of the previous one.
/// </summary>
[TestClass]
public class ExchangeSettingsStoreTests : TestBase
{
    private Model.CryptoExchange _exchange = null!;
    private Model.CryptoExchange _other = null!;

    private SortedList<string, CryptoQuoteData> _savedQuotes = [];
    private SortedList<string, CryptoProductData> _savedProducts = [];
    private SortedList<string, CryptoProductData>? _savedLegacyProducts;
    private List<CryptoPaperAssetDefault> _savedDefaults = [];
    private SortedList<string, CryptoQuoteData>? _savedLegacyQuotes;
    private List<CryptoPaperAssetDefault>? _savedLegacyDefaults;
    private Model.CryptoExchange? _savedLoadedExchange;
    private string? _savedDefaultQuote;

    [TestInitialize]
    public void SaveState()
    {
        InitTestSession();
        _exchange = GlobalData.ActiveExchange!;
        _other = GlobalData.ExchangeListName.Values.First(e => e.Id != _exchange.Id);

        _savedQuotes = GlobalData.Settings.QuoteCoins;
        _savedProducts = GlobalData.Settings.Products;
        _savedLegacyProducts = ExchangeSettingsStore.LegacyProducts;
        _savedDefaults = GlobalData.Settings.Trading.PaperAssetDefaults;
        _savedLegacyQuotes = ExchangeSettingsStore.LegacyQuoteCoins;
        _savedLegacyDefaults = ExchangeSettingsStore.LegacyPaperAssetDefaults;
        _savedLoadedExchange = ExchangeSettingsStore.LoadedExchange;
        _savedDefaultQuote = ExchangeBase.ExchangeOptions.DefaultQuote;

        ExchangeSettingsStore.LegacyQuoteCoins = null;
        ExchangeSettingsStore.LegacyPaperAssetDefaults = null;
        ExchangeSettingsStore.LegacyProducts = null;
        ClearTables();
    }

    [TestCleanup]
    public void RestoreState()
    {
        ClearTables();
        GlobalData.Settings.QuoteCoins = _savedQuotes;
        GlobalData.Settings.Products = _savedProducts;
        ExchangeSettingsStore.LegacyProducts = _savedLegacyProducts;
        GlobalData.Settings.Trading.PaperAssetDefaults = _savedDefaults;
        ExchangeSettingsStore.LegacyQuoteCoins = _savedLegacyQuotes;
        ExchangeSettingsStore.LegacyPaperAssetDefaults = _savedLegacyDefaults;
        ExchangeSettingsStore.LoadedExchange = _savedLoadedExchange;
        ExchangeBase.ExchangeOptions.DefaultQuote = _savedDefaultQuote;
    }

    private void ClearTables()
    {
        using CryptoDatabase database = new();
        database.Open();
        database.Connection.Execute("delete from Quote where ExchangeId in (@a, @b)", new { a = _exchange.Id, b = _other.Id });
        database.Connection.Execute("delete from Product where ExchangeId in (@a, @b)", new { a = _exchange.Id, b = _other.Id });
        DeleteAllPositionRelatedStuff(database);
        _other.Data.AssetList.Clear();
    }

    private static CryptoQuoteData Quote(string name, bool fetchCandles = true) =>
        new() { Name = name, FetchCandles = fetchCandles, MinimalVolume = 1234 };

    private static int AssetRows(Model.CryptoExchange exchange, string name)
    {
        using CryptoDatabase database = new();
        database.Open();
        return database.Connection.ExecuteScalar<int>("select count(*) from Asset where ExchangeId = @id and Name = @name",
            new { id = exchange.Id, name });
    }


    /// <summary>
    /// An old settings file: its lists and the balances without an exchange go to the first exchange
    /// that is activated, and to that one only.
    /// </summary>
    [TestMethod]
    public void TheFirstActivatedExchangeGetsTheOldListsAndRows()
    {
        using (CryptoDatabase database = new())
        {
            database.Open();
            database.Connection.Execute("insert into Asset (Name, Total, Free, Locked, DefaultTotal) values ('ADA', '5', '5', '0', '0')");
        }
        ExchangeSettingsStore.LegacyQuoteCoins = new() { ["USDT"] = Quote("USDT"), ["USDC"] = Quote("USDC", false) };
        ExchangeSettingsStore.LegacyPaperAssetDefaults = [new() { Name = "usdt", Total = 1000m }];

        bool empty = ExchangeSettingsStore.Activate(_exchange);

        Assert.IsFalse(empty);
        Assert.AreEqual(_exchange, ExchangeSettingsStore.LoadedExchange);
        Assert.IsNull(ExchangeSettingsStore.LegacyQuoteCoins, "moved once");
        Assert.IsNull(ExchangeSettingsStore.LegacyPaperAssetDefaults, "moved once");
        CollectionAssert.AreEqual(new[] { "USDC", "USDT" }, GlobalData.Settings.QuoteCoins.Keys.ToArray());
        Assert.IsTrue(GlobalData.Settings.QuoteCoins["USDT"].FetchCandles);
        Assert.IsFalse(GlobalData.Settings.QuoteCoins["USDC"].FetchCandles);
        Assert.AreEqual(1234, GlobalData.Settings.QuoteCoins["USDT"].MinimalVolume);
        CryptoPaperAssetDefault usdt = GlobalData.Settings.Trading.PaperAssetDefaults.Single();
        Assert.AreEqual("USDT", usdt.Name, "in capitals");
        Assert.AreEqual(1000m, usdt.Total);
        Assert.AreEqual(1, AssetRows(_exchange, "ADA"), "the balance without an exchange is claimed");

        // The other exchange gets none of it
        Assert.IsTrue(ExchangeSettingsStore.Activate(_other));
        Assert.AreEqual(0, GlobalData.Settings.QuoteCoins.Count);
        Assert.AreEqual(0, GlobalData.Settings.Trading.PaperAssetDefaults.Count);
    }


    /// <summary>Switching back and forth gives every exchange its own quotes and defaults back.</summary>
    [TestMethod]
    public void EveryExchangeKeepsItsOwnQuotesAndDefaults()
    {
        ExchangeSettingsStore.Activate(_exchange);
        GlobalData.Settings.QuoteCoins = new() { ["USDC"] = Quote("USDC") };
        GlobalData.Settings.Trading.PaperAssetDefaults = [new() { Name = "USDC", Total = 2000m }];
        ExchangeSettingsStore.Save();

        ExchangeSettingsStore.Activate(_other);
        GlobalData.Settings.QuoteCoins = new() { ["EUR"] = Quote("EUR") };
        GlobalData.Settings.Trading.PaperAssetDefaults = [new() { Name = "EUR", Total = 300m }];
        ExchangeSettingsStore.Save();

        ExchangeSettingsStore.Activate(_exchange);
        CollectionAssert.AreEqual(new[] { "USDC" }, GlobalData.Settings.QuoteCoins.Keys.ToArray());
        Assert.AreEqual(2000m, GlobalData.Settings.Trading.PaperAssetDefaults.Single(d => d.Name == "USDC").Total);
        Assert.AreEqual(1, GlobalData.Settings.Trading.PaperAssetDefaults.Count);

        ExchangeSettingsStore.Activate(_other);
        CollectionAssert.AreEqual(new[] { "EUR" }, GlobalData.Settings.QuoteCoins.Keys.ToArray());
        Assert.AreEqual(300m, GlobalData.Settings.Trading.PaperAssetDefaults.Single().Total);
    }


    /// <summary>
    /// A product switched off on one exchange stays on for another: PERP off on HyperLiquid Perpetual
    /// used to switch off every perpetual on Binance as well.
    /// </summary>
    [TestMethod]
    public void AProductSwitchedOffOnOneExchangeStaysOnForAnother()
    {
        ExchangeSettingsStore.LegacyProducts = new() { ["PERP"] = new() { Name = "PERP", Active = true }, ["XYZ"] = new() { Name = "XYZ", Active = false } };

        ExchangeSettingsStore.Activate(_exchange);
        Assert.IsNull(ExchangeSettingsStore.LegacyProducts, "moved once");
        Assert.IsFalse(GlobalData.Settings.Products["XYZ"].Active, "the old list went to the first exchange");
        GlobalData.Settings.Products["PERP"].Active = false;
        ExchangeSettingsStore.Save();

        ExchangeSettingsStore.Activate(_other);
        Assert.AreEqual(0, GlobalData.Settings.Products.Count, "nothing of the other exchange");
        Assert.IsTrue(GlobalData.IsProductActive("PERP"), "a product that comes by is on");
        ExchangeSettingsStore.Save();

        ExchangeSettingsStore.Activate(_exchange);
        Assert.IsFalse(GlobalData.Settings.Products["PERP"].Active);
        Assert.IsFalse(GlobalData.Settings.Products["XYZ"].Active);
        ExchangeSettingsStore.Activate(_other);
        Assert.IsTrue(GlobalData.Settings.Products["PERP"].Active);
    }


    /// <summary>An exchange without any quote gets its default quote, switched on, and keeps it.</summary>
    [TestMethod]
    public void AnEmptyExchangeGetsItsDefaultQuote()
    {
        ExchangeBase.ExchangeOptions.DefaultQuote = "EUR";

        Assert.IsTrue(ExchangeSettingsStore.Activate(_other), "no quotes yet");
        ExchangeSettingsStore.AddDefaultQuote();

        Assert.IsTrue(GlobalData.Settings.QuoteCoins["EUR"].FetchCandles, "the default quote is active");
        Assert.IsFalse(ExchangeSettingsStore.Activate(_other), "stored, so not empty the next time");
        Assert.IsTrue(GlobalData.Settings.QuoteCoins["EUR"].FetchCandles);
    }


    /// <summary>The paper balances of one exchange do not show up on another.</summary>
    [TestMethod]
    public void LoadAssetsReadsOnlyTheActiveExchange()
    {
        using (CryptoDatabase database = new())
        {
            database.Open();
            database.Connection.Execute("insert into Asset (ExchangeId, Name, Total, Free, Locked, DefaultTotal) values (@id, 'XRP', '7', '7', '0', '0')",
                new { id = _other.Id });
        }
        ExchangeSettingsStore.Activate(_exchange);
        GlobalData.Settings.Trading.PaperAssetDefaults = [];

        PaperAssets.LoadAssets(_exchange);

        Assert.IsFalse(_exchange.Data.AssetList.ContainsKey("XRP"));
        Assert.AreEqual(1, AssetRows(_other, "XRP"), "left alone");
    }


    /// <summary>
    /// The Default column is a setting: a reset empties the balance but keeps the default, and hands it
    /// out again in the same row.
    /// </summary>
    [TestMethod]
    public void ResetKeepsTheDefaultAndRefillsTheSameRow()
    {
        ExchangeSettingsStore.Activate(_exchange);
        GlobalData.Settings.Trading.PaperAssetDefaults = [new() { Name = "USDT", Total = 500m }];
        ExchangeSettingsStore.Save();
        Assert.AreEqual(1, AssetRows(_exchange, "USDT"), "a default alone has a row");

        PaperAssets.ResetAssets(_exchange, 0m);
        PaperAssets.ResetAssets(_exchange, 0m);

        CryptoAsset usdt = _exchange.Data.AssetList["USDT"];
        Assert.AreEqual(500m, usdt.Total);
        Assert.AreEqual(500m, usdt.DefaultTotal);
        Assert.AreEqual(1, AssetRows(_exchange, "USDT"), "no second row for the same coin");
        Assert.AreEqual(500m, ExchangeSettingsStore.LoadDefaults(_exchange).Single().Total);
    }


    /// <summary>
    /// The emulator clears the balances in memory before every run and then resets. The start capital
    /// has to land in the row that holds the default, not in a second row for the same coin.
    /// </summary>
    [TestMethod]
    public void AResetAfterTheListWasClearedKeepsOneRowPerCoin()
    {
        ExchangeSettingsStore.Activate(_exchange);
        GlobalData.Settings.Trading.PaperAssetDefaults = [new() { Name = "USDT", Total = 500m }];
        ExchangeSettingsStore.Save();

        for (int run = 0; run < 3; run++)
        {
            _exchange.Data.AssetList.Clear(); // what ReplayRunner.ResetRunState does
            PaperAssets.ResetAssets(_exchange, 0m);
        }

        Assert.AreEqual(1, AssetRows(_exchange, "USDT"));
        CryptoAsset usdt = _exchange.Data.AssetList["USDT"];
        Assert.AreEqual(500m, usdt.Total);
        Assert.AreEqual(500m, usdt.DefaultTotal, "the row with the default, not a new one");
    }


    /// <summary>The database refuses a second row for the same coin on the same exchange.</summary>
    [TestMethod]
    public void ASecondRowForTheSameCoinIsRefused()
    {
        using CryptoDatabase database = new();
        database.Open();
        database.Connection.Execute("insert into Asset (ExchangeId, Name, Total, Free, Locked) values (@id, 'ADA', '1', '1', '0')", new { id = _exchange.Id });

        Assert.ThrowsExactly<Microsoft.Data.Sqlite.SqliteException>(() =>
            database.Connection.Execute("insert into Asset (ExchangeId, Name, Total, Free, Locked) values (@id, 'ADA', '2', '2', '0')", new { id = _exchange.Id }));
        // The same coin on another exchange is a different balance
        database.Connection.Execute("insert into Asset (ExchangeId, Name, Total, Free, Locked) values (@id, 'ADA', '3', '3', '0')", new { id = _other.Id });
    }


    /// <summary>
    /// An old row without an exchange for a coin the exchange already holds stays unclaimed, instead
    /// of failing the start on the unique index.
    /// </summary>
    [TestMethod]
    public void AnOldRowForACoinThatIsAlreadyHeldIsLeftAlone()
    {
        using (CryptoDatabase database = new())
        {
            database.Open();
            database.Connection.Execute("insert into Asset (ExchangeId, Name, Total, Free, Locked) values (@id, 'ADA', '1', '1', '0')", new { id = _exchange.Id });
            database.Connection.Execute("insert into Asset (Name, Total, Free, Locked) values ('ADA', '2', '2', '0')");
            database.Connection.Execute("insert into Asset (Name, Total, Free, Locked) values ('XRP', '3', '3', '0')");
            database.Connection.Execute("insert into Asset (Name, Total, Free, Locked) values ('XRP', '4', '4', '0')");
        }

        ExchangeSettingsStore.ClaimUnassignedRows(_exchange);

        Assert.AreEqual(1, AssetRows(_exchange, "ADA"));
        Assert.AreEqual(1, AssetRows(_exchange, "XRP"), "the oldest of the two old rows");
        using CryptoDatabase check = new();
        check.Open();
        Assert.AreEqual(2, check.Connection.ExecuteScalar<int>("select count(*) from Asset where ExchangeId is null"));
        check.Connection.Execute("delete from Asset where ExchangeId is null");
    }


    /// <summary>
    /// The migration to version 112 merges a pair the emulator left behind (the empty row with the
    /// default, and the row with the balance) into the oldest row.
    /// </summary>
    [TestMethod]
    public void TheMigrationMergesTwoRowsForTheSameCoin()
    {
        using CryptoDatabase database = new();
        database.Open();
        database.Connection.Execute("drop index IdxAssetExchangeIdName");
        try
        {
            database.Connection.Execute("insert into Asset (ExchangeId, Name, Total, Free, Locked, DefaultTotal) values (@id, 'USDT', '0', '0', '0', '500')", new { id = _exchange.Id });
            database.Connection.Execute("insert into Asset (ExchangeId, Name, Total, Free, Locked, DefaultTotal) values (@id, 'USDT', '480.5', '400.5', '80', '0')", new { id = _exchange.Id });

            using (var transaction = database.BeginTransaction())
            {
                DatabaseMigration.MergeDuplicateAssets(database, transaction);
                transaction.Commit();
            }

            CryptoAsset usdt = database.Connection.Query<CryptoAsset>("select * from Asset where ExchangeId = @id and Name = 'USDT'", new { id = _exchange.Id }).Single();
            Assert.AreEqual(480.5m, usdt.Total);
            Assert.AreEqual(400.5m, usdt.Free);
            Assert.AreEqual(80m, usdt.Locked);
            Assert.AreEqual(500m, usdt.DefaultTotal);
        }
        finally
        {
            database.Connection.Execute("delete from Asset");
            database.Connection.Execute("CREATE UNIQUE INDEX IdxAssetExchangeIdName ON Asset(ExchangeId, Name)");
        }
    }


    /// <summary>A default that is taken away removes a row that held nothing else.</summary>
    [TestMethod]
    public void RemovingADefaultRemovesAnEmptyRow()
    {
        ExchangeSettingsStore.Activate(_exchange);
        GlobalData.Settings.Trading.PaperAssetDefaults = [new() { Name = "ADA", Total = 50m }];
        ExchangeSettingsStore.Save();
        Assert.AreEqual(1, AssetRows(_exchange, "ADA"));

        GlobalData.Settings.Trading.PaperAssetDefaults = [];
        ExchangeSettingsStore.Save();

        Assert.AreEqual(0, AssetRows(_exchange, "ADA"));
        Assert.IsFalse(_exchange.Data.AssetList.ContainsKey("ADA"));
    }


    /// <summary>
    /// The settings file leaves both lists out once they belong to an exchange, and keeps them while
    /// they still wait for one.
    /// </summary>
    [TestMethod]
    public void TheSettingsFileLeavesTheListsOutOnceAnExchangeHasThem()
    {
        GlobalData.Settings.QuoteCoins = new() { ["USDT"] = Quote("USDT") };
        GlobalData.Settings.Trading.PaperAssetDefaults = [new() { Name = "USDT", Total = 1m }];

        ExchangeSettingsStore.LoadedExchange = null;
        System.Text.Json.Nodes.JsonNode waiting = GlobalData.SettingsFileContent();
        Assert.IsNotNull(waiting["QuoteCoins"]);
        Assert.IsNotNull(waiting["Trading"]!["PaperAssetDefaults"]);

        ExchangeSettingsStore.LoadedExchange = _exchange;
        System.Text.Json.Nodes.JsonNode stored = GlobalData.SettingsFileContent();
        Assert.IsNull(stored["QuoteCoins"]);
        Assert.IsNull(stored["Products"]);
        Assert.IsNull(stored["Trading"]!["PaperAssetDefaults"]);
        Assert.IsNotNull(stored["Trading"]!["UseAssetManagement"], "the rest of the trading settings stay");
    }
}
