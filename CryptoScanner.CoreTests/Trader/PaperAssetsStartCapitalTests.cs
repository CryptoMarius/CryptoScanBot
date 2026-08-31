using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.CoreTests;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// The start capital per quote coin.
/// <para>
/// One amount for every quote coin cannot be right: 10.000 is a sensible amount of USDT and an absurd
/// amount of BTC, while the capital line adds every coin up in USDT - 10.000 BTC would put the
/// starting point of that line above a billion. So the amount on the quote coin itself wins, and zero
/// on it means "not filled in".
/// </para>
/// </summary>
[TestClass]
public class PaperAssetsStartCapitalTests : TestBase
{
    private decimal _savedStartCapital;
    private List<CryptoPaperAssetDefault> _savedDefaults = [];
    private SortedList<string, CryptoQuoteData> _savedQuoteCoins = [];
    private readonly List<(CryptoQuoteData quote, bool fetchCandles, decimal startCapital)> _savedQuoteState = [];

    [TestInitialize]
    public void SaveSettings()
    {
        InitTestSession();
        _savedStartCapital = GlobalData.Settings.Trading.PaperAssetStartCapital;

        // No default assets unless a test asks for them - the list is what most of these tests are
        // NOT about, and another test class may have left one behind.
        _savedDefaults = GlobalData.Settings.Trading.PaperAssetDefaults;
        GlobalData.Settings.Trading.PaperAssetDefaults = [];

        // The quote coins are process-static, so both the list and the two fields these tests write
        // have to go back the way they were - another test class reads the very same objects.
        _savedQuoteCoins = new SortedList<string, CryptoQuoteData>(GlobalData.Settings.QuoteCoins);
        _savedQuoteState.Clear();
        foreach (CryptoQuoteData quote in GlobalData.Settings.QuoteCoins.Values)
            _savedQuoteState.Add((quote, quote.FetchCandles, quote.StartCapital));
    }

    [TestCleanup]
    public void RestoreSettings()
    {
        GlobalData.Settings.Trading.PaperAssetStartCapital = _savedStartCapital;
        GlobalData.Settings.Trading.PaperAssetDefaults = _savedDefaults;
        foreach (var (quote, fetchCandles, startCapital) in _savedQuoteState)
        {
            quote.FetchCandles = fetchCandles;
            quote.StartCapital = startCapital;
        }
        GlobalData.Settings.QuoteCoins = _savedQuoteCoins;
    }


    /// <summary>Two traded quote coins: USDT without an amount of its own, BTC with one.</summary>
    private static (CryptoQuoteData quoteUsdt, CryptoQuoteData quoteBtc) ArrangeTwoQuoteCoins(CryptoSymbol symbol)
    {
        CryptoQuoteData quoteUsdt = symbol.QuoteData!;
        quoteUsdt.FetchCandles = true;
        quoteUsdt.StartCapital = 0;
        GlobalData.Settings.QuoteCoins[quoteUsdt.Name] = quoteUsdt;

        CryptoQuoteData quoteBtc = GlobalData.AddQuoteData("BTC");
        quoteBtc.FetchCandles = true;
        quoteBtc.StartCapital = 0.1m;

        return (quoteUsdt, quoteBtc);
    }


    /// <summary>An amount filled in on the quote coin wins over the general one.</summary>
    [TestMethod]
    public void TheAmountOnTheQuoteCoinWins()
    {
        CryptoQuoteData quote = new() { Name = "BTC", StartCapital = 0.25m };
        Assert.AreEqual(0.25m, PaperAssets.ResolveStartCapital(quote, 10000m));
    }


    /// <summary>
    /// Nothing filled in means the general amount, so a configuration that trades USDT only keeps
    /// behaving exactly as it did before this setting existed.
    /// </summary>
    [TestMethod]
    public void AnEmptyAmountFallsBackToTheGeneralOne()
    {
        CryptoQuoteData quote = new() { Name = "USDT" };
        Assert.AreEqual(10000m, PaperAssets.ResolveStartCapital(quote, 10000m), "zero is 'not filled in'");

        quote.StartCapital = -5m;
        Assert.AreEqual(10000m, PaperAssets.ResolveStartCapital(quote, 10000m), "and so is a negative amount");
    }


    /// <summary>
    /// Reset hands the amount that was asked for to the quote coins without one of their own, and
    /// leaves the others on theirs: 5.000 USDT next to 0,1 BTC, not 5.000 of both.
    /// </summary>
    [TestMethod]
    public void ResetGivesEveryQuoteCoinItsOwnStartCapital()
    {
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);

        var (quoteUsdt, _) = ArrangeTwoQuoteCoins(symbol);

        PaperAssets.ResetAssets(GlobalData.ActiveExchange!, 5000m);

        Assert.IsTrue(GlobalData.ActiveExchange!.Data.AssetList.TryGetValue(quoteUsdt.Name, out CryptoAsset? seededUsdt),
            "the traded quote coin is seeded");
        Assert.AreEqual(5000m, seededUsdt!.Total, "USDT has no amount of its own, so it gets the one that was asked for");

        Assert.IsTrue(GlobalData.ActiveExchange!.Data.AssetList.TryGetValue("BTC", out CryptoAsset? seededBtc),
            "and so is the second one");
        Assert.AreEqual(0.1m, seededBtc!.Total, "BTC keeps its own amount - 5000 BTC would be nonsense");
    }


    /// <summary>
    /// The same on a fresh database, where nobody types anything at all: this is the path that hands
    /// out money by itself, so it is the one where a single amount does the most damage.
    /// </summary>
    [TestMethod]
    public void SeedingAFreshDatabaseUsesTheAmountOfEachQuoteCoin()
    {
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        // Empties the Asset table as well, which is what makes LoadAssets seed
        DeleteAllPositionRelatedStuff(database);

        GlobalData.Settings.Trading.PaperAssetStartCapital = 7500m;
        var (quoteUsdt, _) = ArrangeTwoQuoteCoins(symbol);

        PaperAssets.LoadAssets(GlobalData.ActiveExchange!);

        Assert.AreEqual(7500m, GlobalData.ActiveExchange!.Data.AssetList[quoteUsdt.Name].Total,
            "USDT falls back to the general amount");
        Assert.AreEqual(0.1m, GlobalData.ActiveExchange!.Data.AssetList["BTC"].Total,
            "BTC gets the amount it has itself");
    }


    /// <summary>
    /// A filled default asset list IS the starting point: coin by coin, whatever the quote coins and
    /// the general amount say, and a coin that is not traded as a quote coin at all can be in it.
    /// </summary>
    [TestMethod]
    public void TheDefaultAssetListReplacesEveryStartCapital()
    {
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);

        // Both would hand out something of their own, and neither may be looked at
        GlobalData.Settings.Trading.PaperAssetStartCapital = 7500m;
        var (quoteUsdt, _) = ArrangeTwoQuoteCoins(symbol);

        GlobalData.Settings.Trading.PaperAssetDefaults =
        [
            new CryptoPaperAssetDefault { Name = quoteUsdt.Name, Total = 250m },
            new CryptoPaperAssetDefault { Name = "ADA", Total = 1000m },
        ];

        PaperAssets.ResetAssets(GlobalData.ActiveExchange!, 5000m);

        Assert.AreEqual(250m, GlobalData.ActiveExchange!.Data.AssetList[quoteUsdt.Name].Total,
            "the list wins from the amount that was asked for");
        Assert.AreEqual(1000m, GlobalData.ActiveExchange!.Data.AssetList["ADA"].Total,
            "a coin that is not a quote coin is handed out just the same");
        Assert.IsFalse(GlobalData.ActiveExchange!.Data.AssetList.ContainsKey("BTC"),
            "BTC is not in the list, so it gets nothing - not even its own start capital");
    }


    /// <summary>Half-filled rows from the settings screen are skipped, not handed out as nothing.</summary>
    [TestMethod]
    public void ADefaultAssetWithoutACoinOrAnAmountIsSkipped()
    {
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);
        ArrangeTwoQuoteCoins(symbol);

        GlobalData.Settings.Trading.PaperAssetDefaults =
        [
            new CryptoPaperAssetDefault { Name = "", Total = 500m },
            new CryptoPaperAssetDefault { Name = "ADA", Total = 0m },
            new CryptoPaperAssetDefault { Name = "ada", Total = 1000m },
        ];

        PaperAssets.ResetAssets(GlobalData.ActiveExchange!, 5000m);

        Assert.AreEqual(1, GlobalData.ActiveExchange!.Data.AssetList.Count, "only the usable row is handed out");
        Assert.AreEqual(1000m, GlobalData.ActiveExchange!.Data.AssetList["ADA"].Total, "and in capitals");
    }
}
