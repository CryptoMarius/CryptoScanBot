using CryptoScanner.Core.Json;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Emulator.Engine;

using System.Text.Json;
using System.Text.Json.Nodes;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// "Take over these settings" in the Results tab: the snapshot of a run laid over the current
/// settings (RunSettingsApplier.BuildMergedSettings).
/// </summary>
[TestClass]
public class RunSettingsApplierTests
{
    private static SettingsBasic CurrentSettings()
    {
        SettingsBasic settings = new();
        settings.General.ExchangeName = "Binance Perpetual";
        settings.General.ActivateExchangeName = "Binance Perpetual";
        settings.Trading.StopLossPercentage = 4m;
        settings.Trading.MaxPositionDurationDays = 21m;
        settings.Trading.DcaList =
        [
            new CryptoDcaEntry { Percentage = 2m, Factor = 200m },
            new CryptoDcaEntry { Percentage = 4m, Factor = 400m },
            new CryptoDcaEntry { Percentage = 8m, Factor = 800m },
        ];
        settings.QuoteCoins["USDT"] = new CryptoQuoteData { Name = "USDT", EntryAmount = 15m, FetchCandles = true };
        settings.QuoteCoins["USDC"] = new CryptoQuoteData { Name = "USDC", EntryAmount = 0m, FetchCandles = true };
        return settings;
    }

    private static string Json(SettingsBasic settings) => JsonSerializer.Serialize(settings, JsonTools.JsonSerializerIndented);

    private static SettingsBasic Merge(Action<JsonObject> editSnapshot)
    {
        SettingsBasic run = CurrentSettings();
        run.Trading.StopLossPercentage = 5m;
        run.Trading.DcaList = [new CryptoDcaEntry { Percentage = 3m, Factor = 200m }, new CryptoDcaEntry { Percentage = 6m, Factor = 200m }];
        run.QuoteCoins["USDT"].EntryAmount = 50m;
        JsonObject snapshot = JsonNode.Parse(Json(run))!.AsObject();
        editSnapshot(snapshot);

        string? merged = RunSettingsApplier.BuildMergedSettings(Json(CurrentSettings()), snapshot.ToJsonString(), out string reason);
        Assert.IsNotNull(merged, reason);
        return JsonSerializer.Deserialize<SettingsBasic>(merged, JsonTools.DeSerializerOptions)!;
    }


    [TestMethod]
    public void WhatTheRunStored_Wins()
    {
        SettingsBasic result = Merge(_ => { });
        Assert.AreEqual(5m, result.Trading.StopLossPercentage);
        Assert.AreEqual(50m, result.QuoteCoins["USDT"].EntryAmount);
    }


    /// <summary>
    /// A setting a newer build added is missing from an older snapshot. It keeps its current value
    /// instead of falling back to the default.
    /// </summary>
    [TestMethod]
    public void ASettingTheRunDidNotStore_KeepsItsCurrentValue()
    {
        SettingsBasic result = Merge(s => s["Trading"]!.AsObject().Remove("MaxPositionDurationDays"));
        Assert.AreEqual(21m, result.Trading.MaxPositionDurationDays, "current value, not the default 0");
    }


    /// <summary>A list is taken over whole: two levels stay two, the current third level does not survive.</summary>
    [TestMethod]
    public void AList_IsReplacedWhole()
    {
        SettingsBasic result = Merge(_ => { });
        Assert.AreEqual(2, result.Trading.DcaList.Count);
        Assert.AreEqual(3m, result.Trading.DcaList[0].Percentage);
        Assert.AreEqual(6m, result.Trading.DcaList[1].Percentage);
    }


    /// <summary>Keyed collections merge per key: a quote coin the run did not store stays as it is.</summary>
    [TestMethod]
    public void AQuoteCoinTheRunDidNotStore_StaysAsItIs()
    {
        SettingsBasic result = Merge(s => s["QuoteCoins"]!.AsObject().Remove("USDC"));
        Assert.IsTrue(result.QuoteCoins.ContainsKey("USDC"));
        Assert.AreEqual(50m, result.QuoteCoins["USDT"].EntryAmount);
    }


    /// <summary>The emulator is bound to its exchange at startup; a snapshot does not move it.</summary>
    [TestMethod]
    public void TheExchange_StaysTheCurrentOne()
    {
        SettingsBasic result = Merge(s =>
        {
            s["General"]!["ExchangeName"] = "Bybit Perpetual";
            s["General"]!["ActivateExchangeName"] = "Bybit Perpetual";
        });
        Assert.AreEqual("Binance Perpetual", result.General.ExchangeName);
        Assert.AreEqual("Binance Perpetual", result.General.ActivateExchangeName);
    }


    [TestMethod]
    public void ASnapshotThatIsNotJson_IsRefused()
    {
        string? merged = RunSettingsApplier.BuildMergedSettings(Json(CurrentSettings()), "{ not json", out string reason);
        Assert.IsNull(merged);
        StringAssert.Contains(reason, "not valid JSON");
    }
}
