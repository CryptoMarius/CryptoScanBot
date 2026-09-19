using CryptoScanner.Analyzers.Dbr;
using CryptoScanner.Analyzers.Mac;
using CryptoScanner.Core.Core;
using CryptoScanner.Emulator.Engine;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// A strategy that brings its OWN entry conditions cannot be steered through the trading overrides
/// of a queue entry. SignalBase.ResolveEntryConditions prefers the strategy's set over
/// GlobalData.Settings.Trading.EntryConditions, so an entry that switches the ma200 condition on
/// there changes nothing for that strategy - and nothing says so: the run completes, the stored
/// settings show the condition as ON, and the result is bit-identical to the run without it.
/// <para>
/// That is not a thought experiment. Four runs of the batch of 17-09-2026 (TX19 to TX22) were meant
/// to measure the ma200 condition on MAC and came back identical to TX1 to TX4, down to the cent.
/// Three strategies have their own set - mac, bbsqueeze and kumosqueeze - and for those the
/// condition has to travel through the SIGNAL overrides, on the strategy's own section.
/// </para>
/// </summary>
[TestClass]
public class StrategyOwnEntryConditionsTests : TestBase
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        // The signal overrides find a plugin section by strategy name through PluginManager, so the
        // plugin has to be registered before "mac" resolves to anything at all.
        RegisterAndEnablePlugin(new MacPlugin());

        // A strategy WITHOUT its own set, to prove the refusal below does not catch everything.
        RegisterAndEnablePlugin(new DbrPlugin());
    }


    private static EmulatorQueueEntry TradingOverrideEntry(string algorithm, string json)
    {
        return new EmulatorQueueEntry
        {
            Algorithm = algorithm,
            TradingOverrides = new()
            {
                ["EntryConditions.CheckPriceAboveMa200"] = JsonDocument.Parse(json).RootElement,
            },
        };
    }


    [TestMethod]
    public void MacBringsItsOwnEntryConditions()
    {
        // The whole trap rests on this being non-null: that is what makes the resolver prefer it.
        Assert.IsNotNull(new MacSettings().EntryConditions,
            "MAC has its own entry conditions on purpose - see the note on its constructor");
    }


    [TestMethod]
    public void TradingOverridesDoNotReachTheStrategysOwnConditions()
    {
        InitTestSession();

        var entry = new EmulatorQueueEntry
        {
            TradingOverrides = new()
            {
                ["EntryConditions.CheckPriceAboveMa200"] = JsonDocument.Parse("true").RootElement,
            },
        };

        var overrides = SignalGridExpander.Apply(entry);
        try
        {
            Assert.IsTrue(GlobalData.Settings.Trading.EntryConditions.CheckPriceAboveMa200,
                "the global set is what the override reaches");
            Assert.IsFalse(MacPlugin.Settings.EntryConditions!.CheckPriceAboveMa200,
                "and the strategy's own set - the one that is actually read - stays untouched");
        }
        finally
        {
            SignalGridExpander.Revert(overrides);
        }
    }


    [TestMethod]
    public void SignalOverridesOnTheStrategyDoReachThem()
    {
        InitTestSession();

        var entry = new EmulatorQueueEntry
        {
            SignalOverrides = new()
            {
                ["mac"] = new()
                {
                    ["EntryConditions.CheckPriceAboveMa200"] = JsonDocument.Parse("true").RootElement,
                    ["EntryConditions.Ma200ConfirmationCandles"] = JsonDocument.Parse("3").RootElement,
                },
            },
        };

        var overrides = SignalGridExpander.Apply(entry);
        try
        {
            Assert.IsTrue(MacPlugin.Settings.EntryConditions!.CheckPriceAboveMa200);
            Assert.AreEqual(3, MacPlugin.Settings.EntryConditions.Ma200ConfirmationCandles);
        }
        finally
        {
            SignalGridExpander.Revert(overrides);
        }

        Assert.IsFalse(MacPlugin.Settings.EntryConditions!.CheckPriceAboveMa200,
            "and it has to go back afterwards, or every later run in the batch measures it too");
        Assert.AreEqual(0, MacPlugin.Settings.EntryConditions.Ma200ConfirmationCandles);
    }


    /// <summary>
    /// The refusal itself: an entry that switches a condition ON for a strategy that brings its own
    /// set is stopped before it runs, with the working spelling in the message.
    /// </summary>
    [TestMethod]
    public void SwitchingAConditionOnForSuchAStrategyIsRefused()
    {
        InitTestSession();

        string? reason = SignalGridExpander.Validate(TradingOverrideEntry("mac", "true"));

        Assert.IsNotNull(reason, "this entry measures nothing and has to say so before the batch starts");
        StringAssert.Contains(reason, "SignalOverrides");
        StringAssert.Contains(reason, "mac");

        // And the same question is asked again before anything is set.
        Assert.ThrowsExactly<NotSupportedException>(
            () => SignalGridExpander.Apply(TradingOverrideEntry("mac", "true")));
    }


    /// <summary>
    /// Switching one OFF is what most entries do to pin the setup, and it is exactly what the
    /// strategy's own set already says - so it stays silent.
    /// </summary>
    [TestMethod]
    public void SwitchingItOffIsLeftAlone()
    {
        InitTestSession();

        Assert.IsNull(SignalGridExpander.Validate(TradingOverrideEntry("mac", "false")));
    }


    [TestMethod]
    public void AStrategyWithoutItsOwnSetIsNotAffected()
    {
        InitTestSession();

        Assert.IsNull(SignalGridExpander.Validate(TradingOverrideEntry("dbr", "true")),
            "dbr reads the global set, so the override does reach it");
    }
}
