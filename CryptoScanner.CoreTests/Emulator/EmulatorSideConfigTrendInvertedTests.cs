using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Settings;
using CryptoScanner.Emulator.Engine;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// The interval trend filter against the side (IntervalTrend.Inverted, open point 51) from a queue
/// entry. Until 12-10-2026 a queue file had no key for it, so a dbr run "with the trend filter
/// against the trend" could not be described at all.
/// </summary>
[TestClass]
public class EmulatorSideConfigTrendInvertedTests
{
    [ClassInitialize]
    public static void ClassInit(TestContext _)
    {
        if (GlobalData.IntervalList.Count > 0)
            return;

        int id = 0;
        foreach (CryptoInterval interval in CryptoInterval.CreateStandardIntervalList())
        {
            interval.Id = id++;
            GlobalData.IntervalList.Add(interval);
            GlobalData.IntervalListId.Add(interval.Id, interval);
            GlobalData.IntervalListPeriodName.Add(interval.Name, interval);
            GlobalData.IntervalListPeriod.Add(interval.IntervalPeriod, interval);
        }
    }

    [TestMethod]
    public void Apply_InvertedAsksTheTrendAgainstTheSide()
    {
        string json = """
            {"Long": {"TrendIntervals": ["1h"], "TrendIntervalsInverted": true},
             "Short": {"TrendIntervals": ["1h"], "TrendIntervalsInverted": true}}
            """;
        EmulatorQueueEntry entry = JsonSerializer.Deserialize<EmulatorQueueEntry>(json)!;

        SettingsTextual longSignal = new(), longTrading = new(), shortSignal = new(), shortTrading = new();
        EmulatorSideConfig.Apply(EmulatorSideConfig.Resolve(entry.Long, null), longSignal, longTrading);
        EmulatorSideConfig.Apply(EmulatorSideConfig.Resolve(entry.Short, null), shortSignal, shortTrading);

        Assert.IsTrue(longSignal.IntervalTrend.Inverted);
        Assert.IsTrue(shortSignal.IntervalTrend.Inverted);

        // And it lands in the compiled condition the signal is checked against.
        SettingsCompiled longCompiled = new(), shortCompiled = new();
        longCompiled.IndexStrategyInternally(longSignal, CryptoTradeSide.Long);
        shortCompiled.IndexStrategyInternally(shortSignal, CryptoTradeSide.Short);
        Assert.AreEqual(CryptoTrendIndicator.Bearish, longCompiled.Trend[CryptoIntervalPeriod.interval1h]);
        Assert.AreEqual(CryptoTrendIndicator.Bullish, shortCompiled.Trend[CryptoIntervalPeriod.interval1h]);
    }

    [TestMethod]
    public void Apply_WithoutKey_KeepsTheTrendWithTheSide()
    {
        SettingsTextual signal = new(), trading = new();
        EmulatorSideConfig.Apply(EmulatorSideConfig.Resolve(new EmulatorSideConfig { TrendIntervals = ["1h"] }, null), signal, trading);

        Assert.IsFalse(signal.IntervalTrend.Inverted);
        SettingsCompiled compiled = new();
        compiled.IndexStrategyInternally(signal, CryptoTradeSide.Long);
        Assert.AreEqual(CryptoTrendIndicator.Bullish, compiled.Trend[CryptoIntervalPeriod.interval1h]);
    }

    [TestMethod]
    public void Resolve_MirrorsInvertedFromOtherSide()
    {
        EmulatorSideConfig source = new() { TrendIntervals = ["1h"], TrendIntervalsInverted = true };
        EmulatorSideConfig resolved = EmulatorSideConfig.Resolve(null, source);

        Assert.AreEqual(true, resolved.TrendIntervalsInverted);
    }
}
