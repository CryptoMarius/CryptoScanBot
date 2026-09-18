using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.CoreTests;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// The balances a paper account starts with.
/// <para>
/// There is one place they are configured: the default asset list of the trader settings, coin by
/// coin. That has to be said per coin, because one amount for every coin cannot be right - 10.000 is
/// a sensible amount of USDT and an absurd amount of BTC, while the capital line adds every coin up
/// in USDT and 10.000 BTC would put the starting point of that line above a billion.
/// </para>
/// <para>
/// The scanner reads nothing else: an empty list means a paper account starts at nothing. A reset
/// asked for by hand (the paper-assets screen) and the start capital of an emulator run are amounts
/// of their own, handed to the traded quote coins only when that list is empty.
/// </para>
/// </summary>
[TestClass]
public class PaperAssetsStartCapitalTests : TestBase
{
    private List<CryptoPaperAssetDefault> _savedDefaults = [];
    private SortedList<string, CryptoQuoteData> _savedQuoteCoins = [];
    private readonly List<(CryptoQuoteData quote, bool fetchCandles)> _savedQuoteState = [];

    [TestInitialize]
    public void SaveSettings()
    {
        InitTestSession();

        // No default assets unless a test asks for them - the list is what most of these tests are
        // NOT about, and another test class may have left one behind.
        _savedDefaults = GlobalData.Settings.Trading.PaperAssetDefaults;
        GlobalData.Settings.Trading.PaperAssetDefaults = [];

        // The quote coins are process-static, so both the list and the field these tests write have
        // to go back the way they were - another test class reads the very same objects.
        _savedQuoteCoins = new SortedList<string, CryptoQuoteData>(GlobalData.Settings.QuoteCoins);
        _savedQuoteState.Clear();
        foreach (CryptoQuoteData quote in GlobalData.Settings.QuoteCoins.Values)
            _savedQuoteState.Add((quote, quote.FetchCandles));
    }

    [TestCleanup]
    public void RestoreSettings()
    {
        GlobalData.Settings.Trading.PaperAssetDefaults = _savedDefaults;
        foreach (var (quote, fetchCandles) in _savedQuoteState)
            quote.FetchCandles = fetchCandles;
        GlobalData.Settings.QuoteCoins = _savedQuoteCoins;
    }


    /// <summary>Two traded quote coins, so a test can tell "per quote coin" from "one coin".</summary>
    private static (CryptoQuoteData quoteUsdt, CryptoQuoteData quoteBtc) ArrangeTwoQuoteCoins(CryptoSymbol symbol)
    {
        CryptoQuoteData quoteUsdt = symbol.QuoteData!;
        quoteUsdt.FetchCandles = true;
        GlobalData.Settings.QuoteCoins[quoteUsdt.Name] = quoteUsdt;

        CryptoQuoteData quoteBtc = GlobalData.AddQuoteData("BTC");
        quoteBtc.FetchCandles = true;

        return (quoteUsdt, quoteBtc);
    }


    /// <summary>
    /// Without a default asset list, a reset hands the amount that was asked for to every traded
    /// quote coin - the amount typed in the paper-assets screen, or the one of an emulator run.
    /// </summary>
    [TestMethod]
    public void ResetGivesEveryTradedQuoteCoinTheAmountThatWasAskedFor()
    {
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);

        var (quoteUsdt, _) = ArrangeTwoQuoteCoins(symbol);

        PaperAssets.ResetAssets(GlobalData.ActiveExchange!, 5000m);

        Assert.IsTrue(GlobalData.ActiveExchange!.Data.AssetList.TryGetValue(quoteUsdt.Name, out CryptoAsset? seededUsdt),
            "the traded quote coin is seeded");
        Assert.AreEqual(5000m, seededUsdt!.Total);

        Assert.IsTrue(GlobalData.ActiveExchange!.Data.AssetList.TryGetValue("BTC", out CryptoAsset? seededBtc),
            "and so is the second one");
        Assert.AreEqual(5000m, seededBtc!.Total);
    }


    /// <summary>
    /// A fresh database with an empty default asset list hands out nothing at all. This is the path
    /// that hands out money by itself, and the only place to say how much a paper account starts
    /// with is that list.
    /// </summary>
    [TestMethod]
    public void AFreshDatabaseWithoutDefaultAssetsStartsEmpty()
    {
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        // Empties the Asset table as well, which is what makes LoadAssets seed
        DeleteAllPositionRelatedStuff(database);

        ArrangeTwoQuoteCoins(symbol);

        PaperAssets.LoadAssets(GlobalData.ActiveExchange!);

        Assert.AreEqual(0, GlobalData.ActiveExchange!.Data.AssetList.Count,
            "no default assets configured, so nothing is handed out");
    }


    /// <summary>The default asset list is what a fresh database starts with, coin by coin.</summary>
    [TestMethod]
    public void AFreshDatabaseUsesTheDefaultAssetList()
    {
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);

        var (quoteUsdt, _) = ArrangeTwoQuoteCoins(symbol);

        GlobalData.Settings.Trading.PaperAssetDefaults =
        [
            new CryptoPaperAssetDefault { Name = quoteUsdt.Name, Total = 7500m },
            new CryptoPaperAssetDefault { Name = "BTC", Total = 0.1m },
        ];

        PaperAssets.LoadAssets(GlobalData.ActiveExchange!);

        Assert.AreEqual(7500m, GlobalData.ActiveExchange!.Data.AssetList[quoteUsdt.Name].Total);
        Assert.AreEqual(0.1m, GlobalData.ActiveExchange!.Data.AssetList["BTC"].Total,
            "0,1 BTC next to 7.500 USDT cannot be said with one amount");
    }


    /// <summary>
    /// A filled default asset list IS the starting point: coin by coin, whatever amount the caller
    /// asked for, and a coin that is not traded as a quote coin at all can be in it.
    /// </summary>
    [TestMethod]
    public void TheDefaultAssetListWinsFromTheAmountThatWasAskedFor()
    {
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);

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
            "BTC is a traded quote coin but is not in the list, so it gets nothing");
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
