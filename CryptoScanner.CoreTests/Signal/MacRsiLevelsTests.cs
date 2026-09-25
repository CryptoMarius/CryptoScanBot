using CryptoScanner.Analyzers.Mac;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// Support and resistance taken from the RSI instead of from a price pivot.
/// <para>
/// The rule: a support is the LOW of the candle on which the RSI crosses back up through its lower
/// bound, a resistance the HIGH of the candle on which it crosses back down through the upper one.
/// One of each is carried forward until the next crossing replaces it.
/// </para>
/// <para>
/// Measured against 45 level readings on three coins, every one of them reproduced to the cent.
/// Thresholds of 30/70 miss every reading and 40/60 miss half, so the two bounds are not free
/// parameters to tune - they are measurements.
/// </para>
/// </summary>
[TestClass]
public class MacRsiLevelsTests
{
    private List<string> _longBefore = [];
    private List<string> _shortBefore = [];
    private MacSettings _settingsBefore = new();

    [ClassInitialize]
    public static void Register(TestContext _)
    {
        TestBase.RegisterPlugin(new MacPlugin());
    }


    /// <summary>
    /// The enabled strategy lists and the plugin settings are global: the hub builds its indicators
    /// from the first and the extension reads the second. A test that leaves either changed takes
    /// the rest of the suite down with it, so both are put back exactly as they were.
    /// </summary>
    [TestInitialize]
    public void Remember()
    {
        _longBefore = [.. GlobalData.Settings.Signal.Long.Strategy];
        _shortBefore = [.. GlobalData.Settings.Signal.Short.Strategy];
        _settingsBefore = MacPlugin.Settings;
    }


    [TestCleanup]
    public void Restore()
    {
        GlobalData.Settings.Signal.Long.Strategy.Clear();
        GlobalData.Settings.Signal.Long.Strategy.AddRange(_longBefore);
        GlobalData.Settings.Signal.Short.Strategy.Clear();
        GlobalData.Settings.Signal.Short.Strategy.AddRange(_shortBefore);
        new MacPlugin().SettingsBase = _settingsBefore;
    }


    /// <summary>
    /// A stretch that falls hard, turns, and rises hard: the fall drives the RSI under the lower
    /// bound and the turn crosses it back up, which is the support. The rise does the mirror image.
    /// </summary>
    private static List<CryptoCandle> FallAndRise(int fall, int rise)
    {
        List<CryptoCandle> candles = [];
        decimal price = 1000m;
        for (int i = 0; i < fall + rise; i++)
        {
            // The rise is steeper than the fall on purpose: with equal steps the V is symmetric
            // and a low on the way down repeats on the way up, so a test could not tell which of
            // the two candles a level came from.
            price += i < fall ? -8m : 13m;
            candles.Add(new CryptoCandle
            {
                OpenTime = new CandleTime((uint)(i * 300)),
                Open = price,
                High = price + 3m,
                Low = price - 3m,
                Close = price,
                Volume = 100m,
            });
        }
        return candles;
    }


    /// <summary>Everything the extension wrote, one entry per candle, through the real hub.</summary>
    private static List<MacCandleData?> Feed(List<CryptoCandle> candles)
    {
        string name = MacPlugin.StrategyInternal.ToLower();
        if (!GlobalData.Settings.Signal.Long.Strategy.Contains(name))
            GlobalData.Settings.Signal.Long.Strategy.Add(name);

        IntervalIndicatorHub hub = new();
        List<MacCandleData?> seen = [];
        foreach (CryptoCandle candle in candles)
        {
            hub.Add(candle);
            seen.Add(hub.BuildCurrent().GetPluginData<MacCandleData>());
        }
        return seen;
    }


    /// <summary>
    /// The levels are always there now. Until 25 September 2026 a setting chose between the RSI
    /// level and a price pivot, and this test pinned what happened with that setting off; since
    /// the rule is settled there is nothing for the pivot to do and the switch is gone.
    /// The pivot itself is still computed - the chart draws it - but no break reads it.
    /// </summary>
    [TestMethod]
    public void TheLevelsAreAlwaysComputed()
    {
        MacCandleData? last = Feed(FallAndRise(60, 60)).Last();

        Assert.IsNotNull(last, "the extension has to fill the slot at all");
        Assert.IsNotNull(last.RsiLevelLow, "a stretch that falls and turns has a crossing in it");
        Assert.IsNotNull(last.PivotLow, "the price pivot is still there for the chart");
    }


    [TestMethod]
    public void On_TakesTheLowOfTheCandleTheRsiCrossedUpOn()
    {
        new MacPlugin().SettingsBase = new MacSettings();
        List<CryptoCandle> candles = FallAndRise(60, 60);
        List<MacCandleData?> seen = Feed(candles);

        MacCandleData? last = seen[^1];
        Assert.IsNotNull(last?.RsiLevelLow, "a stretch that falls and turns has a crossing in it");

        // The level is the low of a candle that really happened, never a number of its own making.
        int owner = candles.FindIndex(c => (double)c.Low == last.RsiLevelLow!.Value);
        Assert.IsTrue(owner >= 0, "the support has to be the low of one of the candles");

        // And it sits in the rise, a few candles after the turn: the RSI needs those candles to
        // climb back through its lower bound.
        Assert.IsTrue(owner >= 60 && owner <= 70,
            $"the crossing sits on candle {owner}, which is not just after the turn at 60");
    }


    [TestMethod]
    public void On_ALevelSetByTheCandleInHandCannotBeBrokenByIt()
    {
        // The level is LIVE on the candle that sets it, which is what a level is - read off
        // its own Support and Resistance plots, 71 of 71 changes match that way and 55 of 71 match
        // when the level is held back a candle.
        //
        // Holding it back was a guard against a candle breaking a level of its own making, and that
        // guard was never needed: a support IS the low of its candle and a close never falls below
        // its own low. That is the property asserted here, on the candles whose level is their own.
        new MacPlugin().SettingsBase = new MacSettings();
        List<CryptoCandle> candles = FallAndRise(60, 60);
        List<MacCandleData?> seen = Feed(candles);

        int eigen = 0;
        for (int i = 0; i < candles.Count; i++)
        {
            MacCandleData? mac = seen[i];
            if (mac?.RsiLevelLow == null)
                continue;
            if (mac.RsiLevelLowAge == 0)
            {
                eigen++;
                Assert.AreEqual((double)candles[i].Low, mac.RsiLevelLow.Value,
                    $"candle {i} carries an age of zero, so the support has to be its own low");
            }
            Assert.IsTrue((double)candles[i].Close >= mac.RsiLevelLow.Value,
                $"candle {i} closed under the support it was handed, which may never happen "
                + $"({candles[i].Close:N8} against {mac.RsiLevelLow.Value:N8})");
        }
        Assert.IsTrue(eigen > 0, "this series should hold at least one candle that sets its own level");
    }


    [TestMethod]
    public void On_CarriesTheLevelForwardUntilTheNextCrossing()
    {
        new MacPlugin().SettingsBase = new MacSettings();
        List<MacCandleData?> seen = Feed(FallAndRise(60, 60));

        List<double> levels = [.. seen.Where(m => m?.RsiLevelLow != null)
                                      .Select(m => m!.RsiLevelLow!.Value)];
        Assert.IsTrue(levels.Count > 10, "the level has to stay available after the crossing");
        Assert.AreEqual(1, levels.Distinct().Count(),
            "one crossing gives one level, held until the next crossing replaces it");
    }
}
