using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;

using Dapper;
using Dapper.Contrib.Extensions;

namespace CryptoScanner.Core.Settings;

/// <summary>
/// The settings that belong to one exchange and are kept in the database instead of the settings
/// file (since 31-10-2026): the quote coins (table Quote), the products (table Product) and the
/// Default column of Paper assets (column DefaultTotal of table Asset).
/// <para>
/// Until then both were one list in the settings file that every exchange shared. Switching from
/// HyperLiquid Spot to Binance Spot kept USDC active next to the new USDT, and on Bitvavo Spot the
/// default quote EUR stayed off because Binance had already added it, switched off. The paper
/// balances were shared in the same way: the Asset table had no exchange at all.
/// </para>
/// <para>
/// The rest of the application keeps reading <see cref="SettingsBasic.QuoteCoins"/>,
/// <see cref="SettingsBasic.Products"/> and <see cref="SettingsTrading.PaperAssetDefaults"/>. Those are now the lists of the active exchange:
/// <see cref="Activate"/> fills them from the database, and <see cref="Save"/> (called by
/// GlobalData.SaveConfiguration) writes them back. The settings file no longer carries them. An
/// emulator run still stores them in its settings snapshot, so its fingerprint stays the same.
/// </para>
/// </summary>
public static class ExchangeSettingsStore
{
    /// <summary>
    /// The quote coins of a settings file written before 31-10-2026, waiting for the exchange they
    /// belong to. That is the first exchange activated after the file was read.
    /// </summary>
    internal static SortedList<string, CryptoQuoteData>? LegacyQuoteCoins { get; set; }

    /// <summary>
    /// The default paper assets of a settings file written before 31-10-2026, see <see cref="LegacyQuoteCoins"/>.
    /// </summary>
    internal static List<CryptoPaperAssetDefault>? LegacyPaperAssetDefaults { get; set; }

    /// <summary>
    /// The products of a settings file written before 31-10-2026, see <see cref="LegacyQuoteCoins"/>.
    /// </summary>
    internal static SortedList<string, CryptoProductData>? LegacyProducts { get; set; }

    /// <summary>
    /// The exchange the two lists in the settings belong to, null while no exchange has been
    /// activated since the settings were read. <see cref="Save"/> writes nothing while it is null:
    /// the lists then hold either nothing or the legacy lists of the file, and neither belongs to
    /// an exchange yet.
    /// </summary>
    public static Model.CryptoExchange? LoadedExchange { get; internal set; }


    /// <summary>
    /// Called after the settings file was read: the lists in it are from before 31-10-2026 and wait
    /// for the first exchange that is activated. The settings object was replaced, so whatever was
    /// loaded for an exchange before is gone as well.
    /// </summary>
    internal static void SettingsWereRead(SettingsBasic settings)
    {
        LoadedExchange = null;

        if (settings.QuoteCoins.Count > 0)
            LegacyQuoteCoins = settings.QuoteCoins;
        if (settings.Trading.PaperAssetDefaults.Count > 0)
            LegacyPaperAssetDefaults = settings.Trading.PaperAssetDefaults;
        if (settings.Products.Count > 0)
            LegacyProducts = settings.Products;
    }


    /// <summary>
    /// Make the quotes and the default paper assets of <paramref name="exchange"/> the ones in the
    /// settings. Rows that have no exchange yet (written before version 111 of the database) and the
    /// lists of an old settings file go to this exchange first. Returns true when the exchange has no
    /// quote at all, so the caller adds its default quote (<see cref="AddDefaultQuote"/>).
    /// <para>
    /// Every symbol keeps pointing at the quote object of the previous lists until it is linked again
    /// (GlobalData.AddSymbol does that, as does RunSettingsApplier.RelinkSymbolsToQuoteCoins).
    /// </para>
    /// </summary>
    public static bool Activate(Model.CryptoExchange exchange)
    {
        ClaimUnassignedRows(exchange);

        if (LegacyQuoteCoins != null)
        {
            SaveQuotes(exchange, LegacyQuoteCoins.Values);
            GlobalData.AddTextToLogTab($"The {LegacyQuoteCoins.Count} quote(s) of the settings file moved to the database, for {exchange.Name}");
            LegacyQuoteCoins = null;
        }

        if (LegacyPaperAssetDefaults != null)
        {
            SaveDefaults(exchange, LegacyPaperAssetDefaults);
            GlobalData.AddTextToLogTab($"The {LegacyPaperAssetDefaults.Count} default paper asset(s) of the settings file moved to the database, for {exchange.Name}");
            LegacyPaperAssetDefaults = null;
        }

        if (LegacyProducts != null)
        {
            SaveProducts(exchange, LegacyProducts.Values);
            GlobalData.AddTextToLogTab($"The {LegacyProducts.Count} product(s) of the settings file moved to the database, for {exchange.Name}");
            LegacyProducts = null;
        }

        GlobalData.Settings.QuoteCoins = LoadQuotes(exchange);
        GlobalData.Settings.Products = LoadProducts(exchange);
        GlobalData.Settings.Trading.PaperAssetDefaults = LoadDefaults(exchange);
        LoadedExchange = exchange;

        return GlobalData.Settings.QuoteCoins.Count == 0;
    }


    /// <summary>
    /// Add the default quote of the active exchange as a quote that fetches candles, with the
    /// minimal volume of that exchange - for an exchange that has no quote at all. Needs the exchange
    /// defaults to be applied first (ExchangeDefaults), they say what the default quote is.
    /// </summary>
    public static void AddDefaultQuote()
    {
        string? defaultQuote = ExchangeBase.ExchangeOptions.DefaultQuote;
        if (string.IsNullOrEmpty(defaultQuote))
            return;

        CryptoQuoteData quoteData = GlobalData.AddQuoteData(defaultQuote);
        quoteData.FetchCandles = true;
        // The boundary comes from the exchange itself: what counts as a tradable 24 hour volume on
        // Binance leaves nothing at all on a small exchange like HyperLiquid.
        quoteData.MinimalVolume = ExchangeBase.ExchangeOptions.MinimalVolume;
        GlobalData.AddTextToLogTab($"No quotes for {GlobalData.ActiveExchange?.Name} yet, the default quote {defaultQuote} added");

        Save();
    }


    /// <summary>
    /// Write the quotes and the default paper assets of the settings to the exchange they were
    /// loaded for. Called by GlobalData.SaveConfiguration, so everything that changes these lists
    /// and saves the settings keeps working as it did.
    /// </summary>
    public static void Save()
    {
        Model.CryptoExchange? exchange = LoadedExchange;
        if (exchange == null)
            return;

        SaveQuotes(exchange, GlobalData.Settings.QuoteCoins.Values);
        SaveProducts(exchange, GlobalData.Settings.Products.Values);
        SaveDefaults(exchange, GlobalData.Settings.Trading.PaperAssetDefaults);
    }


    /// <summary>
    /// Gives the rows from before version 111 of the database (no exchange yet) to
    /// <paramref name="exchange"/>. The migration cannot do it, it runs before the settings are read.
    /// The first exchange activated afterwards is the one the scanner runs on, and so the one these
    /// balances were made on.
    /// </summary>
    internal static void ClaimUnassignedRows(Model.CryptoExchange exchange)
    {
        using CryptoDatabase database = new();
        database.Open();
        using var transaction = database.BeginTransaction();

        // One row at a time, because of the unique index on (ExchangeId, Name): a coin the exchange
        // already holds, or a second old row for the same coin, keeps its NULL and is ignored instead
        // of failing the whole start.
        int assets = 0;
        HashSet<string> held = [.. database.Connection.Query<string>("select Name from Asset where ExchangeId = @id",
            new { id = exchange.Id }, transaction)];
        foreach ((int rowId, string name) in database.Connection.Query<(int, string)>(
            "select Id, Name from Asset where ExchangeId is null order by Id", transaction: transaction).ToList())
        {
            if (held.Add(name))
                assets += database.Connection.Execute("update Asset set ExchangeId = @id where Id = @rowId",
                    new { id = exchange.Id, rowId }, transaction);
        }
        database.Connection.Execute("update AssetSnapshot set ExchangeId = @id where ExchangeId is null", new { id = exchange.Id }, transaction);
        database.Connection.Execute("update AssetAdjustment set ExchangeId = @id where ExchangeId is null", new { id = exchange.Id }, transaction);
        transaction.Commit();

        if (assets > 0)
            GlobalData.AddTextToLogTab($"The {assets} paper asset(s) without an exchange now belong to {exchange.Name}");
    }


    internal static SortedList<string, CryptoQuoteData> LoadQuotes(Model.CryptoExchange exchange)
    {
        using CryptoDatabase database = new();
        database.Open();

        return ReadQuotes(database.Connection.Query<QuoteRow>("select * from Quote where ExchangeId = @id", new { id = exchange.Id }));
    }


    /// <summary>
    /// The quotes of <paramref name="exchangeName"/> in the database of another data folder, read
    /// only. For CandleSync, which brings along the symbols of the quotes a scanner fetches: it used
    /// to copy the scanner's settings file for that, and the quotes are no longer in there. Null when
    /// that database has no Quote table yet (a scanner from before 31-10-2026).
    /// </summary>
    public static SortedList<string, CryptoQuoteData>? LoadQuotesOfDataFolder(string dataFolder, string exchangeName)
    {
        string dbFile = Path.Combine(dataFolder, Const.Constants.AppName + ".db");
        if (!File.Exists(dbFile))
            return null;

        using Microsoft.Data.Sqlite.SqliteConnection connection = new($"Filename={dbFile};Mode=ReadOnly;");
        connection.Open();
        if (connection.ExecuteScalar<long>("select count(*) from sqlite_master where type = 'table' and name = 'Quote'") == 0)
            return null;

        return ReadQuotes(connection.Query<QuoteRow>(
            "select Quote.* from Quote inner join Exchange on Exchange.Id = Quote.ExchangeId where Exchange.Name = @exchangeName",
            new { exchangeName }));
    }


    private static SortedList<string, CryptoQuoteData> ReadQuotes(IEnumerable<QuoteRow> rows)
    {
        SortedList<string, CryptoQuoteData> quotes = [];
        foreach (QuoteRow row in rows)
        {
            CryptoQuoteData quoteData = new()
            {
                Name = row.Name,
                DisplayFormat = row.DisplayFormat,
                FetchCandles = row.FetchCandles,
                MinimalVolume = row.MinimalVolume,
                MinimalPrice = row.MinimalPrice,
                EntryAmount = row.EntryAmount,
                EntryPercentage = (float)row.EntryPercentage,
            };
            try
            {
                quoteData.DisplayColor = CoreColor.Parse(row.DisplayColor);
            }
            catch (Exception)
            {
                // A colour that cannot be read keeps the default, it only tints a grid row
            }
            quotes.TryAdd(quoteData.Name, quoteData);
        }
        return quotes;
    }


    /// <summary>
    /// Replace the quotes of <paramref name="exchange"/> in the database by <paramref name="quotes"/>.
    /// </summary>
    internal static void SaveQuotes(Model.CryptoExchange exchange, IEnumerable<CryptoQuoteData> quotes)
    {
        // Copied first: the list is the live one, and the symbol refresh can add a quote to it while
        // this runs
        List<CryptoQuoteData> list = [.. quotes];

        using CryptoDatabase database = new();
        database.Open();
        using var transaction = database.BeginTransaction();

        database.Connection.Execute("delete from Quote where ExchangeId = @id", new { id = exchange.Id }, transaction);
        foreach (CryptoQuoteData quoteData in list)
        {
            QuoteRow row = new()
            {
                ExchangeId = exchange.Id,
                Name = quoteData.Name,
                DisplayFormat = quoteData.DisplayFormat,
                FetchCandles = quoteData.FetchCandles,
                MinimalVolume = quoteData.MinimalVolume,
                MinimalPrice = quoteData.MinimalPrice,
                EntryAmount = quoteData.EntryAmount,
                EntryPercentage = quoteData.EntryPercentage,
                DisplayColor = quoteData.DisplayColor.ToString(),
            };
            database.Connection.Insert(row, transaction);
        }
        transaction.Commit();
    }


    internal static SortedList<string, CryptoProductData> LoadProducts(Model.CryptoExchange exchange)
    {
        using CryptoDatabase database = new();
        database.Open();

        return ReadProducts(database.Connection.Query<ProductRow>("select * from Product where ExchangeId = @id", new { id = exchange.Id }));
    }


    /// <summary>
    /// The products of <paramref name="exchangeName"/> in the database of another data folder, read
    /// only - for CandleSync, see <see cref="LoadQuotesOfDataFolder"/>. Null when that database has no
    /// Product table yet.
    /// </summary>
    public static SortedList<string, CryptoProductData>? LoadProductsOfDataFolder(string dataFolder, string exchangeName)
    {
        string dbFile = Path.Combine(dataFolder, Const.Constants.AppName + ".db");
        if (!File.Exists(dbFile))
            return null;

        using Microsoft.Data.Sqlite.SqliteConnection connection = new($"Filename={dbFile};Mode=ReadOnly;");
        connection.Open();
        if (connection.ExecuteScalar<long>("select count(*) from sqlite_master where type = 'table' and name = 'Product'") == 0)
            return null;

        return ReadProducts(connection.Query<ProductRow>(
            "select Product.* from Product inner join Exchange on Exchange.Id = Product.ExchangeId where Exchange.Name = @exchangeName",
            new { exchangeName }));
    }


    private static SortedList<string, CryptoProductData> ReadProducts(IEnumerable<ProductRow> rows)
    {
        SortedList<string, CryptoProductData> products = [];
        foreach (ProductRow row in rows)
            products.TryAdd(row.Name, new CryptoProductData { Name = row.Name, Active = row.Active });
        return products;
    }


    /// <summary>
    /// Replace the products of <paramref name="exchange"/> in the database by <paramref name="products"/>.
    /// </summary>
    internal static void SaveProducts(Model.CryptoExchange exchange, IEnumerable<CryptoProductData> products)
    {
        // Copied first, like the quotes: the symbol refresh adds products to the live list
        List<CryptoProductData> list = [.. products];

        using CryptoDatabase database = new();
        database.Open();
        using var transaction = database.BeginTransaction();

        database.Connection.Execute("delete from Product where ExchangeId = @id", new { id = exchange.Id }, transaction);
        foreach (CryptoProductData productData in list)
        {
            ProductRow row = new()
            {
                ExchangeId = exchange.Id,
                Name = productData.Name,
                Active = productData.Active,
            };
            database.Connection.Insert(row, transaction);
        }
        transaction.Commit();
    }


    internal static List<CryptoPaperAssetDefault> LoadDefaults(Model.CryptoExchange exchange)
    {
        using CryptoDatabase database = new();
        database.Open();

        // DefaultTotal is TEXT, like every other decimal in this database, so it is cast to compare it
        return [.. database.Connection.Query<CryptoPaperAssetDefault>(
            "select Name, DefaultTotal as Total from Asset " +
            "where ExchangeId = @id and cast(DefaultTotal as REAL) > 0 order by Name", new { id = exchange.Id })];
    }


    /// <summary>
    /// Make <paramref name="defaults"/> the Default column of the paper account of
    /// <paramref name="exchange"/>: in the database, and in the balances in memory, because those are
    /// written back whole on every fill (PaperAssets.UpdateAsset) and would otherwise put the previous
    /// default back. A coin with a default but no balance gets a row of its own; a row that is left
    /// with nothing at all is removed, as UpdateAsset does.
    /// </summary>
    internal static void SaveDefaults(Model.CryptoExchange exchange, List<CryptoPaperAssetDefault> defaults)
    {
        // In capitals and once per coin, the way the reset reads them (PaperAssets.SeedStartBalances)
        Dictionary<string, decimal> wanted = [];
        foreach (CryptoPaperAssetDefault entry in defaults.ToList())
        {
            if (string.IsNullOrWhiteSpace(entry.Name) || entry.Total <= 0)
                continue;
            wanted[entry.Name.Trim().ToUpperInvariant()] = entry.Total;
        }

        exchange.Data.AssetListSemaphore.Wait();
        try
        {
            using CryptoDatabase database = new();
            database.Open();
            using var transaction = database.BeginTransaction();

            database.Connection.Execute("update Asset set DefaultTotal = '0' where ExchangeId = @id", new { id = exchange.Id }, transaction);
            foreach ((string name, decimal total) in wanted)
            {
                int updated = database.Connection.Execute("update Asset set DefaultTotal = @total where ExchangeId = @id and Name = @name",
                    new { total, id = exchange.Id, name }, transaction);
                if (updated == 0)
                {
                    // In memory as well, with its id: the trader would otherwise insert a second row
                    // for this coin the first time it books on it. A balance the trader made but has
                    // not written yet (id 0) is the row itself.
                    if (!exchange.Data.AssetList.TryGetValue(name, out CryptoAsset? asset))
                    {
                        asset = new() { Name = name };
                        exchange.Data.AssetList.TryAdd(name, asset);
                    }
                    asset.ExchangeId = exchange.Id;
                    asset.DefaultTotal = total;
                    if (asset.Id == 0)
                        database.Connection.Insert(asset, transaction);
                    else
                        database.Connection.Update(asset, transaction);
                }
            }

            database.Connection.Execute("delete from Asset where ExchangeId = @id and cast(Total as REAL) = 0 " +
                "and cast(Locked as REAL) = 0 and cast(DefaultTotal as REAL) = 0", new { id = exchange.Id }, transaction);
            transaction.Commit();

            foreach (CryptoAsset asset in exchange.Data.AssetList.Values)
            {
                asset.DefaultTotal = wanted.TryGetValue(asset.Name, out decimal total) ? total : 0;
                if (asset.Total == 0 && asset.Locked == 0 && asset.DefaultTotal == 0)
                    exchange.Data.AssetList.TryRemove(asset.Name, out _);
            }
        }
        finally
        {
            exchange.Data.AssetListSemaphore.Release();
        }
    }


    /// <summary>One row of table Product.</summary>
    [Table("Product")]
    public class ProductRow
    {
        [Key]
        public int Id { get; set; }
        public int ExchangeId { get; set; }
        public string Name { get; set; } = "";
        public bool Active { get; set; }
    }


    /// <summary>One row of table Quote.</summary>
    [Table("Quote")]
    public class QuoteRow
    {
        [Key]
        public int Id { get; set; }
        public int ExchangeId { get; set; }
        public string Name { get; set; } = "";
        public string DisplayFormat { get; set; } = "N8";
        public bool FetchCandles { get; set; }
        public double MinimalVolume { get; set; }
        public decimal MinimalPrice { get; set; }
        public decimal EntryAmount { get; set; }
        public double EntryPercentage { get; set; }
        public string DisplayColor { get; set; } = "";
    }
}
