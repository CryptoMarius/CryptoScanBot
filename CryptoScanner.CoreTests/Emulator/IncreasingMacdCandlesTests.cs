using CryptoScanner.Core.Core;
using CryptoScanner.Core.Settings;
using CryptoScanner.Emulator.Engine;

using System.Text.Json;

namespace CryptoScanner.CoreTests.Emulator;

/// <summary>
/// EntryConditions.IncreasingMacdCandles (19-10-2026): how many candles in a row the MACD histogram
/// has to rise for CheckIncreasingMacd. The queue has to be able to set it, or the variant with two
/// candles that the storsi.multi review asked for cannot be run.
/// </summary>
[TestClass]
public class IncreasingMacdCandlesTests : TestBase
{
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


    [TestMethod]
    public void DefaultKeepsTheStrategyCount()
    {
        Assert.AreEqual(0, new SettingsEntryConditions().IncreasingMacdCandles);
    }


    [TestMethod]
    public void QueueKeyIsAcceptedAppliedAndReverted()
    {
        InitTestSession();

        var entry = EntryWithTradingOverride("EntryConditions.IncreasingMacdCandles", "2");
        Assert.IsNull(SignalGridExpander.Validate(entry));

        int original = GlobalData.Settings.Trading.EntryConditions.IncreasingMacdCandles;
        var overrides = SignalGridExpander.Apply(entry);
        try
        {
            Assert.AreEqual(2, GlobalData.Settings.Trading.EntryConditions.IncreasingMacdCandles);
        }
        finally
        {
            SignalGridExpander.Revert(overrides);
        }

        Assert.AreEqual(original, GlobalData.Settings.Trading.EntryConditions.IncreasingMacdCandles);
    }
}
