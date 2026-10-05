using CryptoScanner.Analyzers.Dbr;
using CryptoScanner.Analyzers.Sbm;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Emulator.Engine;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// A queue entry can override settings per run. Everything except a list worked; an array in the
/// queue file hit the NotSupportedException at the end of ConvertJsonElement. That made the zone
/// interval lists (ZonesDlz/ZonesFvg/ZonesSmc.IntervalList) reachable only through the settings
/// file, and an empty list there costs a full run: no zones are calculated and the strategy cannot
/// produce a single signal. Twelve overnight runs finished that way before anyone noticed.
/// </summary>
[TestClass]
public class SignalGridExpanderTests : TestBase
{
    private static EmulatorQueueEntry EntryWithOverride(string section, string property, string json)
    {
        return new EmulatorQueueEntry
        {
            SignalOverrides = new()
            {
                [section] = new()
                {
                    [property] = JsonDocument.Parse(json).RootElement,
                },
            },
        };
    }


    [TestMethod]
    public void ListOverrideIsAppliedAndReverted()
    {
        InitTestSession();

        var settings = GlobalData.Settings.Signal.ZonesDlz;
        List<string> original = settings.IntervalList;

        var overrides = SignalGridExpander.Apply(EntryWithOverride("ZonesDlz", "IntervalList", """["1h","4h"]"""));
        try
        {
            CollectionAssert.AreEqual(new List<string> { "1h", "4h" }, settings.IntervalList);
        }
        finally
        {
            SignalGridExpander.Revert(overrides);
        }

        Assert.AreSame(original, settings.IntervalList);
    }


    /// <summary>The scalar path must keep working — that is what every existing queue entry uses.</summary>
    [TestMethod]
    public void ScalarOverrideStillWorks()
    {
        InitTestSession();

        float original = GlobalData.Settings.Signal.AnalysisEffectivePercentage;

        var overrides = SignalGridExpander.Apply(EntryWithOverride("Signal", "AnalysisEffectivePercentage", "3.5"));
        try
        {
            Assert.AreEqual(3.5f, GlobalData.Settings.Signal.AnalysisEffectivePercentage);
        }
        finally
        {
            SignalGridExpander.Revert(overrides);
        }

        Assert.AreEqual(original, GlobalData.Settings.Signal.AnalysisEffectivePercentage);
    }


    private static EmulatorQueueEntry EntryWithTradingOverride(string property, string json)
    {
        return new EmulatorQueueEntry
        {
            TradingOverrides = new()
            {
                [property] = JsonDocument.Parse(json).RootElement,
            },
        };
    }


    /// <summary>
    /// An entry that asks for a setting the code no longer has must be recognisable BEFORE the batch
    /// starts. On 03-09-2026 it was not: entry 26 of 54 asked for the retired EntryWaitForPatterns,
    /// the exception left Apply unhandled and took the whole process down at 03:45, and the 28
    /// entries behind it never ran.
    /// </summary>
    [TestMethod]
    public void ValidateNamesTheRetiredSetting()
    {
        InitTestSession();

        var entry = EntryWithTradingOverride(
            "EntryConditions.EntryWaitForPatterns", """["Hammer","Harami"]""");

        string? reason = SignalGridExpander.Validate(entry);

        Assert.IsNotNull(reason);
        StringAssert.Contains(reason, "EntryWaitForPatterns");
    }


    /// <summary>
    /// Switching a retired setting OFF stays silent: an empty list is what the entry would have had
    /// with the setting still in place, so nothing is lost by ignoring it. All 54 entries of the
    /// batch carried the key; only the 12 that asked for shapes were unusable.
    /// </summary>
    [TestMethod]
    public void ValidateAcceptsARetiredSettingThatIsOff()
    {
        InitTestSession();

        Assert.IsNull(SignalGridExpander.Validate(
            EntryWithTradingOverride("EntryConditions.EntryWaitForPatterns", "[]")));
        Assert.IsNull(SignalGridExpander.Validate(
            EntryWithTradingOverride("EntryConditions.EntryMaxAdversePercentage", "2.5")));
    }


    /// <summary>
    /// A signal-section override is checked too, not just the trading ones - the loop that reads
    /// them is a different one.
    /// </summary>
    [TestMethod]
    public void ValidateAlsoChecksTheSignalOverrides()
    {
        InitTestSession();

        var entry = EntryWithOverride("Signal", "EntryConditions.EntryPatternShape", """{"MinWickPercentage":50}""");

        Assert.IsNotNull(SignalGridExpander.Validate(entry));
    }


    /// <summary>
    /// When Apply does throw, the overrides it had already set must be put back. Otherwise a
    /// half-applied entry leaks into every later run of the batch and silently measures something
    /// nobody asked for.
    /// </summary>
    [TestMethod]
    public void AFailingApplyLeavesNoSettingsBehind()
    {
        InitTestSession();

        float original = GlobalData.Settings.Signal.AnalysisEffectivePercentage;

        // The dictionary preserves insertion order, so the good override is applied first and the
        // retired one throws after it.
        var entry = new EmulatorQueueEntry
        {
            SignalOverrides = new()
            {
                ["Signal"] = new()
                {
                    ["AnalysisEffectivePercentage"] = JsonDocument.Parse("3.5").RootElement,
                    ["EntryConditions.EntryWaitForPatterns"] = JsonDocument.Parse("""["Hammer"]""").RootElement,
                },
            },
        };

        Assert.ThrowsExactly<NotSupportedException>(() => SignalGridExpander.Apply(entry));
        Assert.AreEqual(original, GlobalData.Settings.Signal.AnalysisEffectivePercentage);
    }

    /// <summary>
    /// The two candle limits of dbr have to be reachable from a queue entry, because a path that
    /// does not resolve is skipped without a word - a run then quietly measures the strategy
    /// WITHOUT the limit while its label says it has one.
    /// </summary>
    [TestMethod]
    public void DbrCandleLimitsAreReachableFromAQueueEntry()
    {
        InitTestSession();
        RegisterAndEnablePlugin(new DbrPlugin());

        double originalSize = DbrPlugin.Settings.MaxCandleSizeRatio;
        double originalVolume = DbrPlugin.Settings.MaxCandleVolumeRatio;

        var entry = new EmulatorQueueEntry
        {
            SignalOverrides = new()
            {
                ["dbr"] = new()
                {
                    ["MaxCandleSizeRatio"] = JsonDocument.Parse("5.0").RootElement,
                    ["MaxCandleVolumeRatio"] = JsonDocument.Parse("4.0").RootElement,
                },
            },
        };

        var overrides = SignalGridExpander.Apply(entry);
        try
        {
            Assert.AreEqual(5.0, DbrPlugin.Settings.MaxCandleSizeRatio);
            Assert.AreEqual(4.0, DbrPlugin.Settings.MaxCandleVolumeRatio);
        }
        finally
        {
            SignalGridExpander.Revert(overrides);
        }

        Assert.AreEqual(originalSize, DbrPlugin.Settings.MaxCandleSizeRatio);
        Assert.AreEqual(originalVolume, DbrPlugin.Settings.MaxCandleVolumeRatio);
    }

    /// <summary>
    /// The stake of one entry can be set per run, and is put back afterwards.
    /// <para>
    /// The putting back is the whole point: it lives in Settings.QuoteCoins, which is shared by the
    /// live scanner and the settings file. A run that left its own stake standing would change
    /// every run behind it and the user's own configuration with it, and nothing on screen would
    /// say so.
    /// </para>
    /// </summary>
    [TestMethod]
    public void EntryAmountIsAppliedToEveryQuoteAndReverted()
    {
        InitTestSession();

        if (GlobalData.Settings.QuoteCoins.Count == 0)
        {
            GlobalData.Settings.QuoteCoins.Add("USDT", new CryptoQuoteData
            {
                Name = "USDT",
                EntryAmount = 15m,
                EntryPercentage = 2.5f,
            });
        }

        var voor = GlobalData.Settings.QuoteCoins.Values
            .Select(q => (q.Name, q.EntryAmount, q.EntryPercentage)).ToList();

        var entry = new EmulatorQueueEntry { EntryAmount = 250m };
        var overrides = SignalGridExpander.Apply(entry);
        try
        {
            foreach (var quote in GlobalData.Settings.QuoteCoins.Values)
            {
                Assert.AreEqual(250m, quote.EntryAmount, $"{quote.Name} kreeg de inzet niet");
                // the percentage has to go, or the amount is never read at all
                Assert.AreEqual(0f, quote.EntryPercentage, $"{quote.Name} houdt zijn percentage");
            }
        }
        finally
        {
            SignalGridExpander.Revert(overrides);
        }

        foreach (var (naam, bedrag, percentage) in voor)
        {
            var quote = GlobalData.Settings.QuoteCoins[naam];
            Assert.AreEqual(bedrag, quote.EntryAmount, $"{naam} kreeg zijn inzet niet terug");
            Assert.AreEqual(percentage, quote.EntryPercentage, $"{naam} kreeg zijn percentage niet terug");
        }
    }


    /// <summary>Without the field nothing is touched - an older queue file must not change.</summary>
    [TestMethod]
    public void WithoutEntryAmountNothingChanges()
    {
        InitTestSession();

        if (GlobalData.Settings.QuoteCoins.Count == 0)
            GlobalData.Settings.QuoteCoins.Add("USDT", new CryptoQuoteData { Name = "USDT", EntryAmount = 15m });

        var voor = GlobalData.Settings.QuoteCoins.Values.Select(q => q.EntryAmount).ToList();

        var overrides = SignalGridExpander.Apply(new EmulatorQueueEntry());
        SignalGridExpander.Revert(overrides);

        var na = GlobalData.Settings.QuoteCoins.Values.Select(q => q.EntryAmount).ToList();
        CollectionAssert.AreEqual(voor, na);
    }


    /// <summary>
    /// A section that resolves to nothing used to be skipped without a word. On 03-10-2026 queue
    /// files carried their overrides under "sbm1", "stobb.multi" and "choch.secondary" - names of
    /// sub-strategies, not of the plugins owning the settings - and those runs would have measured
    /// the defaults. Validate has to name the plugin section that does work.
    /// </summary>
    [TestMethod]
    public void ValidateRefusesASubStrategyNameAndNamesThePlugin()
    {
        InitTestSession();
        RegisterAndEnablePlugin(new SbmPlugin());

        string? reason = SignalGridExpander.Validate(EntryWithOverride("sbm1", "BBMinPercentage", "1.5"));

        Assert.IsNotNull(reason);
        StringAssert.Contains(reason, "\"sbm1\"");
        StringAssert.Contains(reason, "\"sbm\"");
    }


    /// <summary>Apply refuses it too, for the callers that do not validate first.</summary>
    [TestMethod]
    public void ApplyRefusesASubStrategyName()
    {
        InitTestSession();
        RegisterAndEnablePlugin(new SbmPlugin());

        Assert.ThrowsExactly<NotSupportedException>(
            () => SignalGridExpander.Apply(EntryWithOverride("sbm1", "BBMinPercentage", "1.5")));
    }


    /// <summary>A name nobody claims at all is refused as well, with its own explanation.</summary>
    [TestMethod]
    public void ValidateRefusesAnUnknownSection()
    {
        InitTestSession();

        string? reason = SignalGridExpander.Validate(EntryWithOverride("nosuchsection", "Anything", "1"));

        Assert.IsNotNull(reason);
        StringAssert.Contains(reason, "\"nosuchsection\"");
        StringAssert.Contains(reason, "matches no settings section");
    }


    /// <summary>The sections that do resolve - "Signal", a SettingsSignal field, a plugin name - stay accepted.</summary>
    [TestMethod]
    public void ValidateAcceptsTheKnownSections()
    {
        InitTestSession();
        RegisterAndEnablePlugin(new SbmPlugin());

        Assert.IsNull(SignalGridExpander.Validate(EntryWithOverride("Signal", "AnalysisEffectivePercentage", "3.5")));
        Assert.IsNull(SignalGridExpander.Validate(EntryWithOverride("ZonesDlz", "IntervalList", """["1h"]""")));
        Assert.IsNull(SignalGridExpander.Validate(EntryWithOverride("sbm", "BBMinPercentage", "1.5")));
    }
}
