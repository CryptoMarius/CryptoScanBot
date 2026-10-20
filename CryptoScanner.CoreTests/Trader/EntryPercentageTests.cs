using CryptoScanner.Core.Context;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;
using CryptoScanner.Emulator.Engine;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// The entry percentage of a quote coin (20-10-2026): a percentage of the whole account - cash plus
/// open positions at the current price - so the stake grows and shrinks with the account, with the
/// entry amount as the floor. Until then it was a percentage of the quote balance, which is the cash
/// outside the positions, and the asset check and the order each read a different balance.
/// </summary>
[TestClass]
public class EntryPercentageTests : TestBase
{
    private static void SetBalance(string name, decimal total)
    {
        CryptoAsset asset = PaperAssets.FindOrCreateAsset(GlobalData.ActiveExchange!, name);
        asset.Total = total;
        asset.Free = total;
        asset.Locked = 0;
    }


    /// <summary>Runs <paramref name="test"/> on a clean balance list and puts the settings back.</summary>
    private static void WithSettings(float percentage, decimal amount, bool assetManagement, Action<CryptoSymbol> test)
    {
        InitTestSession();
        GlobalData.Settings.Trading.TradeVia = CryptoTradeVia.PaperTrade;
        GlobalData.CurrentEmulatorRunId = null;
        using CryptoDatabase database = new();
        database.Open();
        CryptoSymbol symbol = CreateTestSymbol(database);
        DeleteAllPositionRelatedStuff(database);

        CryptoQuoteData quote = symbol.QuoteData!;
        float oldPercentage = quote.EntryPercentage;
        decimal oldAmount = quote.EntryAmount;
        bool oldAssetManagement = GlobalData.Settings.Trading.UseAssetManagement;
        try
        {
            GlobalData.ActiveExchange!.Data.AssetList.Clear();
            quote.EntryPercentage = percentage;
            quote.EntryAmount = amount;
            GlobalData.Settings.Trading.UseAssetManagement = assetManagement;
            test(symbol);
        }
        finally
        {
            quote.EntryPercentage = oldPercentage;
            quote.EntryAmount = oldAmount;
            GlobalData.Settings.Trading.UseAssetManagement = oldAssetManagement;
            GlobalData.ActiveExchange!.Data.AssetList.Clear();
        }
    }


    [TestMethod]
    public void The_percentage_is_taken_of_the_whole_account_including_open_positions()
    {
        WithSettings(1f, 10m, true, symbol =>
        {
            // 9000 cash and 100 TEST at 10 in an open long: the account is worth 10000
            symbol.LastPrice = 10m;
            SetBalance("USDT", 9000m);
            SetBalance(symbol.Base, 100m);

            Assert.AreEqual(100m, TradeTools.GetEntryAmount(symbol, GlobalData.ActiveExchange));
        });
    }


    [TestMethod]
    public void The_amount_is_the_floor_under_the_percentage()
    {
        WithSettings(1f, 15m, true, symbol =>
        {
            // 1% of 800 is 8, below the minimum stake of 15
            SetBalance("USDT", 800m);

            Assert.AreEqual(15m, TradeTools.GetEntryAmount(symbol, GlobalData.ActiveExchange));
        });
    }


    [TestMethod]
    public void Without_asset_management_the_amount_is_the_entry()
    {
        WithSettings(1f, 15m, false, symbol =>
        {
            SetBalance("USDT", 10000m);

            Assert.AreEqual(15m, TradeTools.GetEntryAmount(symbol, GlobalData.ActiveExchange));
        });
    }


    [TestMethod]
    public void The_asset_check_and_the_order_use_the_same_entry()
    {
        WithSettings(1f, 10m, true, symbol =>
        {
            symbol.LastPrice = 10m;
            SetBalance("USDT", 9000m);
            SetBalance(symbol.Base, 100m);

            var (_, checkedEntry, _, _) = AssetTools.CheckAvailableAssets(GlobalData.ActiveExchange!, symbol);
            Assert.AreEqual(TradeTools.GetEntryAmount(symbol, GlobalData.ActiveExchange), checkedEntry);
        });
    }


    [TestMethod]
    public void The_queue_sets_the_percentage_on_top_of_the_amount_and_reverts_both()
    {
        InitTestSession();
        CryptoQuoteData quote = GlobalData.Settings.QuoteCoins.Values.First();
        float oldPercentage = quote.EntryPercentage;
        decimal oldAmount = quote.EntryAmount;

        var entry = new EmulatorQueueEntry { EntryAmount = 15m, EntryPercentage = 0.75m };
        var overrides = SignalGridExpander.Apply(entry);
        try
        {
            Assert.AreEqual(15m, quote.EntryAmount);
            Assert.AreEqual(0.75f, quote.EntryPercentage);
        }
        finally
        {
            SignalGridExpander.Revert(overrides);
        }

        Assert.AreEqual(oldAmount, quote.EntryAmount);
        Assert.AreEqual(oldPercentage, quote.EntryPercentage);

        // The key must also survive the JSON of a queue file
        var parsed = JsonSerializer.Deserialize<EmulatorQueueEntry>("""{"Label":"x","EntryPercentage":0.75}""");
        Assert.AreEqual(0.75m, parsed!.EntryPercentage);
    }
}
