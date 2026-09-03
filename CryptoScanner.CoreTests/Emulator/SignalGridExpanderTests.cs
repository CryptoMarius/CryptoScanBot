using CryptoScanner.Core.Core;
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

        double original = GlobalData.Settings.Signal.AnalysisMinBandRangeIndex;

        var overrides = SignalGridExpander.Apply(EntryWithOverride("Signal", "AnalysisMinBandRangeIndex", "3.5"));
        try
        {
            Assert.AreEqual(3.5, GlobalData.Settings.Signal.AnalysisMinBandRangeIndex);
        }
        finally
        {
            SignalGridExpander.Revert(overrides);
        }

        Assert.AreEqual(original, GlobalData.Settings.Signal.AnalysisMinBandRangeIndex);
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

        double original = GlobalData.Settings.Signal.AnalysisMinBandRangeIndex;

        // The dictionary preserves insertion order, so the good override is applied first and the
        // retired one throws after it.
        var entry = new EmulatorQueueEntry
        {
            SignalOverrides = new()
            {
                ["Signal"] = new()
                {
                    ["AnalysisMinBandRangeIndex"] = JsonDocument.Parse("3.5").RootElement,
                    ["EntryConditions.EntryWaitForPatterns"] = JsonDocument.Parse("""["Hammer"]""").RootElement,
                },
            },
        };

        Assert.ThrowsExactly<NotSupportedException>(() => SignalGridExpander.Apply(entry));
        Assert.AreEqual(original, GlobalData.Settings.Signal.AnalysisMinBandRangeIndex);
    }
}
