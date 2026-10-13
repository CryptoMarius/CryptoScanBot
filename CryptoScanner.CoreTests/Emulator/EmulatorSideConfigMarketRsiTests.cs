using CryptoScanner.Core.Settings;
using CryptoScanner.Emulator.Engine;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The market RSI band per side from a queue entry: [min, max] switches it on, [] switches it off,
/// and leaving the key out keeps what the settings say.
/// </summary>
[TestClass]
public class EmulatorSideConfigMarketRsiTests
{
    [TestMethod]
    public void Apply_SetsTheBandPerSide()
    {
        string json = """
            {"Long": {"MarketRsi": [45, 55]}, "Short": {"MarketRsi": []}}
            """;
        EmulatorQueueEntry entry = JsonSerializer.Deserialize<EmulatorQueueEntry>(json)!;

        SettingsTextual longSignal = new(), longTrading = new(), shortSignal = new(), shortTrading = new();
        shortSignal.MarketRsi.Active = true;
        EmulatorSideConfig.Apply(EmulatorSideConfig.Resolve(entry.Long, null), longSignal, longTrading);
        EmulatorSideConfig.Apply(EmulatorSideConfig.Resolve(entry.Short, null), shortSignal, shortTrading);

        Assert.IsTrue(longSignal.MarketRsi.Active);
        Assert.AreEqual(45m, longSignal.MarketRsi.Minimum);
        Assert.AreEqual(55m, longSignal.MarketRsi.Maximum);
        Assert.IsFalse(longTrading.MarketRsi.Active, "the queue only sets the analyzer side");
        Assert.IsFalse(shortSignal.MarketRsi.Active, "an empty list should switch the filter off");
    }

    [TestMethod]
    public void Apply_ReplacesTheObjectSoTheQueueCanPutTheOriginalBack()
    {
        SettingsTextual signal = new(), trading = new();
        SettingsTextualMarketRsi original = signal.MarketRsi;

        EmulatorSideConfig.Apply(new EmulatorSideConfig { MarketRsi = [40m, 60m] }, signal, trading);

        Assert.AreNotSame(original, signal.MarketRsi);
        Assert.IsFalse(original.Active, "the original settings object was changed");
    }

    [TestMethod]
    public void Apply_WithoutTheKey_LeavesTheSettings()
    {
        SettingsTextual signal = new(), trading = new();
        EmulatorSideConfig.Apply(EmulatorSideConfig.Resolve(new EmulatorSideConfig { Intervals = ["5m"] }, null), signal, trading);

        Assert.IsFalse(signal.MarketRsi.Active);
        Assert.AreEqual(45m, signal.MarketRsi.Minimum);
        Assert.AreEqual(55m, signal.MarketRsi.Maximum);
    }

    [TestMethod]
    public void Apply_ThreeValuesIsRefused()
    {
        SettingsTextual signal = new(), trading = new();
        Assert.ThrowsExactly<NotSupportedException>(() =>
            EmulatorSideConfig.Apply(new EmulatorSideConfig { MarketRsi = [40m, 50m, 60m] }, signal, trading));
    }

    [TestMethod]
    public void Resolve_MirrorsTheBandFromTheOtherSide()
    {
        EmulatorSideConfig resolved = EmulatorSideConfig.Resolve(null, new EmulatorSideConfig { MarketRsi = [45m, 55m] });

        CollectionAssert.AreEqual(new[] { 45m, 55m }, resolved.MarketRsi);
    }
}
