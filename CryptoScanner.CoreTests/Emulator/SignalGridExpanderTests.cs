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
}
