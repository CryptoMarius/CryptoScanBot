using CryptoScanner.Core.Settings;
using CryptoScanner.Emulator.Engine;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The market state filter per side from a queue entry: [min, max] switches it on with that score
/// band, [] switches it off, MarketStateInterval only changes the BTC interval, and leaving
/// the keys out keeps what the settings say.
/// </summary>
[TestClass]
public class EmulatorSideConfigMarketStateTests : TestBase
{
    [TestInitialize]
    public void Setup() => InitTestSession();

    [TestMethod]
    public void Apply_SetsTheBandPerSide()
    {
        string json = """
            {"Long": {"MarketState": [60, 100], "MarketStateInterval": "2h"}, "Short": {"MarketState": []}}
            """;
        EmulatorQueueEntry entry = JsonSerializer.Deserialize<EmulatorQueueEntry>(json)!;

        SettingsTextual longSignal = new(), longTrading = new(), shortSignal = new(), shortTrading = new();
        shortSignal.MarketState.Active = true;
        EmulatorSideConfig.Apply(EmulatorSideConfig.Resolve(entry.Long, null), longSignal, longTrading);
        EmulatorSideConfig.Apply(EmulatorSideConfig.Resolve(entry.Short, null), shortSignal, shortTrading);

        Assert.IsTrue(longSignal.MarketState.Active);
        Assert.AreEqual(60m, longSignal.MarketState.Minimum);
        Assert.AreEqual(100m, longSignal.MarketState.Maximum);
        Assert.AreEqual("2h", longSignal.MarketState.Interval);
        Assert.IsFalse(longTrading.MarketState.Active, "the queue only sets the analyzer side");
        Assert.IsFalse(shortSignal.MarketState.Active, "an empty list should switch the filter off");
    }

    [TestMethod]
    public void Apply_ReplacesTheObjectSoTheQueueCanPutTheOriginalBack()
    {
        SettingsTextual signal = new(), trading = new();
        SettingsTextualMarketState original = signal.MarketState;

        EmulatorSideConfig.Apply(new EmulatorSideConfig { MarketState = [60m, 100m] }, signal, trading);

        Assert.AreNotSame(original, signal.MarketState);
        Assert.IsFalse(original.Active, "the original settings object was changed");
    }

    [TestMethod]
    public void Apply_WithoutTheKey_LeavesTheSettings()
    {
        SettingsTextual signal = new(), trading = new();
        EmulatorSideConfig.Apply(EmulatorSideConfig.Resolve(new EmulatorSideConfig { Intervals = ["5m"] }, null), signal, trading);

        Assert.IsFalse(signal.MarketState.Active);
        Assert.AreEqual("4h", signal.MarketState.Interval);
        Assert.AreEqual(-100m, signal.MarketState.Minimum);
    }

    [TestMethod]
    public void Apply_ThreeValuesOrAnUnknownIntervalIsRefused()
    {
        SettingsTextual signal = new(), trading = new();
        Assert.ThrowsExactly<NotSupportedException>(() =>
            EmulatorSideConfig.Apply(new EmulatorSideConfig { MarketState = [10m, 20m, 30m] }, signal, trading));
        Assert.ThrowsExactly<NotSupportedException>(() =>
            EmulatorSideConfig.Apply(new EmulatorSideConfig { MarketStateInterval = "7h" }, signal, trading));
    }

    [TestMethod]
    public void Resolve_MirrorsTheBandFromTheOtherSide()
    {
        EmulatorSideConfig resolved = EmulatorSideConfig.Resolve(null, new EmulatorSideConfig { MarketState = [60m, 100m] });

        CollectionAssert.AreEqual(new[] { 60m, 100m }, resolved.MarketState);
    }
}
