using CryptoScanner.Core.Core;
using CryptoScanner.Core.Enums;
using CryptoScanner.Core.Model;
using CryptoScanner.Core.Trader;

namespace CryptoScanner.CoreTests.Trader;

/// <summary>
/// The buy and loss cooldowns can be counted in candles of the signal interval instead of minutes
/// (open point 69). Off by default, so an existing setting keeps meaning minutes.
/// </summary>
[TestClass]
public class CooldownInCandlesTests
{
    private bool _saved;

    [TestInitialize]
    public void SaveSettings() => _saved = GlobalData.Settings.Trading.CooldownInCandles;

    [TestCleanup]
    public void RestoreSettings() => GlobalData.Settings.Trading.CooldownInCandles = _saved;

    private static CryptoInterval Interval(CryptoIntervalPeriod period) => period switch
    {
        CryptoIntervalPeriod.interval1m => CryptoInterval.CreateInterval(period, "1m", 1, null),
        CryptoIntervalPeriod.interval15m => CryptoInterval.CreateInterval(period, "15m", 15, null),
        _ => CryptoInterval.CreateInterval(period, "1h", 60, null),
    };

    [TestMethod]
    public void TheDefaultIsMinutes()
    {
        Assert.IsFalse(new CryptoScanner.Core.Settings.SettingsTrading().CooldownInCandles);
    }

    [TestMethod]
    public void InMinutesTheIntervalDoesNotMatter()
    {
        GlobalData.Settings.Trading.CooldownInCandles = false;
        Assert.AreEqual(30, PositionMonitor.CooldownMinutes(30, Interval(CryptoIntervalPeriod.interval15m)));
        Assert.AreEqual(30, PositionMonitor.CooldownMinutes(30, null));
    }

    [TestMethod]
    public void InCandlesTheValueIsMultipliedByTheIntervalLength()
    {
        GlobalData.Settings.Trading.CooldownInCandles = true;
        Assert.AreEqual(2, PositionMonitor.CooldownMinutes(2, Interval(CryptoIntervalPeriod.interval1m)));
        Assert.AreEqual(30, PositionMonitor.CooldownMinutes(2, Interval(CryptoIntervalPeriod.interval15m)));
        Assert.AreEqual(120, PositionMonitor.CooldownMinutes(2, Interval(CryptoIntervalPeriod.interval1h)));
    }

    [TestMethod]
    public void WithoutAnIntervalItStaysMinutes()
    {
        GlobalData.Settings.Trading.CooldownInCandles = true;
        Assert.AreEqual(2, PositionMonitor.CooldownMinutes(2, null));
    }
}
