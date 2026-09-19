using CryptoScanner.Analyzers.Mac;
using CryptoScanner.Core.Contracts;
using CryptoScanner.Core.Core;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Signal.Indicators;

using Skender.Stock.Indicators;

namespace CryptoScanner.CoreTests.Signal;

/// <summary>
/// The levels taken where the RSI turns instead of where the price does.
/// <para>
/// A symmetric price pivot puts a level down every few candles, and most of them carry nothing: the
/// breakout marker fires about three times as often as the catalogued marks. An RSI pivot - the
/// candle whose RSI is the extreme of its window AND past its threshold - puts far fewer levels
/// down. Measured over six labelled coins it keeps all twelve marks that carry a dot while cutting
/// the thirty-nine that carry none to fifteen, which is why the setting exists.
/// </para>
/// <para>
/// The hub is the real path: the RSI comes from the shared registry, so the extension only sees it
/// when the candles go through <see cref="IntervalIndicatorHub"/>.
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


    /// <summary>A rise and a fall, so the RSI has one real top and one real bottom in it.</summary>
    private static List<CryptoCandle> Wave(int count)
    {
        List<CryptoCandle> candles = [];
        decimal price = 1000m;
        for (int i = 0; i < count; i++)
        {
            price += i % 120 < 60 ? 5m : -5m;
            candles.Add(new CryptoCandle
            {
                OpenTime = new CandleTime((uint)(i * 300)),
                Open = price,
                High = price + 2m,
                Low = price - 2m,
                Close = price,
                Volume = 100m,
            });
        }
        return candles;
    }


    /// <summary>What the extension wrote on the last candle, with MAC switched on for the hub.</summary>
    private static MacCandleData? Feed(List<CryptoCandle> candles)
    {
        string name = MacPlugin.StrategyInternal.ToLower();
        if (!GlobalData.Settings.Signal.Long.Strategy.Contains(name))
            GlobalData.Settings.Signal.Long.Strategy.Add(name);

        IntervalIndicatorHub hub = new();
        foreach (CryptoCandle candle in candles)
            hub.Add(candle);
        return hub.BuildCurrent().GetPluginData<MacCandleData>();
    }


    [TestMethod]
    public void Off_LeavesTheRsiLevelsEmptyAndThePricePivotInPlace()
    {
        new MacPlugin().SettingsBase = new MacSettings { UseRsiLevels = false };
        MacCandleData? mac = Feed(Wave(300));

        Assert.IsNotNull(mac, "the extension has to fill the slot at all");
        Assert.IsNull(mac.RsiLevelHigh, "nothing may be computed while the setting is off");
        Assert.IsNull(mac.RsiLevelLow, "nothing may be computed while the setting is off");
        Assert.IsNotNull(mac.PivotHigh, "the price pivot is what the strategy falls back on");
    }


    [TestMethod]
    public void On_TakesTheHighOfACandleThatReallyHappened()
    {
        new MacPlugin().SettingsBase = new MacSettings { UseRsiLevels = true, RsiLevelPivotCandles = 10 };
        List<CryptoCandle> candles = Wave(300);
        MacCandleData? mac = Feed(candles);

        Assert.IsNotNull(mac);
        Assert.IsNotNull(mac.RsiLevelHigh, "a series that rises and falls has an RSI top in it");
        Assert.IsTrue(candles.Any(c => (double)c.High == mac.RsiLevelHigh!.Value),
            "the level has to be the high of a candle, never a number of its own making");
        Assert.IsTrue(mac.RsiLevelHighAge >= 10,
            $"a level confirmed {mac.RsiLevelHighAge} candles back cannot be the candle that breaks it");
    }


    [TestMethod]
    public void On_PutsFewerLevelsDownThanThePricePivot()
    {
        List<CryptoCandle> candles = Wave(300);

        new MacPlugin().SettingsBase = new MacSettings { UseRsiLevels = false };
        MacCandleData? pivot = Feed(candles);

        new MacPlugin().SettingsBase = new MacSettings { UseRsiLevels = true, RsiLevelPivotCandles = 10 };
        MacCandleData? rsi = Feed(candles);

        Assert.IsNotNull(pivot?.PivotHigh);
        Assert.IsNotNull(rsi?.RsiLevelHigh);
        // The whole point: an RSI turn is rarer than a price turn, so by the time the same candle
        // comes round its level has been standing longer.
        Assert.IsTrue(rsi.RsiLevelHighAge >= pivot.PivotHighAge,
            $"the RSI level is {rsi.RsiLevelHighAge} candles old against {pivot.PivotHighAge} for the "
            + "price pivot - an RSI level cannot be the fresher of the two on one series");
    }
}
