using CryptoScanner.Core.Settings;
using CryptoScanner.Emulator.Engine;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The market breadth condition per side from a queue entry. It cannot be reached through a dotted
/// path in SignalOverrides (Barometer is a field, the path walker only follows properties and skips
/// silently), so a run asking for "long only while 0-40% of the coins rose" ran without any filter.
/// </summary>
[TestClass]
public class EmulatorSideConfigBreadthTests
{
    [TestMethod]
    public void Apply_SetsBreadthPerSide()
    {
        string json = """
            {"Long": {"BarometerBreadthActive": true, "BarometerBreadthMinimum": 0, "BarometerBreadthMaximum": 40},
             "Short": {"BarometerBreadthActive": true, "BarometerBreadthMinimum": 60, "BarometerBreadthMaximum": 100}}
            """;
        EmulatorQueueEntry entry = JsonSerializer.Deserialize<EmulatorQueueEntry>(json)!;

        SettingsTextual longSignal = new(), longTrading = new(), shortSignal = new(), shortTrading = new();
        EmulatorSideConfig.Apply(EmulatorSideConfig.Resolve(entry.Long, null), longSignal, longTrading);
        EmulatorSideConfig.Apply(EmulatorSideConfig.Resolve(entry.Short, null), shortSignal, shortTrading);

        Assert.IsTrue(longSignal.Barometer.BreadthActive);
        Assert.AreEqual(0m, longSignal.Barometer.BreadthMinimum);
        Assert.AreEqual(40m, longSignal.Barometer.BreadthMaximum);
        Assert.IsTrue(shortSignal.Barometer.BreadthActive);
        Assert.AreEqual(60m, shortSignal.Barometer.BreadthMinimum);
        Assert.AreEqual(100m, shortSignal.Barometer.BreadthMaximum);
    }

    [TestMethod]
    public void Apply_WithoutBreadth_LeavesDefaults()
    {
        SettingsTextual signal = new(), trading = new();
        EmulatorSideConfig.Apply(EmulatorSideConfig.Resolve(new EmulatorSideConfig { Intervals = ["5m"] }, null), signal, trading);

        Assert.IsFalse(signal.Barometer.BreadthActive);
        Assert.AreEqual(0m, signal.Barometer.BreadthMinimum);
        Assert.AreEqual(100m, signal.Barometer.BreadthMaximum);
    }

    [TestMethod]
    public void Resolve_MirrorsBreadthFromOtherSide()
    {
        EmulatorSideConfig source = new() { BarometerBreadthActive = true, BarometerBreadthMinimum = 10, BarometerBreadthMaximum = 30 };
        EmulatorSideConfig resolved = EmulatorSideConfig.Resolve(null, source);

        Assert.AreEqual(true, resolved.BarometerBreadthActive);
        Assert.AreEqual(10m, resolved.BarometerBreadthMinimum);
        Assert.AreEqual(30m, resolved.BarometerBreadthMaximum);
    }
}
