using CryptoScanner.Analyzers.Dbr;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Emulator.Engine;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// Every queue entry starts from the defaults of the code, not from the settings of the session
/// (09-11-2026). The settings file is where the small manual tests are done, and a test setting left
/// on went along into every queue run: the short side of the HyperLiquid 2k runs 174-184 ran with a
/// market RSI cross of 75 that no entry asked for.
/// </summary>
[TestClass]
public class QueueBaselineTests : TestBase
{
    [TestMethod]
    public void EntryStartsFromTheDefaultsAndTheSessionComesBackUntouched()
    {
        InitTestSession();
        RegisterAndEnablePlugin(new DbrPlugin());

        // A session with a test setting left on, a changed strategy setting and its own stake
        SettingsSignal sessionSignal = GlobalData.Settings.Signal;
        SettingsTrading sessionTrading = GlobalData.Settings.Trading;
        sessionSignal.Short.MarketRsiCross = new SettingsTextualMarketRsiCross { Active = true, Level = 75, Hours = 3 };
        sessionTrading.StopLossPercentage = 33m;
        DbrSettings sessionDbr = DbrPlugin.Settings;
        double defaultSizeRatio = new DbrSettings().MaxCandleSizeRatio;
        sessionDbr.MaxCandleSizeRatio = defaultSizeRatio + 7.0;
        if (!GlobalData.Settings.QuoteCoins.TryGetValue("USDT", out CryptoQuoteData? quote))
        {
            quote = new CryptoQuoteData { Name = "USDT" };
            GlobalData.Settings.QuoteCoins.Add("USDT", quote);
        }
        quote.EntryAmount = 123m;
        quote.EntryPercentage = 4.5f;

        EmulatorQueueEntry entry = new()
        {
            Short = new EmulatorSideConfig { Enabled = true, MarketRsi = [20m, 80m] },
            TradingOverrides = new() { ["StopLossPercentage"] = JsonDocument.Parse("20").RootElement },
        };

        QueueBaseline.Install();
        List<SignalGridExpander.Override> overrides = [];
        try
        {
            Assert.IsTrue(QueueBaseline.IsActive);
            overrides = QueueEntryApplier.Apply(entry);

            // What the entry says is applied, everything else is the default - not the session
            Assert.IsTrue(GlobalData.Settings.Signal.Short.MarketRsi.Active);
            Assert.AreEqual(new SettingsTextualMarketRsiCross().Active, GlobalData.Settings.Signal.Short.MarketRsiCross.Active);
            Assert.AreEqual(20m, GlobalData.Settings.Trading.StopLossPercentage);
            Assert.AreEqual(21m, GlobalData.Settings.Trading.StopLossLimitPercentage);
            Assert.IsTrue(GlobalData.Settings.Trading.Active);
            Assert.AreEqual(defaultSizeRatio, DbrPlugin.Settings.MaxCandleSizeRatio);
            Assert.AreEqual(new CryptoQuoteData { Name = "" }.EntryAmount, quote.EntryAmount);
            Assert.AreEqual(new CryptoQuoteData { Name = "" }.EntryPercentage, quote.EntryPercentage);
            Assert.AreNotSame(sessionSignal, GlobalData.Settings.Signal);
        }
        finally
        {
            SignalGridExpander.Revert(overrides);
            QueueBaseline.Restore();
        }

        // The session objects themselves are back, with the values they had
        Assert.IsFalse(QueueBaseline.IsActive);
        Assert.AreSame(sessionSignal, GlobalData.Settings.Signal);
        Assert.AreSame(sessionTrading, GlobalData.Settings.Trading);
        Assert.AreSame(sessionDbr, DbrPlugin.Settings);
        Assert.IsTrue(sessionSignal.Short.MarketRsiCross.Active);
        Assert.AreEqual(75m, sessionSignal.Short.MarketRsiCross.Level);
        Assert.AreEqual(33m, sessionTrading.StopLossPercentage);
        Assert.AreEqual(defaultSizeRatio + 7.0, sessionDbr.MaxCandleSizeRatio);
        Assert.AreEqual(123m, quote.EntryAmount);
        Assert.AreEqual(4.5f, quote.EntryPercentage);
    }


    /// <summary>
    /// A missed or doubled call must never lose the session settings: Restore without Install does
    /// nothing, and a second Install first puts the session back instead of saving the defaults of
    /// the first one as if they were the session.
    /// </summary>
    [TestMethod]
    public void RestoreWithoutInstallChangesNothingAndInstallTwiceKeepsTheSession()
    {
        InitTestSession();
        SettingsSignal sessionSignal = GlobalData.Settings.Signal;

        QueueBaseline.Restore();
        Assert.AreSame(sessionSignal, GlobalData.Settings.Signal);

        QueueBaseline.Install();
        QueueBaseline.Install();
        QueueBaseline.Restore();
        Assert.AreSame(sessionSignal, GlobalData.Settings.Signal);
    }
}
