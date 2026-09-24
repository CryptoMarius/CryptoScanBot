using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Model;
using CryptoScanner.CoreTests;

namespace CryptoScanner.Core.Trader.Tests;

/// <summary>
/// Which coin the capital of a day is expressed in.
/// <para>
/// This used to be the constant "USDT" and nothing else, and every price in
/// <see cref="AssetSnapshotTools.ResolvePrice(Model.CryptoExchange, string, string)"/> comes from the
/// pair "coin + reference coin". On a market that does not trade that coin there is no such pair, so
/// every price came out zero, every value with it, and the capital line of the dashboard sat flat on
/// zero. Measured on 22-09-2026 that was four of the nineteen markets: HyperLiquid Perpetual (USDC),
/// Coinbase Spot (USD), Kraken Perpetual (USD) and Bitvavo Spot (EUR).
/// </para>
/// </summary>
[TestClass]
public class AssetSnapshotReferenceCoinTests : TestBase
{
    private string? previousDefaultQuote;
    private readonly List<(CryptoQuoteData QuoteData, bool FetchCandles, List<CryptoSymbol> SymbolList)> restore = [];

    /// <summary>Quote coins this test invented, so they can be taken out again.</summary>
    private readonly List<string> added = [];

    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        previousDefaultQuote = ExchangeBase.ExchangeOptions.DefaultQuote;

        // Every quote coin of the test settings goes back to how it was found. They are process-wide
        // and the whole suite shares them, so a test that leaves one switched on changes the answer
        // of whatever class runs next.
        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
            restore.Add((quoteData, quoteData.FetchCandles, quoteData.SymbolList));

        foreach (CryptoQuoteData quoteData in GlobalData.Settings.QuoteCoins.Values)
        {
            quoteData.FetchCandles = false;
            quoteData.SymbolList = [];
        }
    }


    [TestCleanup]
    public void Cleanup()
    {
        ExchangeBase.ExchangeOptions.DefaultQuote = previousDefaultQuote;
        // The invented ones go first: the settings are shared with every other test class, and a
        // quote coin left switched on here decides the reference coin over there.
        foreach (string name in added)
            GlobalData.Settings.QuoteCoins.Remove(name);
        added.Clear();

        foreach (var (quoteData, fetchCandles, symbolList) in restore)
        {
            quoteData.FetchCandles = fetchCandles;
            quoteData.SymbolList = symbolList;
        }
        restore.Clear();
    }


    /// <summary>A quote coin the scanner is really fetching, with symbols under it.</summary>
    private void QuoteInUse(string name, int symbolCount)
    {
        if (!GlobalData.Settings.QuoteCoins.ContainsKey(name))
            added.Add(name);

        CryptoQuoteData quoteData = GlobalData.AddQuoteData(name);
        quoteData.FetchCandles = true;
        quoteData.SymbolList = [.. Enumerable.Range(0, symbolCount)
            .Select(i => new CryptoSymbol
            {
                Exchange = GlobalData.ActiveExchange!,
                QuoteData = quoteData,
                Quote = name,
                Base = $"TEST{i}",
                Name = $"TEST{i}{name}",
                ExchangeName = $"TEST{i}{name}",
            })];
    }


    [TestMethod]
    public void The_main_coin_the_api_states_for_itself_wins()
    {
        // HyperLiquid Perpetual: one quote coin, and it is not a dollar tether
        ExchangeBase.ExchangeOptions.DefaultQuote = "USDC";
        QuoteInUse("USDC", 396);

        Assert.AreEqual("USDC", AssetSnapshotTools.ResolveReferenceCoin());
    }


    [TestMethod]
    public void A_main_coin_that_is_not_being_traded_is_ignored()
    {
        // Kucoin Spot states USDC while the settings only fetch USDT. Following the declaration
        // blindly would value the entire balance at zero there, which is the very hole this fixes.
        ExchangeBase.ExchangeOptions.DefaultQuote = "USDC";
        QuoteInUse("USDT", 854);

        Assert.AreEqual("USDT", AssetSnapshotTools.ResolveReferenceCoin());
    }


    [TestMethod]
    public void A_market_with_a_dollar_tether_keeps_it_even_next_to_a_bigger_quote()
    {
        // The history of such a market was written in USDT, so it may not switch coins halfway
        ExchangeBase.ExchangeOptions.DefaultQuote = null;
        QuoteInUse("USDT", 10);
        QuoteInUse("EUR", 500);

        Assert.AreEqual("USDT", AssetSnapshotTools.ResolveReferenceCoin());
    }


    [TestMethod]
    public void Without_a_main_coin_the_largest_quote_in_use_decides()
    {
        ExchangeBase.ExchangeOptions.DefaultQuote = null;
        QuoteInUse("EUR", 433);
        QuoteInUse("USDC", 13);

        Assert.AreEqual("EUR", AssetSnapshotTools.ResolveReferenceCoin());
    }


    [TestMethod]
    public void Nothing_in_use_falls_back_on_the_dollar_tether()
    {
        ExchangeBase.ExchangeOptions.DefaultQuote = null;

        Assert.AreEqual(AssetSnapshotTools.FallbackReferenceCoin, AssetSnapshotTools.ResolveReferenceCoin());
    }


    [TestMethod]
    public void The_cash_of_a_usdc_market_is_worth_one_of_itself()
    {
        ExchangeBase.ExchangeOptions.DefaultQuote = "USDC";
        QuoteInUse("USDC", 396);

        // The whole bug in one line: with the reference coin fixed on USDT this asked for a USDCUSDT
        // pair, HyperLiquid Perpetual does not list one, and 9.542 USDC of cash was valued at zero.
        string referenceCoin = AssetSnapshotTools.ResolveReferenceCoin();
        Assert.AreEqual(1m, AssetSnapshotTools.ResolvePrice(GlobalData.ActiveExchange!, "USDC", referenceCoin));
    }
}
