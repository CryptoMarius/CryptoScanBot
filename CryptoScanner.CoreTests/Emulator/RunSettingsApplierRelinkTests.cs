using CryptoScanner.Core.Core;
using CryptoScanner.Core.Exchange;
using CryptoScanner.Core.Json;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Core.Trader;
using CryptoScanner.Emulator.Engine;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// After "Take over these settings" every symbol has to hang under the quote coin of the settings
/// that were just loaded (RunSettingsApplier.RelinkSymbolsToQuoteCoins). On 29-10-2026 they did not:
/// the reload left the symbols on the old CryptoQuoteData and the new ones without symbols, so the
/// capital snapshots of HyperLiquid Perpetual fell back on USDT and run 327 was valued at zero.
/// </summary>
[TestClass]
public class RunSettingsApplierRelinkTests : TestBase
{
    private SettingsBasic? previousSettings;
    private string? previousDefaultQuote;


    [TestInitialize]
    public void Setup()
    {
        InitTestSession();
        previousSettings = GlobalData.Settings;
        previousDefaultQuote = ExchangeBase.ExchangeOptions.DefaultQuote;
    }


    [TestCleanup]
    public void Cleanup()
    {
        // The settings are shared with every other test class
        GlobalData.Settings = previousSettings!;
        ExchangeBase.ExchangeOptions.DefaultQuote = previousDefaultQuote;
    }


    /// <summary>The settings as the takeover reads them back from disk: new objects throughout.</summary>
    private static SettingsBasic Reloaded(SettingsBasic settings)
    {
        string text = JsonSerializer.Serialize(settings, JsonTools.JsonSerializerIndented);
        return JsonSerializer.Deserialize<SettingsBasic>(text, JsonTools.DeSerializerOptions)!;
    }


    [TestMethod]
    public void The_symbols_hang_under_the_quote_coin_of_the_reloaded_settings()
    {
        // A market of its own, so the symbols of the shared test exchange are left alone
        var exchange = new CryptoScanner.Core.Model.CryptoExchange { Name = "Relink test" };
        CryptoQuoteData before = new() { Name = "USDC", FetchCandles = true };
        CryptoSymbol symbol = new()
        {
            Exchange = exchange,
            QuoteData = before,
            Quote = "USDC",
            Base = "RELINK",
            Name = "RELINKUSDC.PERP",
            ExchangeName = "RELINK",
            Status = 1,
        };
        exchange.SymbolListName.Add(symbol.Name, symbol);

        GlobalData.Settings = Reloaded(previousSettings!);
        CryptoQuoteData after = GlobalData.AddQuoteData("USDC");
        after.FetchCandles = true;
        ExchangeBase.ExchangeOptions.DefaultQuote = "USDC";

        RunSettingsApplier.RelinkSymbolsToQuoteCoins(exchange);

        Assert.AreSame(after, symbol.QuoteData);
        CollectionAssert.Contains(after.SymbolList, symbol);
        Assert.AreEqual("USDC", AssetSnapshotTools.ResolveReferenceCoin());
    }
}
